using System;
using System.Collections.Generic;
using Verse;

namespace VehicleTacticalModules
{
    public class GameComponent_VehicleTacticalModules : GameComponent
    {
        private Dictionary<string, int> lastIncidentCallTicks = new Dictionary<string, int>();

        public GameComponent_VehicleTacticalModules(Game game) : base()
        {
        }

        public int GetLastCallTick(string key)
        {
            if (lastIncidentCallTicks != null && lastIncidentCallTicks.TryGetValue(key, out int tick))
            {
                return tick;
            }
            return -999999;
        }

        public void SetLastCallTick(string key, int tick)
        {
            if (lastIncidentCallTicks == null)
            {
                lastIncidentCallTicks = new Dictionary<string, int>();
            }
            lastIncidentCallTicks[key] = tick;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref lastIncidentCallTicks, "lastIncidentCallTicks", LookMode.Value, LookMode.Value);
            if (lastIncidentCallTicks == null)
            {
                lastIncidentCallTicks = new Dictionary<string, int>();
            }
        }
    }
}
