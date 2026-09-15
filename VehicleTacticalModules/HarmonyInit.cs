using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace VehicleTacticalModules
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.sanos.vehicletacticalmodules");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Dynamically patch VRF_JobGiver_DynamicAssault.TryGiveJob to avoid a hard assembly dependency on VRF
            Type vrfDynamicAssaultType = AccessTools.TypeByName("VehicleRaidFramework.VRF_JobGiver_DynamicAssault");
            if (vrfDynamicAssaultType != null)
            {
                MethodInfo original = AccessTools.Method(vrfDynamicAssaultType, "TryGiveJob", new Type[] { typeof(Pawn) });
                MethodInfo prefix = AccessTools.Method(typeof(Patch_DeadMeat_DynamicAssault), nameof(Patch_DeadMeat_DynamicAssault.Prefix));

                if (original != null && prefix != null)
                {
                    harmony.Patch(original, prefix: new HarmonyMethod(prefix));
                    TacticalLog.Message("[VTM] Successfully attached DeadMeat Prefix patch to VRF_JobGiver_DynamicAssault.TryGiveJob.");
                }
                else
                {
                    TacticalLog.Warning("[VTM] Failed to find TryGiveJob or Prefix method for DynamicAssault patch.");
                }
            }
            else
            {
                TacticalLog.Message("[VTM] VehicleRaidFramework is not loaded. DynamicAssault patch skipped.");
            }
        }
    }
}
