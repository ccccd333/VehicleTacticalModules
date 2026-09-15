using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Vehicles;

namespace VehicleTacticalModules
{
    public class CompDeployAntimatterMine : ThingComp
    {
        public CompProperties_DeployAntimatterMine Props => (CompProperties_DeployAntimatterMine)this.props;

        private int lastDeployTick = -999999;
        private ThingDef cachedMeatDef;
        private ThingDef cachedMineDef;

        public VehiclePawn Vehicle => this.parent as VehiclePawn;

        public ThingDef MeatDef
        {
            get
            {
                if (this.cachedMeatDef == null)
                {
                    this.cachedMeatDef = this.Props.meatDef ?? ThingDefOf.Meat_Human;
                }
                return this.cachedMeatDef;
            }
        }

        public ThingDef MineDef
        {
            get
            {
                if (this.cachedMineDef == null)
                {
                    this.cachedMineDef = this.Props.mineDef 
                        ?? TacticalThingDefOf.DMS_DeadMeat_AntimatterMine 
                        ?? TacticalThingDefOf.TrapIED_AntigrainWarhead;
                }
                return this.cachedMineDef;
            }
        }

        private string SafeLabel
        {
            get
            {
                if (this.parent == null)
                {
                    return "null";
                }
                try
                {
                    if (this.parent is Pawn p && p.kindDef != null)
                    {
                        return p.LabelShortCap;
                    }
                    return this.parent.def?.defName ?? "Vehicle";
                }
                catch
                {
                    return this.parent.def?.defName ?? "Vehicle";
                }
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"Initialize() called for {this.SafeLabel} (Def: {this.parent?.def?.defName})");
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"PostSpawnSetup() called for {this.SafeLabel} (respawningAfterLoad: {respawningAfterLoad}, map: {this.parent?.Map?.ToString() ?? "null"})");
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"PostDeSpawn() called for {this.SafeLabel} from map: {map?.ToString() ?? "null"} (mode: {mode})");
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.lastDeployTick, "lastDeployTick", -999999);
            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"PostExposeData() called for {this.SafeLabel} (mode: {Scribe.mode}, lastDeployTick: {this.lastDeployTick})");
            }
        }

        public bool IsUpgradeUnlocked()
        {
            if (string.IsNullOrEmpty(this.Props.requiresUpgradeKey))
            {
                return true;
            }

            CompUpgradeTree tree = this.Vehicle?.CompUpgradeTree;
            if (tree == null || tree.Props?.def == null)
            {
                return false;
            }

            UpgradeNode node = tree.Props.def.GetNode(this.Props.requiresUpgradeKey);
            return node != null && tree.NodeUnlocked(node);
        }

        public List<ThingDefCountClass> EffectiveCosts
        {
            get
            {
                if (this.Props.costList != null && this.Props.costList.Count > 0)
                {
                    return this.Props.costList;
                }
                if (this.Props.meatCost > 0)
                {
                    ThingDef mDef = this.MeatDef;
                    if (mDef != null)
                    {
                        return new List<ThingDefCountClass> { new ThingDefCountClass(mDef, this.Props.meatCost) };
                    }
                }
                return new List<ThingDefCountClass>();
            }
        }

        public int GetAvailableCount(ThingDef def)
        {
            if (this.Vehicle?.inventory?.innerContainer == null || def == null)
            {
                return 0;
            }

            return this.Vehicle.inventory.innerContainer.TotalStackCountOfDef(def);
        }

        public int GetAvailableMeatCount()
        {
            return this.GetAvailableCount(this.MeatDef);
        }

        public bool HasSufficientCosts(out List<string> missingList)
        {
            missingList = new List<string>();
            List<ThingDefCountClass> costs = this.EffectiveCosts;
            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass cost = costs[i];
                int available = this.GetAvailableCount(cost.thingDef);
                if (available < cost.count)
                {
                    string label = cost.thingDef?.LabelCap ?? "Unknown";
                    missingList.Add($"{label} ({available} / {cost.count})");
                }
            }
            return missingList.Count == 0;
        }

        private string GetCostDescription()
        {
            List<ThingDefCountClass> costs = this.EffectiveCosts;
            if (costs.Count == 0)
            {
                return string.Empty;
            }
            List<string> lines = new List<string>();
            for (int i = 0; i < costs.Count; i++)
            {
                ThingDefCountClass c = costs[i];
                string label = c.thingDef?.LabelCap ?? "Unknown";
                lines.Add($"• {label}: {c.count}");
            }
            return string.Join("\n", lines);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"CompGetGizmosExtra() evaluated for {this.SafeLabel} (Faction: {this.parent?.Faction?.Name ?? "None"}, IsPlayer: {this.parent?.Faction == Faction.OfPlayer}, UpgradeUnlocked: {this.IsUpgradeUnlocked()})");
            }

            // Only available for player faction
            if (this.parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            // Check if required upgrade node is unlocked
            if (!this.IsUpgradeUnlocked())
            {
                yield break;
            }

            string costDesc = this.GetCostDescription();
            Command_Action cmd = new Command_Action
            {
                defaultLabel = "VTM_DeployAntimatterMine_Label".TranslateOrFallback("Deploy Antimatter Mine"),
                defaultDesc = "VTM_DeployAntimatterMine_Desc".TranslateOrFallback($"Consumes materials from cargo to fabricate and deploy an armed antimatter mine behind the vehicle.\n\nRequired:\n{costDesc}", costDesc),
                icon = ContentFinder<Texture2D>.Get(this.Props.iconTexPath, true)
            };

            if (this.Vehicle != null && !this.Vehicle.Spawned)
            {
                cmd.Disable("Vehicle is not on map.");
            }
            else
            {
                int ticksSinceDeploy = Find.TickManager.TicksGame - this.lastDeployTick;

                if (!this.HasSufficientCosts(out List<string> missingList))
                {
                    string missingStr = string.Join(", ", missingList);
                    cmd.Disable("VTM_InsufficientCosts".TranslateOrFallback($"Insufficient materials: {missingStr}", missingStr));
                }
                else if (ticksSinceDeploy < this.Props.cooldownTicks)
                {
                    float secRemaining = (this.Props.cooldownTicks - ticksSinceDeploy) / 60f;
                    cmd.Disable("VTM_CooldownRemaining".TranslateOrFallback($"Reloading ({secRemaining:0.0}s)", secRemaining.ToString("0.0")));
                }
            }

            cmd.action = this.DeployMine;
            yield return cmd;
        }

        private void DeployMine()
        {
            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"DeployMine() triggered on {this.SafeLabel} at tick {Find.TickManager?.TicksGame}");
            }

            if (this.Vehicle == null || !this.Vehicle.Spawned)
            {
                if (TacticalModulesSettings.enableLogging)
                {
                    TacticalLog.Warning("DeployMine aborted: vehicle is null or not spawned.");
                }
                return;
            }

            if (!this.TryFindDeployCell(out IntVec3 cell))
            {
                if (TacticalModulesSettings.enableLogging)
                {
                    TacticalLog.Warning("DeployMine failed: deploy cell behind vehicle is blocked.");
                }
                Messages.Message("VTM_DeployCellBlocked".TranslateOrFallback("Deployment location (behind vehicle) is blocked."), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!this.TryConsumeCosts())
            {
                this.HasSufficientCosts(out List<string> missingList);
                string missingStr = string.Join(", ", missingList);
                if (TacticalModulesSettings.enableLogging)
                {
                    TacticalLog.Warning($"DeployMine failed: unable to consume costs. Missing: {missingStr}");
                }
                Messages.Message("VTM_InsufficientCosts".TranslateOrFallback($"Insufficient materials: {missingStr}", missingStr), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Map map = this.Vehicle.Map;
            ThingDef mineDef = this.MineDef;
            if (mineDef == null)
            {
                TacticalLog.Error("TrapIED_AntigrainWarhead Def not found in database!");
                return;
            }

            Thing mine = ThingMaker.MakeThing(mineDef);
            mine.SetFaction(this.Vehicle.Faction);
            GenSpawn.Spawn(mine, cell, map, WipeMode.Vanish);

            this.lastDeployTick = Find.TickManager.TicksGame;

            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Message($"Antimatter mine successfully deployed at cell {cell} (Faction: {mine.Faction?.Name}). Costs consumed successfully.");
            }

            // Visual effects
            FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 1.5f, new Color(0.7f, 0.7f, 0.7f));
            FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 1.2f);

            // Sound effect
            SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("DMS_MBTVehicleStop") ?? SoundDefOf.DropElement;
            sound?.PlayOneShot(new TargetInfo(cell, map));

            Messages.Message("VTM_MineDeployedSuccess".TranslateOrFallback("Antimatter mine deployed and armed."), new LookTargets(mine), MessageTypeDefOf.PositiveEvent, false);
        }

        private bool TryFindDeployCell(out IntVec3 deployCell)
        {
            deployCell = IntVec3.Invalid;
            if (this.Vehicle == null || !this.Vehicle.Spawned)
            {
                return false;
            }

            Map map = this.Vehicle.Map;
            CellRect rect = this.Vehicle.OccupiedRect();
            Rot4 rot = this.Vehicle.Rotation;

            List<IntVec3> candidates = new List<IntVec3>();

            if (rot == Rot4.North)
            {
                int z = rect.minZ - 1;
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    candidates.Add(new IntVec3(x, 0, z));
                }
                candidates.SortBy(c => Math.Abs(c.x - this.Vehicle.Position.x));
            }
            else if (rot == Rot4.South)
            {
                int z = rect.maxZ + 1;
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    candidates.Add(new IntVec3(x, 0, z));
                }
                candidates.SortBy(c => Math.Abs(c.x - this.Vehicle.Position.x));
            }
            else if (rot == Rot4.East)
            {
                int x = rect.minX - 1;
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    candidates.Add(new IntVec3(x, 0, z));
                }
                candidates.SortBy(c => Math.Abs(c.z - this.Vehicle.Position.z));
            }
            else // West
            {
                int x = rect.maxX + 1;
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    candidates.Add(new IntVec3(x, 0, z));
                }
                candidates.SortBy(c => Math.Abs(c.z - this.Vehicle.Position.z));
            }

            foreach (IntVec3 c in candidates)
            {
                if (this.IsValidMineCell(c, map))
                {
                    deployCell = c;
                    if (TacticalModulesSettings.enableLogging)
                    {
                        TacticalLog.Message($"TryFindDeployCell selected valid cell: {c} (candidates evaluated: {candidates.Count})");
                    }
                    return true;
                }
            }

            if (TacticalModulesSettings.enableLogging)
            {
                TacticalLog.Warning($"TryFindDeployCell found no valid cell among {candidates.Count} candidates.");
            }
            return false;
        }

        private bool IsValidMineCell(IntVec3 c, Map map)
        {
            if (!c.InBounds(map) || !c.Walkable(map))
            {
                return false;
            }

            TerrainDef terrain = c.GetTerrain(map);
            if (terrain != null && (terrain.IsWater || terrain.passability == Traversability.Impassable))
            {
                return false;
            }

            // Check for blocking buildings or traps
            List<Thing> thingList = c.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing t = thingList[i];
                if (t.def.category == ThingCategory.Building || t is Building_Trap)
                {
                    return false;
                }
            }

            // Do not deploy directly under another vehicle
            Pawn p = c.GetFirstPawn(map);
            if (p is VehiclePawn)
            {
                return false;
            }

            return true;
        }

        private bool TryConsumeCosts()
        {
            if (this.Vehicle?.inventory?.innerContainer == null)
            {
                return false;
            }

            if (!this.HasSufficientCosts(out _))
            {
                return false;
            }

            List<ThingDefCountClass> costs = this.EffectiveCosts;
            for (int cIdx = 0; cIdx < costs.Count; cIdx++)
            {
                ThingDefCountClass cost = costs[cIdx];
                int remaining = cost.count;
                List<Thing> things = new List<Thing>(this.Vehicle.inventory.innerContainer);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].def == cost.thingDef)
                    {
                        int take = Math.Min(things[i].stackCount, remaining);
                        Thing taken = this.Vehicle.inventory.innerContainer.Take(things[i], take);
                        remaining -= taken.stackCount;
                        taken.Destroy(DestroyMode.Vanish);
                        if (remaining <= 0)
                        {
                            break;
                        }
                    }
                }
            }

            return true;
        }

        private bool TryConsumeMeat(int amount)
        {
            if (this.Vehicle?.inventory?.innerContainer == null || this.MeatDef == null)
            {
                return false;
            }

            int totalAvailable = this.Vehicle.inventory.innerContainer.TotalStackCountOfDef(this.MeatDef);
            if (totalAvailable < amount)
            {
                return false;
            }

            int remaining = amount;
            List<Thing> things = new List<Thing>(this.Vehicle.inventory.innerContainer);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def == this.MeatDef)
                {
                    int take = Math.Min(things[i].stackCount, remaining);
                    Thing taken = this.Vehicle.inventory.innerContainer.Take(things[i], take);
                    remaining -= taken.stackCount;
                    taken.Destroy(DestroyMode.Vanish);
                    if (remaining <= 0)
                    {
                        break;
                    }
                }
            }

            return true;
        }
    }

    public static class TranslationExtensions
    {
        public static string TranslateOrFallback(this string key, string fallback, params NamedArgument[] args)
        {
            if (key.CanTranslate())
            {
                if (args != null && args.Length > 0)
                {
                    return key.Translate().Formatted(args).ToString();
                }
                return key.Translate().ToString();
            }
            if (args != null && args.Length > 0)
            {
                object[] objArgs = new object[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    objArgs[i] = args[i].arg;
                }
                return string.Format(fallback, objArgs);
            }
            return fallback;
        }
    }
}
