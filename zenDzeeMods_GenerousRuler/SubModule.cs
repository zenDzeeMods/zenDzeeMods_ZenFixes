using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace zenDzeeMods_GenerousRuler
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
                campaignStarter.AddBehavior(new GenerousRulerBehavior());
            }
        }
    }

    internal class GenerousRulerBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, ConsiderFiefGiveAway);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public static void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (!Campaign.Current.GameStarted)
            {
                return;
            }

            SettlementComponent component = settlement.GetComponent<SettlementComponent>();

            if (mobileParty != null
                && mobileParty != MobileParty.MainParty
                && mobileParty.IsLordParty
                && !mobileParty.IsDisbanding
                && mobileParty.LeaderHero != null
                && mobileParty.LeaderHero.MapFaction.IsKingdomFaction
                && FactionManager.IsAlliedWithFaction(mobileParty.MapFaction, settlement.MapFaction)
                && component.IsOwnerUnassigned
                && settlement.OwnerClan == Clan.PlayerClan
                && settlement.IsFortification)
            {
                float currentTime = Campaign.CurrentTime;
                float num = mobileParty.LeaderHero.VisitedSettlements.ContainsKey(settlement) ? mobileParty.LeaderHero.VisitedSettlements[settlement] : 0f;
                if (currentTime - num > 12f)
                {
                    BasicCharacterObject tmp = Game.Current.PlayerTroop;
                    Game.Current.PlayerTroop = mobileParty.Leader;
                    int num2 = Campaign.Current.Models.SettlementGarrisonModel.FindNumberOfTroopsToLeaveToGarrison(mobileParty, mobileParty.CurrentSettlement);
                    Game.Current.PlayerTroop = tmp;
                    if (num2 > 0)
                    {
                        LeaveTroopsToSettlementAction.Apply(mobileParty, settlement, num2, true);
                        return;
                    }
                }
            }
        }

        private static void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement.IsFortification
                && newOwner != capturerHero
                && detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                ChangeOwnerOfSettlementAction.ApplyBySiege(capturerHero, capturerHero, settlement);
            }
        }

        private static void ConsiderFiefGiveAway(Clan clan)
        {
            if (clan == Clan.PlayerClan || clan.Kingdom == null || clan.Kingdom.RulingClan != clan)
            {
                return;
            }

            if (clan.Settlements == null || clan.Settlements.Count(s => s.IsFortification) < 2)
            {
                return;
            }

            Kingdom kingdom = clan.Kingdom;

            if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is SettlementClaimantDecision) != null)
            {
                return;
            }

            Clan bomzh = kingdom.Clans.FirstOrDefault(c => !c.IsUnderMercenaryService
                    && c.Heroes.Count(h => h.IsAlive) > 0
                    && (c.Settlements == null || c.Settlements.Count() == 0));

            if (bomzh == null)
            {
                return;
            }

            SettlementValueModel model = Campaign.Current.Models.SettlementValueModel;

            Settlement settlement = clan.Settlements
                    .Where(s => s.IsFortification)
                    .MaxBy(s => model.CalculateValueForFaction(s, kingdom));

            ChangeOwnerOfSettlementAction.ApplyByKingDecision(bomzh.Leader, settlement);
        }
    }
}
