using RimWorld;
using Verse;
using Verse.AI;

namespace RunAndGunShootWhileFleeing
{
    public static class AutoAttackHelper
    {
        public static void CheckForAutoAttack(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed) return;
            if (pawn.story != null && (pawn.story.DisabledWorkTagsBackstoryAndTraits & WorkTags.Violent) != 0) return;
            if (pawn.Faction == null) return;

            // Don't restart attack if already in RunAndGun stance
            if (pawn.stances?.curStance != null && pawn.stances.curStance.GetType().FullName == "RunAndGun.Stance_RunAndGun") return;

            // If drafter exists, respect FireAtWill
            if (pawn.drafter != null && !pawn.drafter.FireAtWill) return;

            // Get attack verb
            Verb attackVerb = pawn.TryGetAttackVerb(null, false, false);
            if (attackVerb == null) return;

            TargetScanFlags flags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
            if (VerbUtility.IsIncendiary_Ranged(attackVerb))
            {
                flags |= TargetScanFlags.NeedNonBurning;
            }

            Thing target = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(pawn, flags, null, 0f, 9999f);
            if (target == null) return;

            if (attackVerb.IsMeleeAttack && ReachabilityImmediate.CanReachImmediate(pawn, target, PathEndMode.Touch)) return;

            pawn.TryStartAttack(target);
        }
    }
}
