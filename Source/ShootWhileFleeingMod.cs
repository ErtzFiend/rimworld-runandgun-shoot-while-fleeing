using UnityEngine;
using Verse;

namespace RunAndGunShootWhileFleeing
{
    public class ShootWhileFleeingSettings : ModSettings
    {
        // Safe default: true so addon functionality activates upon install.
        public bool enableShootWhileFleeing = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableShootWhileFleeing, "enableShootWhileFleeing", true);
        }
    }

    public class ShootWhileFleeingMod : Mod
    {
        public static ShootWhileFleeingSettings settings;

        public ShootWhileFleeingMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ShootWhileFleeingSettings>();
        }

        public static bool IsEnabled => settings == null || settings.enableShootWhileFleeing;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "Enable shoot while fleeing",
                ref settings.enableShootWhileFleeing,
                "When enabled, fleeing pawns will shoot while moving using Run and Gun logic. Safe default is enabled."
            );
            listing.End();
        }

        public override string SettingsCategory()
        {
            return "Run and Gun: Shoot While Fleeing";
        }
    }
}
