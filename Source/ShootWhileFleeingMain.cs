using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RunAndGunShootWhileFleeing
{
    [StaticConstructorOnStartup]
    public static class ShootWhileFleeingMain
    {
        public const string HarmonyId = "ertzfiend.runandgunshootwhilefleeing";

        public static MethodBase TargetMethod1_JobDriver_SetupToils { get; private set; }
        public static MethodBase TargetMethod2_Verb_TryStartCastOn { get; private set; }
        public static MethodBase TargetMethod3_StanceBusy { get; private set; }
        public static MethodBase TargetMethod4_IsEnabled { get; private set; }

        public static bool Patched { get; private set; }

        static ShootWhileFleeingMain()
        {
            Initialize();
        }

        public static void ResetForTesting()
        {
            Patched = false;
            TargetMethod1_JobDriver_SetupToils = null;
            TargetMethod2_Verb_TryStartCastOn = null;
            TargetMethod3_StanceBusy = null;
            TargetMethod4_IsEnabled = null;
        }

        public static bool IsRunAndGunPresent()
        {
            return AccessTools.TypeByName("RunAndGun.CompRunAndGun") != null;
        }

        public static bool Initialize(Harmony customHarmony = null)
        {
            if (Patched) return true;

            if (!IsRunAndGunPresent())
            {
                Log.Message("[RunAndGun: Shoot While Fleeing] MemeGoddess.RunAndGun is not active; addon inactive.");
                return false;
            }

            Harmony harmony = customHarmony ?? new Harmony(HarmonyId);
            bool ok = ApplyPatches(harmony);
            if (ok)
            {
                Patched = true;
                Log.Message("[RunAndGun: Shoot While Fleeing] Successfully patched RunAndGun for fleeing pawns (4 targets registered).");
            }
            return ok;
        }

        public static bool ApplyPatches(Harmony harmony)
        {
            try
            {
                // Target 1: Postfix on RunAndGun.Harmony.JobDriver_SetupToils.Postfix
                var type1 = AccessTools.TypeByName("RunAndGun.Harmony.JobDriver_SetupToils");
                TargetMethod1_JobDriver_SetupToils = AccessTools.Method(type1, "Postfix");
                if (TargetMethod1_JobDriver_SetupToils != null)
                {
                    harmony.Patch(
                        TargetMethod1_JobDriver_SetupToils,
                        postfix: new HarmonyMethod(typeof(Patch_JobDriver_SetupToils_Postfix), nameof(Patch_JobDriver_SetupToils_Postfix.Postfix))
                    );
                }
                else
                {
                    Log.Warning("[RunAndGun: Shoot While Fleeing] Failed to find RunAndGun.Harmony.JobDriver_SetupToils.Postfix");
                }

                // Target 2: Transpiler on RunAndGun.Harmony.Verb_TryStartCastOn.Prefix
                var type2 = AccessTools.TypeByName("RunAndGun.Harmony.Verb_TryStartCastOn");
                TargetMethod2_Verb_TryStartCastOn = AccessTools.Method(type2, "Prefix");
                if (TargetMethod2_Verb_TryStartCastOn != null)
                {
                    harmony.Patch(
                        TargetMethod2_Verb_TryStartCastOn,
                        transpiler: new HarmonyMethod(typeof(Patch_Verb_TryStartCastOn_Transpiler), nameof(Patch_Verb_TryStartCastOn_Transpiler.Transpiler))
                    );
                }
                else
                {
                    Log.Warning("[RunAndGun: Shoot While Fleeing] Failed to find RunAndGun.Harmony.Verb_TryStartCastOn.Prefix");
                }

                // Target 3: Postfix on RunAndGun.Stance_RunAndGun_Cooldown.get_StanceBusy
                var type3 = AccessTools.TypeByName("RunAndGun.Stance_RunAndGun_Cooldown");
                TargetMethod3_StanceBusy = AccessTools.PropertyGetter(type3, "StanceBusy");
                if (TargetMethod3_StanceBusy != null)
                {
                    harmony.Patch(
                        TargetMethod3_StanceBusy,
                        postfix: new HarmonyMethod(typeof(Patch_Stance_RunAndGun_Cooldown_StanceBusy), nameof(Patch_Stance_RunAndGun_Cooldown_StanceBusy.Postfix))
                    );
                }
                else
                {
                    Log.Warning("[RunAndGun: Shoot While Fleeing] Failed to find RunAndGun.Stance_RunAndGun_Cooldown.StanceBusy getter");
                }

                // Target 4: Postfix on RunAndGun.CompRunAndGun.get_isEnabled
                var type4 = AccessTools.TypeByName("RunAndGun.CompRunAndGun");
                TargetMethod4_IsEnabled = AccessTools.PropertyGetter(type4, "isEnabled");
                if (TargetMethod4_IsEnabled != null)
                {
                    harmony.Patch(
                        TargetMethod4_IsEnabled,
                        postfix: new HarmonyMethod(typeof(Patch_CompRunAndGun_IsEnabled), nameof(Patch_CompRunAndGun_IsEnabled.Postfix))
                    );
                }
                else
                {
                    Log.Warning("[RunAndGun: Shoot While Fleeing] Failed to find RunAndGun.CompRunAndGun.isEnabled getter");
                }

                return TargetMethod1_JobDriver_SetupToils != null &&
                       TargetMethod2_Verb_TryStartCastOn != null &&
                       TargetMethod3_StanceBusy != null &&
                       TargetMethod4_IsEnabled != null;
            }
            catch (Exception ex)
            {
                Log.Error("[RunAndGun: Shoot While Fleeing] Exception during patching: " + ex);
                return false;
            }
        }
    }
}
