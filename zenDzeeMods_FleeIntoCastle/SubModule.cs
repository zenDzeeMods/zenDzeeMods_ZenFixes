using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace zenDzeeMods_FleeIntoCastle
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
                campaignStarter.AddBehavior(new FleeIntoCastleBehavior());
            }
        }
    }

    internal class FleeIntoCastleBehavior : CampaignBehaviorBase
    {
        private const float magnetDistance = 600;

        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private static void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddPlayerLine("clan_member_manage_troops",
                "hero_main_options",
                "lord_pretalk",
                "{=TQKXkQAT}Let me inspect your troops.",
                ConditionInspectTroops,
                ConsequenceInspectTroops,
                100, null, null);
        }

        private static bool ConditionInspectTroops()
        {
            Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
            return oneToOneConversationHero != null
                && oneToOneConversationHero.Clan != Clan.PlayerClan
                && oneToOneConversationHero.PartyBelongedTo != null
                && oneToOneConversationHero.PartyBelongedTo.LeaderHero == oneToOneConversationHero
                && oneToOneConversationHero.PartyBelongedTo.Army != null
                && oneToOneConversationHero.PartyBelongedTo.Army.LeaderParty == MobileParty.MainParty;
        }

        private static void ConsequenceInspectTroops()
        {
            PartyScreenManager.OpenScreenAsManageTroops(Hero.OneToOneConversationHero.PartyBelongedTo);
        }

        private static void OnHourlyTickParty(MobileParty mobileParty)
        {
            if (mobileParty.IsCaravan || mobileParty.IsVillager)
            {
                if (mobileParty.CurrentSettlement != null
                    && mobileParty.CurrentSettlement.IsCastle
                    && !mobileParty.CurrentSettlement.IsUnderSiege
                    && MBRandom.RandomFloat < 0.5f)
                {
                    Settlement currentSettlement = mobileParty.CurrentSettlement;
                    LeaveSettlementAction.ApplyForParty(mobileParty);
                    mobileParty.SetMoveGoToPoint(currentSettlement.GatePosition);
                    return;
                }

                if (mobileParty.DefaultBehavior == AiBehavior.GoToSettlement
                    && mobileParty.ShortTermBehavior != AiBehavior.FleeToPoint
                    && mobileParty.TargetSettlement != null
                    && mobileParty.TargetSettlement.IsUnderSiege
                    && mobileParty.CurrentSettlement == null)
                {
                    mobileParty.SetMoveGoToPoint(mobileParty.Position2D);
                    return;
                }

            }

            if (mobileParty.IsLordParty || mobileParty.IsCaravan || mobileParty.IsVillager)
            {
                if (mobileParty.ShortTermBehavior == AiBehavior.FleeToPoint && mobileParty.ShortTermTargetParty != null)
                {
                    // find nearest safe settlement
                    Settlement settlementToFlee = SettlementHelper.FindNearestSettlementToMapPoint(mobileParty,
                        s => s.IsFortification
                            && (s.MapFaction == mobileParty.MapFaction
                                || (s.MapFaction.IsKingdomFaction && !s.MapFaction.IsAtWarWith(mobileParty.MapFaction))));

                    if (settlementToFlee == null || settlementToFlee.IsUnderSiege || settlementToFlee.Party.MapEvent != null)
                    {
                        return;
                    }

                    float dist = mobileParty.Position2D.DistanceSquared(settlementToFlee.GatePosition);

                    if (dist < magnetDistance && dist < mobileParty.ShortTermTargetParty.Position2D.DistanceSquared(settlementToFlee.GatePosition))
                    {
                        mobileParty.SetMoveGoToSettlement(settlementToFlee);
                        mobileParty.RecalculateShortTermAi();
                    }
                }
            }
        }

    }
}