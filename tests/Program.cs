using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HarmonyLib;
using RimWorld;
using RunAndGunShootWhileFleeing;
using Verse;
using Verse.AI;
using FleeUtil = RunAndGunShootWhileFleeing.FleeUtility;

internal static class Program
{
    private static int failures;

    private static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
        if (!ok) failures++;
    }

    private static Pawn CreateTestPawn(string jobDefName = null, string mentalStateDefName = null)
    {
        var pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        var mindState = (Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));
        var handler = (MentalStateHandler)RuntimeHelpers.GetUninitializedObject(typeof(MentalStateHandler));
        var jobs = (Pawn_JobTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_JobTracker));
        var health = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));

        AccessTools.Field(typeof(Pawn_HealthTracker), "healthState").SetValue(health, PawnHealthState.Mobile);
        AccessTools.Field(typeof(Pawn), "mindState").SetValue(pawn, mindState);
        AccessTools.Field(typeof(Pawn_MindState), "mentalStateHandler").SetValue(mindState, handler);
        AccessTools.Field(typeof(Pawn), "jobs").SetValue(pawn, jobs);
        AccessTools.Field(typeof(Pawn), "health").SetValue(pawn, health);
        AccessTools.Field(typeof(MentalStateHandler), "pawn").SetValue(handler, pawn);

        if (jobDefName != null)
        {
            var curJob = (Job)RuntimeHelpers.GetUninitializedObject(typeof(Job));
            curJob.def = new JobDef { defName = jobDefName };
            AccessTools.Field(typeof(Pawn_JobTracker), "curJob").SetValue(jobs, curJob);
        }

        if (mentalStateDefName != null)
        {
            var mentalDef = new MentalStateDef { defName = mentalStateDefName };
            var ms = (MentalState)RuntimeHelpers.GetUninitializedObject(typeof(MentalState));
            ms.def = mentalDef;
            ms.pawn = pawn;
            AccessTools.Field(typeof(MentalStateHandler), "curStateInt").SetValue(handler, ms);
        }

        return pawn;
    }

    private static int Main()
    {
        // Resolve .NET runtime dependencies
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            var path = "/root/.nuget/packages/microsoft.aspnetcore.app.runtime.linux-x64/10.0.14/runtimes/linux-x64/lib/net10.0/" + name.Name + ".dll";
            return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
        };

        Console.WriteLine("=== 1. PRESENCE GATING (RunAndGun absent) ===");
        Check(!ShootWhileFleeingMain.IsRunAndGunPresent(), "IsRunAndGunPresent() is false before RunAndGun is loaded");
        bool initAbsent = ShootWhileFleeingMain.Initialize();
        Check(!initAbsent, "Initialize() returns false when RunAndGun is absent");
        Check(!ShootWhileFleeingMain.Patched, "Patched is false when RunAndGun is absent");
        Check(ShootWhileFleeingMain.TargetMethod1_JobDriver_SetupToils == null, "TargetMethod1 is null when absent");

        Console.WriteLine("\n=== 2. FLEE CLASSIFIER LOGIC ===");
        Check(!FleeUtil.IsFleeing(null), "IsFleeing(null) is false");

        var plainPawn = CreateTestPawn();
        Check(!FleeUtil.IsFleeing(plainPawn), "IsFleeing(plainPawn) is false");

        var fleeJobPawn = CreateTestPawn("Flee");
        Check(FleeUtil.IsFleeing(fleeJobPawn), "IsFleeing with JobDefOf.Flee is true");

        var cowerJobPawn = CreateTestPawn("FleeAndCower");
        Check(FleeUtil.IsFleeing(cowerJobPawn), "IsFleeing with JobDefOf.FleeAndCower is true");

        var cowerShortJobPawn = CreateTestPawn("FleeAndCowerShort");
        Check(FleeUtil.IsFleeing(cowerShortJobPawn), "IsFleeing with JobDefOf.FleeAndCowerShort is true");

        var gotoJobPawn = CreateTestPawn("Goto");
        Check(!FleeUtil.IsFleeing(gotoJobPawn), "IsFleeing with Goto job is false");

        var meleeJobPawn = CreateTestPawn("AttackMelee");
        Check(!FleeUtil.IsFleeing(meleeJobPawn), "IsFleeing with AttackMelee job is false");

        var panicFleePawn = CreateTestPawn(mentalStateDefName: "PanicFlee");
        Check(FleeUtil.IsFleeing(panicFleePawn), "IsFleeing with MentalState PanicFlee is true");

        var panicFirePawn = CreateTestPawn(mentalStateDefName: "PanicFleeFire");
        Check(FleeUtil.IsFleeing(panicFirePawn), "IsFleeing with MentalState PanicFleeFire is true");

        var berserkPawn = CreateTestPawn(mentalStateDefName: "Berserk");
        Check(!FleeUtil.IsFleeing(berserkPawn), "IsFleeing with MentalState Berserk is false");

        var fleeDriver = (JobDriver_Flee)RuntimeHelpers.GetUninitializedObject(typeof(JobDriver_Flee));
        Check(FleeUtil.IsFleeing(plainPawn, fleeDriver), "IsFleeing with JobDriver_Flee is true");

        var gotoDef = new JobDef { defName = "Goto" };
        var fleeDef = new JobDef { defName = "Flee" };
        Check(FleeUtil.IsGotoOrFleeing(gotoDef, plainPawn), "IsGotoOrFleeing with Goto is always true");
        Check(FleeUtil.IsGotoOrFleeing(fleeDef, fleeJobPawn), "IsGotoOrFleeing with Flee on fleeing pawn is true");
        Check(!FleeUtil.IsGotoOrFleeing(fleeDef, plainPawn), "IsGotoOrFleeing with Flee on non-fleeing pawn is false");

        // Settings toggle check
        ShootWhileFleeingMod.settings = new ShootWhileFleeingSettings { enableShootWhileFleeing = false };
        Check(!FleeUtil.IsGotoOrFleeing(fleeDef, fleeJobPawn), "IsGotoOrFleeing returns false for fleeing when mod disabled");
        Check(FleeUtil.IsGotoOrFleeing(gotoDef, plainPawn), "IsGotoOrFleeing returns true for Goto even when mod disabled");
        ShootWhileFleeingMod.settings.enableShootWhileFleeing = true;
        Check(FleeUtil.IsGotoOrFleeing(fleeDef, fleeJobPawn), "IsGotoOrFleeing returns true for fleeing when mod re-enabled");

        Console.WriteLine("\n=== 3. DYNAMIC RUNANDGUN LOADING & PRESENCE GATING ===");
        var ragAssembly = Assembly.LoadFrom("/root/scratch/RunAndGun.dll");
        Check(ragAssembly != null, "Loaded /root/scratch/RunAndGun.dll");
        Check(ShootWhileFleeingMain.IsRunAndGunPresent(), "IsRunAndGunPresent() is true after loading RunAndGun");

        ShootWhileFleeingMain.ResetForTesting();
        bool initPresent = ShootWhileFleeingMain.Initialize();
        Check(initPresent, "Initialize() returns true when RunAndGun is present");
        Check(ShootWhileFleeingMain.Patched, "Patched is true after Initialize()");

        Console.WriteLine("\n=== 4. HARMONY PATCH REGISTRATION ON REAL RUNANDGUN ===");
        // Target 1: Postfix on RunAndGun.Harmony.JobDriver_SetupToils.Postfix
        var t1 = ShootWhileFleeingMain.TargetMethod1_JobDriver_SetupToils;
        Check(t1 != null, "TargetMethod1 (JobDriver_SetupToils.Postfix) resolved");
        var info1 = Harmony.GetPatchInfo(t1);
        bool t1Registered = info1 != null && info1.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_JobDriver_SetupToils_Postfix));
        Check(t1Registered, "Patch_JobDriver_SetupToils_Postfix registered on TargetMethod1");

        // Target 2: Transpiler on RunAndGun.Harmony.Verb_TryStartCastOn.Prefix
        var t2 = ShootWhileFleeingMain.TargetMethod2_Verb_TryStartCastOn;
        Check(t2 != null, "TargetMethod2 (Verb_TryStartCastOn.Prefix) resolved");
        var info2 = Harmony.GetPatchInfo(t2);
        bool t2Registered = info2 != null && info2.Transpilers.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_Verb_TryStartCastOn_Transpiler));
        Check(t2Registered, "Patch_Verb_TryStartCastOn_Transpiler registered on TargetMethod2");

        // Target 3: Postfix on RunAndGun.Stance_RunAndGun_Cooldown.get_StanceBusy
        var t3 = ShootWhileFleeingMain.TargetMethod3_StanceBusy;
        Check(t3 != null, "TargetMethod3 (Stance_RunAndGun_Cooldown.StanceBusy) resolved");
        var info3 = Harmony.GetPatchInfo(t3);
        bool t3Registered = info3 != null && info3.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_Stance_RunAndGun_Cooldown_StanceBusy));
        Check(t3Registered, "Patch_Stance_RunAndGun_Cooldown_StanceBusy registered on TargetMethod3");

        // Target 4: Postfix on RunAndGun.CompRunAndGun.get_isEnabled
        var t4 = ShootWhileFleeingMain.TargetMethod4_IsEnabled;
        Check(t4 != null, "TargetMethod4 (CompRunAndGun.isEnabled) resolved");
        var info4 = Harmony.GetPatchInfo(t4);
        bool t4Registered = info4 != null && info4.Postfixes.Any(p => p.PatchMethod.DeclaringType == typeof(Patch_CompRunAndGun_IsEnabled));
        Check(t4Registered, "Patch_CompRunAndGun_IsEnabled registered on TargetMethod4");

        Console.WriteLine("\n=== 5. PATCH HOOK BEHAVIOR & SAFETY VERIFICATION ===");
        // Verify Transpiler rewritten instructions
        var modifiedInstructions = PatchProcessor.GetCurrentInstructions(t2);
        bool hasCallToHelper = modifiedInstructions.Any(i => i.opcode == OpCodes.Call && i.operand is MethodInfo m && m.Name == "IsGotoOrFleeing");
        Check(hasCallToHelper, "Transpiler inlined call to FleeUtility.IsGotoOrFleeing into Verb_TryStartCastOn.Prefix");

        // StanceBusy Postfix hook test
        var stanceCooldown = (Stance_Cooldown)RuntimeHelpers.GetUninitializedObject(typeof(Stance_Cooldown));
        var stanceTracker = (Pawn_StanceTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_StanceTracker));
        stanceCooldown.stanceTracker = stanceTracker;
        AccessTools.Field(typeof(Pawn_StanceTracker), "pawn").SetValue(stanceTracker, fleeJobPawn);

        bool stanceBusyResult = true;
        Patch_Stance_RunAndGun_Cooldown_StanceBusy.Postfix(stanceCooldown, ref stanceBusyResult);
        Check(!stanceBusyResult, "StanceBusy Postfix sets result to false for fleeing pawn");

        AccessTools.Field(typeof(Pawn_StanceTracker), "pawn").SetValue(stanceTracker, plainPawn);
        stanceBusyResult = true;
        Patch_Stance_RunAndGun_Cooldown_StanceBusy.Postfix(stanceCooldown, ref stanceBusyResult);
        Check(stanceBusyResult, "StanceBusy Postfix preserves true for non-fleeing pawn");

        // CompRunAndGun isEnabled Postfix hook test
        var compType = AccessTools.TypeByName("RunAndGun.CompRunAndGun");
        var comp = (ThingComp)RuntimeHelpers.GetUninitializedObject(compType);
        AccessTools.Field(typeof(ThingComp), "parent").SetValue(comp, fleeJobPawn);

        bool compEnabled = false;
        Patch_CompRunAndGun_IsEnabled.Postfix(comp, ref compEnabled);
        Check(compEnabled, "CompRunAndGun.isEnabled Postfix sets result to true for fleeing pawn");

        AccessTools.Field(typeof(ThingComp), "parent").SetValue(comp, plainPawn);
        compEnabled = false;
        Patch_CompRunAndGun_IsEnabled.Postfix(comp, ref compEnabled);
        Check(!compEnabled, "CompRunAndGun.isEnabled Postfix preserves false for non-fleeing pawn");

        Console.WriteLine("\n----------------------------------------");
        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }
}
