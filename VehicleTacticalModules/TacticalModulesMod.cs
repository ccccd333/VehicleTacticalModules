using UnityEngine;
using Verse;

namespace VehicleTacticalModules
{
    public class TacticalModulesMod : Mod
    {
        public static TacticalModulesSettings settings;

        public TacticalModulesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<TacticalModulesSettings>();
        }

        public override string SettingsCategory()
        {
            return "VTM_ModCategory".TranslateOrFallback("Vehicle Tactical Modules");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "VTM_EnableLoggingLabel".TranslateOrFallback("Enable debug logging"),
                ref TacticalModulesSettings.enableLogging,
                "VTM_EnableLoggingTooltip".TranslateOrFallback("When enabled, detailed lifecycle and execution logs for vehicle tactical modules will be printed to the debug console.")
            );

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
