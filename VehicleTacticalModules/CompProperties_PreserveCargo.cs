using System.Collections.Generic;
using Verse;

namespace VehicleTacticalModules
{
    public class CompProperties_PreserveCargo : CompProperties
    {
        public bool preserveAll = true;

        public CompProperties_PreserveCargo()
        {
            this.compClass = typeof(CompPreserveCargo);
        }
    }
}
