using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CopyOnPlayScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "copy-on-play";

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            var all = save.GetAllGameData();
            var upgrade = all.GetAllCardUpgradeData().Single(item => item.name == "CreateSpellCopyWithPurge");
            var trait = upgrade.GetTraitDataUpgrades().Single(item => item.GetTraitStateName() == "CardTraitCopyOnPlay");
            using (var fixture = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Upgrade = Project(upgrade), Trait = CardTraitCompositionProbe.Definition(trait),
                CopyUpgrade = trait.GetCardUpgradeDataParam() == null ? null : Project(trait.GetCardUpgradeDataParam()) }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "copy-on-play-definition.mt2f")))
                fixture.Write(stream);
            foreach (bool consume in new[] { false, true })
            {
                var card = managers.GetCardManager()!.AddNewCard(all.FindCardData("3d1a2e1b-7751-4b2c-a549-77d03db715c9"),
                    CardPile.HandPile, false, false, animate: false);
                if (consume)
                {
                    var chaos = new CardUpgradeState(); chaos.Setup(all.GetAllCardUpgradeData().Single(item => item.name == "ChaosScepter"));
                    card.ApplyPermanentUpgrade(chaos, save, ignoreUpgradeAnimation: true);
                }
                var state = new CardUpgradeState(); state.Setup(upgrade);
                card.ApplyPermanentUpgrade(state, save, ignoreUpgradeAnimation: true);
                managers.GetCardManager()!.MoveCardFromHandToTopOfDeck(card);
            }
            log.LogInfo("COPY-ON-PLAY-PREPARED two explicit original Flicker carriers: plain and original ChaosScepter Consume; original copy upgrade and draw ordering.");
        }

        private static CardUpgradeModifier Project(CardUpgradeData data)
        {
            var state = new CardUpgradeState(); state.Setup(data);
            return CardModifierProbe.Upgrade(state, rejectFilters: false, filtersAlreadyApplied: true);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            return plays.FirstOrDefault(action => source.Spawn.Train.Context!.CardInstances?.Any(card =>
                    card.InstanceId == action.CardInstanceId && card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == CardCopyOnPlayModel.UpgradeId)) == true) ??
                plays.FirstOrDefault(action => source.Spawn.Train.Context!.Cards.Hand.Any(card =>
                    card.InstanceId == action.CardInstanceId && card.DataId == "3d1a2e1b-7751-4b2c-a549-77d03db715c9")) ??
                BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
