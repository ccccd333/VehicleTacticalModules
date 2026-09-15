using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace VehicleTacticalModules
{
    [HarmonyPatch(typeof(CompRottable), "Active", MethodType.Getter)]
    public static class Patch_CompRottable_Active
    {
        [HarmonyPrefix]
        public static bool Prefix(CompRottable __instance, ref bool __result)
        {
            if (__instance.parent?.ParentHolder is Pawn_InventoryTracker tracker
                && tracker.pawn is VehiclePawn vehicle)
            {
                CompPreserveCargo comp = vehicle.GetComp<CompPreserveCargo>();
                if (comp != null && comp.Protects(__instance.parent.def))
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
