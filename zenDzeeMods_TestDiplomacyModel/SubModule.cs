using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace zenDzeeMods_TestDiplomacyModel
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
                campaignStarter.AddModel(new TestDiplomacyModel());
            }
        }
    }

    internal class TestDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan)
        {
            if (Kingdom.All.Any(k => k.IsAtWarWith(factionDeclaresWar)))
            {
                return 0;
            }

            return base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
        }
    }
}
