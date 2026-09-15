using Verse;

namespace VehicleTacticalModules
{
    public class CompPreserveCargo : ThingComp
    {
        public CompProperties_PreserveCargo Props => (CompProperties_PreserveCargo)this.props;

        public bool Protects(ThingDef def)
        {
            if (this.Props == null || def == null)
            {
                return false;
            }
            return this.Props.preserveAll;
        }
    }
}
