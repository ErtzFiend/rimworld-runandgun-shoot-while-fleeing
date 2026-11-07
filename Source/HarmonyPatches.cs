using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RunAndGunShootWhileFleeing
{
    // Target 1: Postfix on RunAndGun.Harmony.JobDriver_SetupToils.Postfix
    public static class Patch_JobDriver_SetupToils_Postfix
    {
        public static void Postfix(JobDriver __instance, [HarmonyArgument(1)] List<Toil> toils)
        {
            if (!ShootWhileFleeingMod.IsEnabled) return;
            if (__instance == null || toils == null || toils.Count == 0) return;

            Pawn pawn = __instance.pawn;
            if (pawn == null) return;

            if (!FleeUtility.IsFleeing(pawn, __instance)) return;

            foreach (Toil toil in toils)
            {
                if (toil == null) continue;
                toil.AddPreTickAction(() =>
                {
                    if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed) return;
                    if (!Gen.IsHashIntervalTick(pawn, 10)) return;
                    if (FireUtility.IsBurning(pawn)) return;
                    if (!ShootWhileFleeingMod.IsEnabled) return;
                    if (!FleeUtility.IsFleeing(pawn, __instance)) return;

                    AutoAttackHelper.CheckForAutoAttack(pawn);
                });
            }
        }
    }

    // Target 2: Transpiler on RunAndGun.Harmony.Verb_TryStartCastOn.Prefix
    // Replaces `curJob.def == JobDefOf.Goto` check with `FleeUtility.IsGotoOrFleeing(curJob.def, pawn)`.
    // Eliminates the dangerous temporary `curJob.def` mutation and restore window entirely.
    public static class Patch_Verb_TryStartCastOn_Transpiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            bool found = false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].LoadsField(AccessTools.Field(typeof(JobDefOf), nameof(JobDefOf.Goto))))
                {
                    if (i + 1 < list.Count && (list[i + 1].opcode == OpCodes.Beq || list[i + 1].opcode == OpCodes.Beq_S))
                    {
                        var targetLabel = list[i + 1].operand;
                        // Replace ldsfld JobDefOf::Goto with ldloc.0 (pawn)
                        list[i].opcode = OpCodes.Ldloc_0;
                        list[i].operand = null;
                        // Replace beq/beq.s with call FleeUtility.IsGotoOrFleeing
                        var callMethod = AccessTools.Method(typeof(FleeUtility), nameof(FleeUtility.IsGotoOrFleeing));
                        list[i + 1].opcode = OpCodes.Call;
                        list[i + 1].operand = callMethod;
                        // Insert brtrue to targetLabel
                        list.Insert(i + 2, new CodeInstruction(OpCodes.Brtrue, targetLabel));
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                Log.Warning("[RunAndGun: Shoot While Fleeing] Transpiler could not find JobDefOf.Goto comparison in RunAndGun.Harmony.Verb_TryStartCastOn.Prefix");
            }

            return list;
        }
    }

    // Target 3: Postfix on RunAndGun.Stance_RunAndGun_Cooldown.get_StanceBusy
    public static class Patch_Stance_RunAndGun_Cooldown_StanceBusy
    {
        public static void Postfix(Stance_Cooldown __instance, ref bool __result)
        {
            if (!ShootWhileFleeingMod.IsEnabled) return;
            Pawn pawn = __instance?.stanceTracker?.pawn;
            if (pawn != null && FleeUtility.IsFleeing(pawn))
            {
                __result = false;
            }
        }
    }

    // Target 4: Postfix on RunAndGun.CompRunAndGun.get_isEnabled
    public static class Patch_CompRunAndGun_IsEnabled
    {
        public static void Postfix(ThingComp __instance, ref bool __result)
        {
            if (__result) return;
            if (!ShootWhileFleeingMod.IsEnabled) return;
            Pawn pawn = __instance?.parent as Pawn;
            if (pawn != null && FleeUtility.IsFleeing(pawn))
            {
                __result = true;
            }
        }
    }
}
