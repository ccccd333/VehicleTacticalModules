using HarmonyLib;
using RimWorld;
using Verse;
using Vehicles;

namespace VehicleTacticalModules
{
    [HarmonyPatch(typeof(TargetingHelper), "TargetValidator")]
    public static class Patch_DeadMeat_BreachTargetValidator
    {
        public const string DeadMeatDefName = "DMS_Chelydridae_MainBattleTank_Miho_Faction_Supremacist_DM_NPC";

        [HarmonyPostfix]
        public static void Postfix(VehicleTurret turret, Map map, LocalTargetInfo target, ref bool __result)
        {
            if (__result) return;
            VehiclePawn vehicle = turret?.vehicle;
            if (vehicle == null || vehicle.def == null || vehicle.def.defName != DeadMeatDefName)
            {
                return;
            }

            // If this vehicle has a breaching target and the target matches, allow turret targeting
            if (vehicle.mindState?.breachingTarget != null && target.HasThing && target.Thing == vehicle.mindState.breachingTarget.target)
            {
                __result = true;
            }
        }
    }
}
