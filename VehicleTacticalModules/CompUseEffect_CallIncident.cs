using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VehicleTacticalModules
{
    public class CompUseEffect_CallIncident : CompUseEffect
    {
        public CompProperties_UseEffect_CallIncident Props => (CompProperties_UseEffect_CallIncident)this.props;

        public override TaggedString ConfirmMessage(Pawn p)
        {
            if (!string.IsNullOrEmpty(this.Props.confirmMessageKey))
            {
                return this.Props.confirmMessageKey.Translate();
            }
            return null;
        }

        private AcceptanceReport CheckCooldown()
        {
            int cooldownTicks = this.Props.CooldownTicks;
            if (cooldownTicks > 0 && this.Props.incident != null && Current.Game != null)
            {
                var comp = Current.Game.GetComponent<GameComponent_VehicleTacticalModules>();
                if (comp != null)
                {
                    int lastTick = comp.GetLastCallTick(this.Props.incident.defName);
                    int elapsed = Find.TickManager.TicksGame - lastTick;
                    if (lastTick >= 0 && elapsed >= 0 && elapsed < cooldownTicks)
                    {
                        int remainingTicks = cooldownTicks - elapsed;
                        return "VRF_CallerBeacon_Cooldown".Translate(remainingTicks.ToStringTicksToPeriod(true, false, true, true, false));
                    }
                }
            }
            return true;
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (this.Props.incident == null)
            {
                return false;
            }

            Map map = p.MapHeld ?? this.parent.MapHeld;
            if (map == null)
            {
                return "VRF_CallerBeacon_NoMap".Translate();
            }

            if (this.Props.requirePlayerHome && !map.IsPlayerHome)
            {
                return "VRF_CallerBeacon_RequiresPlayerHome".Translate();
            }

            AcceptanceReport cooldownReport = this.CheckCooldown();
            if (!cooldownReport.Accepted)
            {
                return cooldownReport;
            }

            if (this.Props.checkCanFireNow)
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(this.Props.incident.category, map);
                parms.forced = true;
                parms.target = map;
                if (this.Props.fixedPoints > 0f)
                {
                    parms.points = this.Props.fixedPoints;
                }

                if (!this.Props.incident.Worker.CanFireNow(parms))
                {
                    return "VRF_CallerBeacon_CannotFireNow".Translate();
                }
            }

            return true;
        }

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (this.Props.incident == null)
            {
                Log.Error("[VehicleTacticalModules] CompUseEffect_CallIncident: incident is null.");
                return;
            }

            Map map = usedBy.MapHeld ?? this.parent.MapHeld;
            if (map == null)
            {
                Messages.Message("VRF_CallerBeacon_NoMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (this.Props.requirePlayerHome && !map.IsPlayerHome)
            {
                Messages.Message("VRF_CallerBeacon_RequiresPlayerHome".Translate(), this.parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            AcceptanceReport cooldownReport = this.CheckCooldown();
            if (!cooldownReport.Accepted)
            {
                Messages.Message(cooldownReport.Reason, this.parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(this.Props.incident.category, map);
            parms.forced = true;
            parms.target = map;
            if (this.Props.fixedPoints > 0f)
            {
                parms.points = this.Props.fixedPoints;
            }

            bool success = false;
            try
            {
                success = this.Props.incident.Worker.TryExecute(parms);
            }
            catch (Exception ex)
            {
                Log.Error($"[VehicleTacticalModules] Exception executing incident {this.Props.incident.defName}: {ex}");
            }

            if (success)
            {
                var comp = Current.Game?.GetComponent<GameComponent_VehicleTacticalModules>();
                if (comp != null && this.Props.incident != null)
                {
                    comp.SetLastCallTick(this.Props.incident.defName, Find.TickManager.TicksGame);
                }

                if (this.Props.sound != null)
                {
                    this.Props.sound.PlayOneShot(new TargetInfo(this.parent.PositionHeld, map, false));
                }

                if (this.Props.consumeOnSuccess)
                {
                    this.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
                }
            }
            else
            {
                Messages.Message("VRF_CallerBeacon_FailedToCall".Translate(), this.parent, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
