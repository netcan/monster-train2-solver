using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CopyOnPlayTerminalScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_COPY_ON_PLAY_TERMINAL") == "1";
        internal static bool Consume => Environment.GetEnvironmentVariable("MT2_PROBE_COPY_ON_PLAY_TERMINAL_CONSUME") == "1";
        internal static bool PurgeCopy => Environment.GetEnvironmentVariable("MT2_PROBE_COPY_ON_PLAY_TERMINAL_PURGE") == "1";
        internal static bool LethalBoss => Environment.GetEnvironmentVariable("MT2_PROBE_COPY_ON_PLAY_TERMINAL_THREAT") == "1";
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            AccessTools.Field(typeof(CharacterData), "health").SetValue(steward.GetSpawnCharacterData(), 2000);
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 20);
            if (PurgeCopy)
                AccessTools.Field(typeof(CharacterData), "health").SetValue(
                    all.GetAllCharacterData().Single(unit => unit.GetAssetKey() == "Boss_Level1_Basic"), 2000);
            if (LethalBoss)
                AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(
                    all.GetAllCharacterData().Single(unit => unit.GetAssetKey() == "Boss_Level1_Basic"), 2000);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            var first = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            var second = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            var spell = cards.AddNewCard(all.FindCardData("3d1a2e1b-7751-4b2c-a549-77d03db715c9")!, CardPile.HandPile, false, false, animate: false);
            if (Consume)
            {
                var consume = new CardUpgradeState(); consume.Setup(all.GetAllCardUpgradeData().Single(item => item.name == "ChaosScepter"));
                spell.ApplyPermanentUpgrade(consume, save, ignoreUpgradeAnimation: true);
            }
            var copy = new CardUpgradeState(); copy.Setup(all.GetAllCardUpgradeData().Single(item => item.name == "CreateSpellCopyWithPurge"));
            spell.ApplyPermanentUpgrade(copy, save, ignoreUpgradeAnimation: true);
            var damage = new CardUpgradeState(); damage.Setup(); damage.SetAttackDamage(1000);
            spell.ApplyPermanentUpgrade(damage, save, ignoreUpgradeAnimation: true);
            var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); spell.AddTemporaryTrait(freeze, managers.GetCoreManagers());
            CardIds = new[] { first, second, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            log.LogInfo("COPY-ON-PLAY-TERMINAL-PREPARED original copy upgrade, frozen original Flicker, authored +1000 power/durable Stewards; Consume=" + Consume + " PurgeCopy=" + PurgeCopy + " LethalBoss=" + LethalBoss);
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            foreach (var room in state.Spawn.Train.Rooms)
                if (room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss == true && unit.Health > 0))
                {
                    CombatContext context = state.Spawn.Train.Context!;
                    if (context.Cards.Hand.Any(card => card.InstanceId == CardIds[2])) return new PlayCardAction(CardIds[2], room.RoomIndex);
                    CardToken? copy = context.Cards.Hand.FirstOrDefault(card => context.CardInstances!.Any(instance =>
                        instance.InstanceId == card.InstanceId && instance.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "CopyOnPlayTraitUpgrade")));
                    return copy == null ? null : new PlayCardAction(copy.InstanceId, room.RoomIndex);
                }
            for (int index = 0; index < 2; index++)
                if (state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == CardIds[index]))
                    return new PlayCardAction(CardIds[index], index, playerPosition: 0);
            return null;
        }
    }
}
