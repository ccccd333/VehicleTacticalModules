using RimWorld;
using Verse;

namespace VehicleTacticalModules
{
    public class CompProperties_UseEffect_CallIncident : CompProperties_UseEffect
    {
        public IncidentDef incident;
        public float fixedPoints = 10000f;
        public bool consumeOnSuccess = true;
        public SoundDef sound;
        public string confirmMessageKey;
        public bool checkCanFireNow = true;
        public bool requirePlayerHome = true;
        public float cooldownDays = 0f;
        public float cooldownHours = 3f;

        public int CooldownTicks => (int)(this.cooldownDays * 60000f + this.cooldownHours * 2500f);

        public CompProperties_UseEffect_CallIncident()
        {
            this.compClass = typeof(CompUseEffect_CallIncident);
        }
    }
}
