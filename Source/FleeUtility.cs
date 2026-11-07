using RimWorld;
using Verse;
using Verse.AI;

namespace RunAndGunShootWhileFleeing
{
    public static class FleeUtility
    {
        public static bool IsFleeing(Pawn pawn)
        {
            if (pawn == null) return false;

            // 1. Check mental state (PanicFlee, PanicFleeFire)
            MentalStateDef mentalDef = pawn.MentalStateDef;
            if (mentalDef != null)
            {
                if (mentalDef.defName == "PanicFlee" || (MentalStateDefOf.PanicFlee != null && mentalDef == MentalStateDefOf.PanicFlee)) return true;
                if (mentalDef.defName == "PanicFleeFire" || (MentalStateDefOf.PanicFleeFire != null && mentalDef == MentalStateDefOf.PanicFleeFire)) return true;
            }

            // 2. Check current job def via property or CurJob
            JobDef curJobDef = pawn.CurJobDef ?? pawn.CurJob?.def;
            if (curJobDef != null)
            {
                if (curJobDef.defName == "Flee" || (JobDefOf.Flee != null && curJobDef == JobDefOf.Flee)) return true;
                if (curJobDef.defName == "FleeAndCower" || (JobDefOf.FleeAndCower != null && curJobDef == JobDefOf.FleeAndCower)) return true;
                if (curJobDef.defName == "FleeAndCowerShort" || (JobDefOf.FleeAndCowerShort != null && curJobDef == JobDefOf.FleeAndCowerShort)) return true;
            }

            return false;
        }

        public static bool IsFleeing(Pawn pawn, JobDriver driver)
        {
            if (IsFleeing(pawn)) return true;
            if (driver is JobDriver_Flee) return true;
            if (driver != null && driver.GetType().Name == "JobDriver_Flee") return true;
            return false;
        }

        public static bool IsGotoOrFleeing(JobDef curJobDef, Pawn pawn)
        {
            if (curJobDef != null && (curJobDef.defName == "Goto" || (JobDefOf.Goto != null && curJobDef == JobDefOf.Goto)))
            {
                return true;
            }

            if (!ShootWhileFleeingMod.IsEnabled)
            {
                return false;
            }

            return IsFleeing(pawn);
        }
    }
}
