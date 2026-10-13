using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MagebladeScenario
    {
        internal const string Name = "mageblade";
        internal const string OriginalMagnitudeName = "original-mageblade-magnitude";
        private const int RelicRoomMagicPower = 15;
        internal static bool OriginalMagnitudeRelic => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == OriginalMagnitudeName;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name || OriginalMagnitudeRelic;
        internal static bool CaptureMagnitudeQueries => OriginalMagnitudeRelic && Prepared;
        private static bool configured;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static string? MagebladeCardId { get; private set; }
        internal static int RoomIndex { get; private set; } = -1;

        internal static void ConfigureBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (configured) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Mageblade test input must be configured before battle setup.");
            RoomIndex = 1;
            AllGameData gameData = save.GetAllGameData();
            CollectableRelicData relic = UnityEngine.Object.Instantiate(gameData.GetAllCollectableRelicData().First());
            relic.name = "PojuMagebladeMagicPowerRelic";
            AccessTools.Field(typeof(GameData), "id").SetValue(relic, "0f1d0b70-e71f-4c13-9f11-b9dafbde4232");
            var effect = new RelicEffectData();
            Set(effect, "relicEffectClassName", typeof(RelicEffectModifyCardMagicalPower).AssemblyQualifiedName!);
            Set(effect, "effectConditions", new List<RelicEffectCondition>());
            Set(effect, "paramInt", RelicRoomMagicPower);
            Set(relic, "effects", new List<RelicEffectData> { effect });
            ((IList)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(gameData)).Add(relic);
            save.AddRelic(relic);
            if (OriginalMagnitudeRelic)
            {
                CollectableRelicData original = gameData.GetAllCollectableRelicData()
                    .Single(item => item.GetID() == "622860d3-b2ca-45b0-991a-b7745e1dcd5c");
                RelicEffectData originalEffect = original.GetEffects().Single();
                StatusEffectStackData[] statuses = originalEffect.GetParamStatusEffects();
                if (original.name != "DoubleMagebladeDamage" ||
                    originalEffect.GetEffectClassName() != "RelicEffectModifyStatusMagnitude" ||
                    originalEffect.GetParamSourceTeam() != Team.Type.Monsters || originalEffect.GetParamInt() != 1 ||
                    originalEffect.GetEffectConditions().Count != 0 || statuses.Length != 1 ||
                    statuses[0].statusId != "mageblade" || statuses[0].count != 0)
                    throw new InvalidOperationException("Original DoubleMagebladeDamage definition changed.");
                save.AddRelic(original);
            }
            configured = true;
            log.LogInfo("MAGEBLADE-POWER-INPUT synthetic native RelicEffectModifyCardMagicalPower=" + RelicRoomMagicPower +
                " in isolated battle; this is not an original relic owner." +
                (OriginalMagnitudeRelic ? " Acquired unchanged original DoubleMagebladeDamage for combat evidence." : string.Empty));
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (CaptureReady) return;
            if (!configured) throw new InvalidOperationException("Mageblade setup missed pre-battle power configuration.");

            RoomManager rooms = managers.GetRoomManager()!;
            if (RoomIndex < 0 || RoomIndex >= rooms.GetNumRooms())
                throw new InvalidOperationException("Mageblade test room is outside the native room range.");
            RoomState room = rooms.GetRoom(RoomIndex);

            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData gameData = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData steward = owned.Select(card => gameData.FindCardData(card.GetCardDataID())!)
                .FirstOrDefault(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("Mageblade fixture needs an owned Train Steward card.");
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80);
            Set(unit, "attackDamage", 2);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "mageblade", count = 2 } });
            Set(steward, "cost", 0);

            foreach (CardState card in owned.Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save, setupStartingUpgrades: false);
            CaptureReady = true;
            CardState? spell = owned.FirstOrDefault(card => card.GetCardType() == CardType.Spell && card.CanScaleWithMagicPower());
            if (spell == null) throw new InvalidOperationException("Mageblade relic fixture needs an owned spell that scales with magic power.");
            managers.GetRelicManager().ApplyCardStateModifiers(spell, resetTempCardModifiers: true);
            CardState added = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Mageblade Train Steward did not enter the native hand.");

            MagebladeCardId = steward.GetID();
            int magicPower = room.GetRoomStateModifiedMagicPower(Team.Type.Monsters, managers.GetCoreManagers()) +
                managers.GetRelicManager().GetMagicPowerModification();
            if (magicPower != RelicRoomMagicPower)
                throw new InvalidOperationException("Mageblade test room did not expose the synthetic relic's native magic-power input.");
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("MAGEBLADE-PREPARED card=" + steward.name + " room=" + RoomIndex +
                " dataId=" + MagebladeCardId + " roomMagicPower=" + magicPower + " startingStacks=2; native synthetic relic.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!CaptureReady || MagebladeCardId == null || source.PlayRules == null || source.Spawn.Train.Context == null)
                return null;
            CombatContext context = source.Spawn.Train.Context;
            HashSet<int> empoweredRooms = context.MagicPower?.Where(power => power.Team == CombatTeam.Player && power.Value > 0)
                .Select(power => power.RoomIndex).ToHashSet() ?? new HashSet<int>();
            if (empoweredRooms.Count == 0) return null;

            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (CardToken card in context.Cards.Hand.Where(item => item.DataId == MagebladeCardId))
            foreach (PlayCardAction play in plays.Where(item => item.CardInstanceId == card.InstanceId && empoweredRooms.Contains(item.RoomIndex)))
                return play;
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
