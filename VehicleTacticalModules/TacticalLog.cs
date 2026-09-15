using Verse;

namespace VehicleTacticalModules
{
    public static class TacticalLog
    {
        public static void Message(string msg)
        {
            if (TacticalModulesSettings.enableLogging)
            {
                Log.Message($"[VehicleTacticalModules] {msg}");
            }
        }

        public static void Warning(string msg)
        {
            if (TacticalModulesSettings.enableLogging)
            {
                Log.Warning($"[VehicleTacticalModules] {msg}");
            }
        }

        public static void Error(string msg)
        {
            Log.Error($"[VehicleTacticalModules] {msg}");
        }
    }
}
