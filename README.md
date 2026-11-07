# Run and Gun: Shoot While Fleeing

RimWorld 1.7 addon for [Run and Gun - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3562365100)
(`MemeGoddess.RunAndGun`). Pawns that are **fleeing** — panic-flee (`PanicFlee`/`PanicFleeFire`)
or running a `Flee` / `FleeAndCower` / `FleeAndCowerShort` job — keep firing on the move exactly
like normal run-and-gun movement, instead of being arrested by vanilla warmup while they run.

## What this addon changes

Run and Gun itself hardcodes its mobile-shooting path to `JobDefOf.Goto` only. Fleeing pawns run
`JobDriver_Flee`, so all four of Run and Gun's gates shut off during a flee. This addon reopens
those gates **only for fleeing pawns**, staying inside Run and Gun's own pipeline (it does not
re-implement run and gun and does not patch vanilla `Verb`/`StanceTracker` independently):

1. **`RunAndGun.Harmony.JobDriver_SetupToils.Postfix`** (postfix) — attaches Run and Gun's
   auto-attack pre-tick to fleeing-job toils, so a fleeing pawn keeps scanning for and firing at
   targets.
2. **`RunAndGun.Harmony.Verb_TryStartCastOn.Prefix`** (transpiler) — replaces the
   `curJob.def == JobDefOf.Goto` comparison with a call that returns true for `Goto` **or** any
   fleeing job/mental state, so the cast is not interrupted by vanilla warmup while fleeing.
   The pawn's job definition is **never mutated** — see design note below.
3. **`RunAndGun.Stance_RunAndGun_Cooldown.get_StanceBusy`** (postfix) — returns `false` while
   fleeing so the pawn keeps moving during the post-fire weapon cooldown.
4. **`RunAndGun.CompRunAndGun.get_isEnabled`** (postfix) — reports run-and-gun as enabled while
   fleeing, so the shooting state survives the whole flee.

Other suppression reasons (downed/incapacitated, drafted restrictions, non-fleeing mental states
like berserk) are untouched.

## Design note: why a transpiler (and not a job-def swap)

An earlier draft temporarily set `curJob.def = JobDefOf.Goto` around Run and Gun's gate and
restored it afterwards. That was rejected in review: `WarmupComplete()` can interrupt jobs, down
targets or fire events mid-window, and a restore-after-throw could corrupt a pawn's job
definition. The shipped implementation is a narrow transpiler that rewrites only the
`JobDefOf.Goto` comparison inside Run and Gun's own prefix — zero state mutation, exception-safe
by construction.

## Settings

- Mod Options toggle: **Shoot while fleeing** (default **ON**). Turning it off makes the addon
  inactive (all four gates revert to vanilla Run and Gun behavior).

## When Run and Gun is missing

The addon loads harmlessly: a runtime presence check finds no `RunAndGun` types, no patches are
applied, and no errors are thrown.

## Limitations

- Tightly coupled to the installed Run and Gun build: verified against
  `RunAndGun.dll` SHA-256 `6c60cdf7c1d3c404dcc10c17759108d28a68ae40c7301a8fea47d2e49efba1c1`
  (workshop 3562365100, "Run and Gun - Continued"). If that mod updates its internals, patch
  targets may need re-verification.
- Fleeing pawns shoot opportunistically from their current position (Run and Gun's own
  `AttackTargetFinder.BestShootTargetFromCurrentPosition` path); they do not stop to aim.
- **Not tested in a live game.** Regression tests (38 checks) cover the flee classifier,
  presence gating, patch registration on the real RunAndGun methods, and patch hook behavior.

## Build and test

```
dotnet build RunAndGunShootWhileFleeing.csproj -c Release -p:GameManagedDir=/path/to/RimWorldWin64_Data/Managed
dotnet run --project tests/RunAndGunShootWhileFleeing.Tests.csproj -c Release
```

`GameManagedDir` defaults to `/root/rimworld_libs`. Dependencies: Harmony, MemeGoddess.RunAndGun
(this mod loads after it).
