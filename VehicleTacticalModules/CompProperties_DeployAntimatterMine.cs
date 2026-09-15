using System.Collections.Generic;
using Verse;

namespace VehicleTacticalModules
{
    public class CompProperties_DeployAntimatterMine : CompProperties
    {
        public List<ThingDefCountClass> costList = new List<ThingDefCountClass>();
        public int meatCost = 0;
        public ThingDef meatDef = null;
        public ThingDef mineDef = null;
        public int cooldownTicks = 300;
        public string requiresUpgradeKey = null;
        public string iconTexPath = "Things/Vehicles/Chelydridae/DeadMeat_AntimatterMine";

        public CompProperties_DeployAntimatterMine()
        {
            this.compClass = typeof(CompDeployAntimatterMine);
        }
    }
}
