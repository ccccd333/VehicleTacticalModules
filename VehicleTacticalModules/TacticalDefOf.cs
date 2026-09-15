using RimWorld;
using Verse;

namespace VehicleTacticalModules
{
    [DefOf]
    public static class TacticalThingDefOf
    {
        public static ThingDef DMS_DeadMeat_AntimatterMine;
        public static ThingDef TrapIED_AntigrainWarhead;

        static TacticalThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TacticalThingDefOf));
        }
    }
}
