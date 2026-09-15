using Verse;

namespace VehicleTacticalModules
{
    public class TacticalModulesSettings : ModSettings
    {
        public static bool enableLogging = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableLogging, "enableLogging", false);
        }
    }
}
