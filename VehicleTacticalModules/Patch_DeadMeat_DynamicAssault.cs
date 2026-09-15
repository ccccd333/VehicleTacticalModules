using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace VehicleTacticalModules
{
    public static class Patch_DeadMeat_DynamicAssault
    {
        public const string DeadMeatDefName = "DMS_Chelydridae_MainBattleTank_Miho_Faction_Supremacist_DM_NPC";

        private static MethodInfo findNearestEnemyMethod;
        private static MethodInfo findPositionAtIdealRangeMethod;
        private static MethodInfo findNearestWallTowardEnemyMethod;
        private static MethodInfo findBestBreachTargetMethod;

        public static bool Prefix(ThinkNode __instance, Pawn pawn, ref Job __result)
        {
            // Guard: ONLY target DeadMeat NPC vehicle
            VehiclePawn vehicle = pawn as VehiclePawn;
            if (vehicle == null || vehicle.def == null || vehicle.def.defName != DeadMeatDefName)
            {
                return true; // Let VRF handle all other vehicles
            }

            if (vehicle.Map == null || vehicle.Dead || vehicle.Downed)
            {
                return true;
            }

            // If already moving on a Goto job, do not interrupt
            if (vehicle.CurJobDef == JobDefOf.Goto && vehicle.vehiclePather != null && vehicle.vehiclePather.Moving)
            {
                __result = null;
                return false;
            }

            // Find enemy target
            Thing enemy = FindEnemy(__instance, vehicle);
            if (enemy == null)
            {
                __result = null;
                return false;
            }
            vehicle.mindState.enemyTarget = enemy;

            // Turret ranges
            CompVehicleTurrets turrets = vehicle.CompVehicleTurrets;
            float maxRange = (turrets != null && turrets.MaxRange > 0f) ? turrets.MaxRange : 42.1f;
            float minRange = (turrets != null) ? turrets.MinRange : 1.0f;
            float dist = vehicle.Position.DistanceTo(enemy.Position);

            // Calculate angle to enemy
            float angleToEnemy = (enemy.Position - vehicle.Position).AngleFlat;
            Rot4 enemyFacing = Rot4.FromAngleFlat(angleToEnemy);

            // Too close: retreat or reposition
            if (dist < minRange + 1f && minRange > 0f)
            {
                float idealRange = Mathf.Clamp(maxRange * 0.75f, minRange + 3f, maxRange - 3f);
                IntVec3 retreatPos = CallFindPositionAtIdealRange(__instance, vehicle, enemy, idealRange, maxRange, minRange, true, 0f, true);
                if (retreatPos.IsValid && retreatPos != vehicle.Position)
                {
                    Job retreatJob = JobMaker.MakeJob(JobDefOf.Goto, retreatPos);
                    retreatJob.expiryInterval = 1200;
                    retreatJob.checkOverrideOnExpire = true;
                    __result = retreatJob;
                    return false;
                }
            }

            // Check if enemy is in range and line of sight is clear
            bool inRange = dist >= minRange && dist <= maxRange;
            bool hasLoS = GenSight.LineOfSight(vehicle.Position, enemy.Position, vehicle.Map, true);

            if (inRange && hasLoS)
            {
                // CRITICAL FIX: Direct fire Wait_Combat job with sufficient duration for DeadMeat's warm-up (240 ticks) + burst (180 ticks)
                Job attackJob = JobMaker.MakeJob(JobDefOf.Wait_Combat, 500, true);
                attackJob.overrideFacing = enemyFacing;
                attackJob.checkOverrideOnExpire = true;
                __result = attackJob;

                if (TacticalModulesSettings.enableLogging)
                {
                    TacticalLog.Message($"[DeadMeat_AI] Direct assault firing stance at {enemy.LabelShortCap} (dist={dist:F1}, facing={enemyFacing}). Expiry=500 ticks.");
                }
                return false; // Handled, skip VRF's CanReachVehicle wall breaching check
            }

            // If not in range or no line of sight, find a firing position where LoS to enemy exists
            float targetIdealRange = Mathf.Clamp(maxRange * 0.75f, minRange + 3f, maxRange - 3f);
            IntVec3 firingPos = CallFindPositionAtIdealRange(__instance, vehicle, enemy, targetIdealRange, maxRange, minRange, true, 0f, false);
            if (firingPos.IsValid && firingPos != vehicle.Position)
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, firingPos);
                moveJob.expiryInterval = 1200;
                moveJob.checkOverrideOnExpire = true;
                __result = moveJob;

                if (TacticalModulesSettings.enableLogging)
                {
                    TacticalLog.Message($"[DeadMeat_AI] Moving to firing position {firingPos} for {enemy.LabelShortCap}.");
                }
                return false;
            }

            // If completely blocked by walls with no reachable firing position, fall back to breaching
            Thing wall = CallFindNearestWallTowardEnemy(__instance, vehicle, enemy);
            if (wall != null)
            {
                int width = Mathf.Max(vehicle.def.size.x, 1);
                Thing breachTarget = CallFindBestBreachTarget(__instance, vehicle, wall, width) ?? wall;
                vehicle.mindState.breachingTarget = new BreachingTargetData(breachTarget, vehicle.Position);

                float wallDist = vehicle.Position.DistanceTo(breachTarget.Position);
                if (wallDist >= minRange && wallDist <= maxRange && GenSight.LineOfSight(vehicle.Position, breachTarget.Position, vehicle.Map, true))
                {
                    Job breachWaitJob = JobMaker.MakeJob(JobDefOf.Wait_Combat, 500, true);
                    breachWaitJob.overrideFacing = Rot4.FromAngleFlat((breachTarget.Position - vehicle.Position).AngleFlat);
                    breachWaitJob.checkOverrideOnExpire = true;
                    __result = breachWaitJob;

                    if (TacticalModulesSettings.enableLogging)
                    {
                        TacticalLog.Message($"[DeadMeat_AI] Breaching wall {breachTarget.LabelCap} at {breachTarget.Position} with 500-tick Wait_Combat.");
                    }
                    return false;
                }
                else
                {
                    IntVec3 wallPos = CallFindPositionAtIdealRange(__instance, vehicle, breachTarget, targetIdealRange, maxRange, minRange, false, 0f, false);
                    if (wallPos.IsValid && wallPos != vehicle.Position)
                    {
                        Job moveWallJob = JobMaker.MakeJob(JobDefOf.Goto, wallPos);
                        moveWallJob.expiryInterval = 1200;
                        moveWallJob.checkOverrideOnExpire = true;
                        __result = moveWallJob;
                        return false;
                    }
                }
            }

            // If all else fails, let VRF's original method handle it
            return true;
        }

        private static Thing FindEnemy(ThinkNode instance, VehiclePawn vehicle)
        {
            try
            {
                if (findNearestEnemyMethod == null)
                {
                    findNearestEnemyMethod = AccessTools.Method(instance.GetType(), "FindNearestEnemy", new Type[] { typeof(VehiclePawn) });
                }
                if (findNearestEnemyMethod != null)
                {
                    return findNearestEnemyMethod.Invoke(instance, new object[] { vehicle }) as Thing;
                }
            }
            catch (Exception ex)
            {
                TacticalLog.Warning($"[DeadMeat_AI] Error invoking FindNearestEnemy: {ex.Message}");
            }

            // Fallback search in hostile targets cache
            Thing bestThing = null;
            float bestDistSq = float.MaxValue;
            var targets = vehicle.Map?.attackTargetsCache?.TargetsHostileToFaction(vehicle.Faction);
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    Thing t = target.Thing;
                    if (t == null || t.Destroyed || t.Map == null || t.Map.fogGrid.IsFogged(t.Position)) continue;
                    if (t is Pawn p && (p.Dead || p.Downed)) continue;

                    float d = (t.Position - vehicle.Position).LengthHorizontalSquared;
                    if (d < bestDistSq)
                    {
                        bestDistSq = d;
                        bestThing = t;
                    }
                }
            }
            return bestThing;
        }

        private static IntVec3 CallFindPositionAtIdealRange(ThinkNode instance, VehiclePawn vehicle, Thing target, float idealRange, float maxRange, float minRange, bool hasRestrictedTurrets, float idealTurretAngle, bool isRetreating)
        {
            try
            {
                if (findPositionAtIdealRangeMethod == null)
                {
                    findPositionAtIdealRangeMethod = AccessTools.Method(instance.GetType(), "FindPositionAtIdealRange");
                }
                if (findPositionAtIdealRangeMethod != null)
                {
                    return (IntVec3)findPositionAtIdealRangeMethod.Invoke(instance, new object[] { vehicle, target, idealRange, maxRange, minRange, hasRestrictedTurrets, idealTurretAngle, isRetreating });
                }
            }
            catch (Exception ex)
            {
                TacticalLog.Warning($"[DeadMeat_AI] Error invoking FindPositionAtIdealRange: {ex.Message}");
            }
            return IntVec3.Invalid;
        }

        private static Thing CallFindNearestWallTowardEnemy(ThinkNode instance, VehiclePawn vehicle, Thing enemy)
        {
            try
            {
                if (findNearestWallTowardEnemyMethod == null)
                {
                    findNearestWallTowardEnemyMethod = AccessTools.Method(instance.GetType(), "FindNearestWallTowardEnemy");
                }
                if (findNearestWallTowardEnemyMethod != null)
                {
                    return findNearestWallTowardEnemyMethod.Invoke(instance, new object[] { vehicle, enemy }) as Thing;
                }
            }
            catch (Exception ex)
            {
                TacticalLog.Warning($"[DeadMeat_AI] Error invoking FindNearestWallTowardEnemy: {ex.Message}");
            }

            // Fallback: check cells on line of sight
            foreach (IntVec3 c in GenSight.PointsOnLineOfSight(vehicle.Position, enemy.Position))
            {
                Building edifice = c.GetEdifice(vehicle.Map);
                if (edifice != null && !edifice.Destroyed && edifice.def.useHitPoints && (edifice.def.fillPercent > 0.5f || edifice is Building_Door))
                {
                    return edifice;
                }
            }
            return null;
        }

        private static Thing CallFindBestBreachTarget(ThinkNode instance, VehiclePawn vehicle, Thing mainWall, int vehicleWidth)
        {
            try
            {
                if (findBestBreachTargetMethod == null)
                {
                    findBestBreachTargetMethod = AccessTools.Method(instance.GetType(), "FindBestBreachTarget");
                }
                if (findBestBreachTargetMethod != null)
                {
                    return findBestBreachTargetMethod.Invoke(instance, new object[] { vehicle, mainWall, vehicleWidth }) as Thing;
                }
            }
            catch (Exception ex)
            {
                TacticalLog.Warning($"[DeadMeat_AI] Error invoking FindBestBreachTarget: {ex.Message}");
            }
            return mainWall;
        }
    }
}
