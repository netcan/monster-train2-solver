using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSupportSpellsScenario
    {
        internal static bool CapturesTriggeredSummons { get; private set; }
        internal static bool CapturesBump { get; private set; }
        internal const string Name = "original-support-spells";
        internal const string IsolatedPrefix = "original-isolated-spells:";
        internal static string[]? IsolatedIds => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is string scenario &&
            scenario.StartsWith(IsolatedPrefix, StringComparison.Ordinal) ? scenario.Substring(IsolatedPrefix.Length).Split(',') : null;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name || IsolatedIds != null;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            if (IsolatedIds is string[] isolated) { PrepareIsolated(managers, log, isolated); return; }
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30); save.SetDragonsHoardCap(4); save.SetDragonsHoardAmount(3);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Control(int suffix, string effectName, int value, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuSupportControl" + suffix; Set(data, "id", "ac110052-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                if (effectName == "CardEffectAdjustCorruption") Set(effect, "paramBool", true);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Control(1, "CardEffectGainEnergy", 30, Team.Type.None), wound = Control(2, "CardEffectDamage", 30, Team.Type.Monsters),
                health = Control(3, "CardEffectBuffMaxHealth", 200, Team.Type.Heroes), corruption = Control(4, "CardEffectAdjustCorruption", 3, Team.Type.None),
                pyre = Control(5, "CardEffectDamage", 10, Team.Type.None);
            pyre.GetEffects()[0].Cheat_SetTargetMode(TargetMode.Pyre);
            CardData hit = Control(6, "CardEffectDamage", 50, Team.Type.Monsters), piercing = Control(7, "CardEffectDamage", 50, Team.Type.Monsters),
                heal = Control(8, "CardEffectHeal", 15, Team.Type.Monsters);
            heal.GetEffects()[0].Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            hit.GetEffects()[0].Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            piercing.GetEffects()[0].Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            var ignoreArmor = new CardTraitData(); ignoreArmor.Setup("CardTraitIgnoreArmor"); Set(piercing, "traits", new List<CardTraitData> { ignoreArmor });
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(wound.GetID()), Add(health.GetID()), Add(corruption.GetID()), Add(pyre.GetID()),
                Add("8ccdb317-e3e3-47b1-95c0-662ffe3fb7c6"), Add("2ff4ef73-00f4-4bd5-b868-607320188980"), Add("acc303db-30ea-4ff1-8e46-97344210cb6b"),
                Add("6534ff5b-d036-4b2f-ac21-70ee4d68700e"), Add("24e4729c-f256-4524-84c8-99564affb42c"), Add("f25d8b79-056a-4db3-940d-c6cfc540bc4d"),
                Add("4ca4ed47-9867-4004-acf0-171c3282751f"), Add(heal.GetID()), Add("297c2618-1a10-4367-8b7c-d068a6c5b3f4"),
                Add("3c96e9b7-bdfb-4784-88f7-4b01bd58b4b7"), Add("1754dece-cf0b-455e-b9ca-d238134d7762"), Add("c354dd2c-0087-4eae-82d9-d4ab17b15f19"),
                Add("996a0953-168a-44b9-bb90-5e46cbbb3e09"), Add("7f3dcb62-e418-4120-861c-02f8372173ce"), Add("7f3dcb62-e418-4120-861c-02f8372173ce"),
                Add(piercing.GetID()), Add(hit.GetID()), Add(hit.GetID()), Add(hit.GetID()) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SUPPORT-SPELLS-PREPARED thirteen unchanged originals; wounded/full-health controls, Conduit, permanent Corruption, Hoard healing and positive/zero X capacity.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 && IsolatedIds?.FirstOrDefault() == "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a" &&
                IsolatedIds.Contains("6a3633b6-4ecc-43b1-bb56-548f56cf1d60") &&
                source.Spawn.Train.Context!.Statistics!.Value(ids[^1], "TimesPlayed", "ThisBattle") == 1)
                index = ids.Length - 1; // Recast the actual returned recipient after the original choice.
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            if (IsolatedIds?.FirstOrDefault() == "93759003-bb97-44e4-9ef6-472c55bee2d9" && index >= 4 &&
                source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "6bc6afc8-a113-4f03-b090-d4436f41d822" && source.Spawn.Turn < 2)
                return null; // Observe Rage callbacks before replacing their originating attachment.
            if (IsolatedIds != null)
            {
                string? replayKind = source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId)
                    .Effects.FirstOrDefault(effect => effect.Type == "PlayUnitTrigger")?.UnitTriggerKind;
            IEnumerable<PlayCardAction> candidates = index < 4 ? Array.Empty<PlayCardAction>() : BattleActionModel.EnumerateSupportedPlays(source, ids[index]);
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == RoomPreCombatProbe.SirenSongId)
                candidates = candidates.Where(action => action.RoomIndex == 2);
            if (index >= 4 && IsolatedIds.Contains("02258ff7-457e-4de6-88cf-faff348801d7"))
            {
                string played = source.Spawn.Train.Context!.FindCard(ids[index])!.DataId;
                int room = played == "5656f5dd-e400-4e19-96d3-c9415820a4bc" ? 1 : 0;
                candidates = candidates.Where(action => action.RoomIndex == room);
                if (source.PlayRules.Cards.Single(rule => rule.DataId == played).SpawnUnit == null)
                    candidates = candidates.Where(action => source.Spawn.Train.Rooms.SelectMany(floor => floor.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.SpawnerCardId == ids[room]));
            }
            if (index >= 4 && IsolatedIds.Contains("00f55f64-c5c1-4d5e-9f59-f50413341c1e"))
            {
                string played = source.Spawn.Train.Context!.FindCard(ids[index])!.DataId;
                candidates = candidates.Where(action => action.RoomIndex == (played == "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a" ? 1 : 0));
                if (played is "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b" or "d81f1f43-f20e-4459-9fc4-19e5836a876f" or "5b2ba535-54b5-4f42-86e4-9b2ff2e07563")
                    candidates = candidates.Where(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.SpawnerCardId == ids[0]));
            }
            if (index >= 4 && IsolatedIds.Contains("d255dfe4-3fa7-4a47-873a-84d441889544") &&
                source.PlayRules.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).SpawnUnit != null)
                candidates = candidates.Where(action => action.RoomIndex ==
                    (source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "0347419a-2e3a-492d-a054-f6e340be3db2" ? 1 : 0));
            if (index >= 4 && IsolatedIds.Contains("83734ffd-cbaa-47f1-9809-cc4c4dac568d") &&
                source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "63a48bad-07da-4687-bfef-2f7c6653af5e")
                candidates = candidates.Where(action => action.RoomIndex == 0);
            if (index >= 4 && IsolatedIds.Contains("f73bc157-ef23-4641-a938-ecd31945ca28") &&
                source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "63a48bad-07da-4687-bfef-2f7c6653af5e")
                candidates = candidates.OrderByDescending(action => action.RoomIndex == 0 && action.PlayerPosition == 1);
            if (index >= 4 && IsolatedIds.Contains("5d34a8d1-be5c-44e0-bd7e-b6cd576df0bb") &&
                source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "28875721-10df-4427-b9ef-4d237e89608e")
                candidates = candidates.OrderByDescending(action => action.RoomIndex == 0 && source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Player));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId is
                "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0" or "175a7730-7a1c-4da1-97b8-d799e7570396" or "98276338-b18f-4ffa-83a2-9501c2741f03" &&
                ids.Any(id => source.Spawn.Train.Context.FindCard(id)?.DataId is "3cb0976c-7cf5-4470-bb5a-7eacfb404e9a" or "1362aa8c-d7b4-49b9-a4f7-268f47de4aa6" or "a5b05b40-45be-41ea-8c56-dd18f3feb1b4"))
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Player && unit.Modifiers?.RoomStatuses?.Count > 0));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId is "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0" or "175a7730-7a1c-4da1-97b8-d799e7570396" &&
                ids.Any(id => source.Spawn.Train.Context.FindCard(id)?.DataId == "acb9a1cb-f6fc-40df-b287-fc82b8df156b"))
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && source.Spawn.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "acb9a1cb-f6fc-40df-b287-fc82b8df156b"));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "25787bff-d2fb-4d9a-a636-d96a5863d559" &&
                ids.Any(id => source.Spawn.Train.Context.FindCard(id)?.DataId == "f892b119-fa6a-4f69-9369-10e29bcfd96e"))
                candidates = candidates.OrderByDescending(action => action.RoomIndex == 1);
                if (replayKind != null) candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Triggers.Any(trigger => trigger.Kind == replayKind)));
                if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "6a3633b6-4ecc-43b1-bb56-548f56cf1d60")
                    candidates = candidates.OrderByDescending(action => action.RecursionSelections.Any(selection =>
                        selection.CardInstanceIds.Any(id => source.Spawn.Train.Context!.FindCard(id)?.DataId == "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a")));
                if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "f3406a6d-69f0-46e8-8db9-d829db017716")
                    candidates = candidates.OrderByDescending(action => action.RoomIndex == 2 && action.PlayerPosition == 1);
                if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "ae114dc4-d08f-4909-a2ad-d99215661463")
                    candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy && unit.Health + (unit.Status("armor")?.Stacks ?? 0) <= 8));
                if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "f29d063b-cb41-44ee-8118-4db66b6f8195")
                    candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy && unit.Health <= 5));
                if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "cc374cb3-1675-4deb-93bc-53e30b2045fe")
                    candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.Health + (unit.Status("armor")?.Stacks ?? 0) <= 10 &&
                            (IsolatedIds.Contains("0c8ff108-478c-4c1c-9c49-f6a297b4e0d8")
                                ? unit.Team == CombatTeam.Player && unit.Status("horde")?.Stacks > 0 : unit.Team == CombatTeam.Enemy)));
                if (index >= 4 && source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).AssetKey == "ScrapForParts")
                    candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.EquipmentCards?.Count > 0));
                if (index >= 4 && IsolatedIds.Contains("75ce6859-dd29-4c88-a67a-5159a753f2db") &&
                    source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).AssetKey is "RitualOfBattle" or "ScrapForParts")
                    candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.AssetKey == "MonsterBalmabello"));
                if (index >= 4 && IsolatedIds.Contains("6a2bbbef-1282-4d28-8890-1daf0c1be50c") &&
                    source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).AssetKey == "HiddenPassage")
                    candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Any(unit => unit.Id == action.TargetUnitId && unit.AssetKey == "MonsterDeathsDancer"));
                PlayCardAction? chosen = index < 4 ? new PlayCardAction(ids[index], index < 3 ? index : 0,
                    playerPosition: index < 3 ? 0 : -1) : candidates.FirstOrDefault();
                if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "6a3633b6-4ecc-43b1-bb56-548f56cf1d60")
                    UnityEngine.Debug.Log("ORIGINAL-CORRUPT-CHOICE hand=" + string.Join(",", source.Spawn.Train.Context.Cards.Hand.Select(card => card.InstanceId)) +
                        " discard=" + string.Join(",", source.Spawn.Train.Context.Cards.Discard.Select(card => card.InstanceId)) +
                        " draw=" + string.Join(",", source.Spawn.Train.Context.Cards.Draw.Select(card => card.InstanceId)) +
                        " selected=" + string.Join(",", chosen?.RecursionSelections.SelectMany(selection => selection.CardInstanceIds) ?? Array.Empty<int>()));
                BattleActionResult? isolatedResult = chosen == null ? null : BattleActionModel.PlayCard(source, chosen);
                if (isolatedResult?.Supported == true) return chosen;
                string rejection = index + ":" + source.Spawn.Train.Context!.FindCard(ids[index])!.DataId;
                if (lastRejection != rejection)
                { lastRejection = rejection; logger?.LogInfo("ORIGINAL-ISOLATED-SPELL-REJECT index=" + index + " dataId=" + source.Spawn.Train.Context.FindCard(ids[index])!.DataId +
                    " reason=" + (isolatedResult?.Reason ?? string.Join("; ", BattleActionModel.EnumerateActions(source, ids[index]).UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()) +
                        " default=" + BattleActionModel.PlayCard(source, new PlayCardAction(ids[index], 0)).Reason)); }
                return null;
            }
            int target = index is >= 8 and <= 16 or >= 23 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            var action = new PlayCardAction(ids[index], index is 2 or 22 ? 1 : 0, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-SUPPORT-SPELLS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void PrepareIsolated(AllGameManagers managers, ManualLogSource log, string[] spellIds)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            if (spellIds.Contains("ff002cdc-e448-4004-b445-0ed380808911"))
            {
                save.SetForgePoints(0); save.SetForgeToggleActive(true);
                Set(managers.GetPlayerManager(), "isForgeTrackingActive", true);
            }
            if (spellIds.Contains("e8f47c5d-c2d8-44e8-a443-4dca81acfa4d")) save.SetGold(250, isTotalReset: true);
            if (spellIds.Contains("e90fc0e0-b383-48a9-bb10-d8aaa1518531") || spellIds.Contains("f3406a6d-69f0-46e8-8db9-d829db017716"))
                save.Cheat_SetTowerHP(save.GetMaxTowerHP() - 10);
            if (spellIds.Length == 0 || spellIds.Length > 30 || spellIds.Any(id => all.FindCardData(id)?.GetCardType() is not (CardType.Spell or CardType.Equipment or CardType.Monster or CardType.Junk or CardType.TrainRoomAttachment)))
                throw new InvalidOperationException("Isolated spell IDs must name one to thirty native playable card definitions.");
            foreach (string id in spellIds)
                foreach (CardEffectData effect in all.FindCardData(id)!.GetEffects())
                    log.LogInfo("ORIGINAL-ISOLATED-EFFECT id=" + id + " type=" + effect.GetEffectStateName() +
                        " target=" + effect.GetTargetMode() + " subtype=" + effect.GetParamSubtype().Key +
                        " status=" + effect.GetUseStatusEffectStackMultiplier() + " health=" + effect.GetUseHealthMissingStackMultiplier() +
                        " magic=" + effect.GetUseMagicPowerMultiplier());
            CapturesTriggeredSummons = spellIds.Any(id => all.FindCardData(id)!.GetSpawnCharacterData()?.GetTriggers()
                .Any(trigger => trigger.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster")) == true) ||
                spellIds.Contains("e1a39d88-4a77-4399-98b2-0b81f44fe6a8");
            CapturesBump = spellIds.Contains(RoomPreCombatProbe.SirenSongId) || spellIds.Any(id => all.FindCardData(id)!.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectBump"));
            bool hatchControls = spellIds.Contains("27b0fdda-5dc4-4280-a510-fe51e550e3fb") ||
                spellIds.Contains("07448181-5b6f-404a-88ff-cb4d8ac8a3b1");
            if (hatchControls || spellIds.Contains("a524d58a-6b12-4ec4-870c-679837e234cf") || spellIds.Contains("ae114dc4-d08f-4909-a2ad-d99215661463"))
                for (int roomIndex = 0; roomIndex < 3; roomIndex++)
                {
                    RoomState room = managers.GetRoomManager()!.GetRoom(roomIndex);
                    var corruption = room.AdjustCorruption(managers.GetCombatManager()!, managers.GetRelicManager(),
                        hatchControls ? 4 : 3, 4, skipTriggersAndNotifications: true);
                    while (corruption.MoveNext()) { }
                    if (hatchControls) room.SetPermanentCorruption(4);
                }
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 10000); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData energy = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            energy.name = "PojuIsolatedSpellEnergy"; Set(energy, "id", "ac110052-0000-4000-8000-000000000099");
            Set(energy, "cost", 0); Set(energy, "costType", CardData.CostType.Default); Set(energy, "targetless", true); Set(energy, "targetsRoom", true);
            var energyTraits = new List<CardTraitData>();
            if (spellIds.Contains("6a3633b6-4ecc-43b1-bb56-548f56cf1d60"))
            {
                var consume = new CardTraitData(); consume.Setup("CardTraitExhaustState"); energyTraits.Add(consume);
            }
            Set(energy, "traits", energyTraits); Set(energy, "startingUpgrades", new List<CardUpgradeData>());
            Set(energy, "triggers", new List<CardTriggerEffectData>()); Set(energy, "effectTriggers", new List<CharacterTriggerData>());
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room);
            Set(gain, "paramInt", Math.Max(1, spellIds.Sum(id => all.FindCardData(id)!.GetCost()) - 3));
            Set(energy, "effects", new List<CardEffectData> { gain });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(energy);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(energy.GetID()) }
                .Concat(spellIds.Select(Add)).ToArray();
            if (spellIds.Contains(RoomPreCombatProbe.SirenSongId))
            {
                // Keep lower-floor enemies alive long enough to reach the native
                // Limited Range attachment. The unchanged boss still dies promptly.
                CardUpgradeData control = DynamicUpgradeScenario.Upgrade("PojuSirenLowerFloorControl",
                    "ac110052-0000-4000-8000-000000000098", -990, 0, 0, 0, "", 0);
                Set(control, "statusEffectUpgrades", new List<StatusEffectStackData>());
                ((List<CardUpgradeData>)AccessTools.Field(typeof(AllGameData), "cardUpgradeDatas").GetValue(all)).Add(control);
                foreach (CardState card in created.Take(2))
                    if (!CardUpgradeHelper.TryApplyUpgradeToCard(card, null, control, true, false, managers.GetCoreManagers(), out _))
                        throw new InvalidOperationException("Siren Song lower-floor control upgrade rejected.");
            }
            if (spellIds.Contains("93fd40da-054d-4cc2-a8b3-da77e619a1d0") && spellIds.Contains("63a48bad-07da-4687-bfef-2f7c6653af5e"))
                foreach (string id in new[] { "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b", "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b", "63a48bad-07da-4687-bfef-2f7c6653af5e" })
                {
                    CardState exhausted = cards.AddNewCard(all.FindCardData(id)!, CardPile.DiscardPile, false, false, animate: false);
                    cards.GetDiscardPile().Remove(exhausted); cards.GetExhaustedPile().Add(exhausted);
                }
            if (spellIds.Contains("e221f8ac-7b5f-4485-a4d1-dedd22f61466") && spellIds.Contains("63a48bad-07da-4687-bfef-2f7c6653af5e"))
            {
                for (int equipment = 0; equipment < 2; equipment++)
                    cards.AddNewCard(all.FindCardData("66cbbd14-f5f3-44a1-bd1c-ce98f3c13584")!, CardPile.DeckPileTop, false, false, animate: false);
                for (int spellControl = 0; spellControl < 14; spellControl++)
                    cards.AddNewCard(all.FindCardData("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")!, CardPile.DeckPileTop, false, false, animate: false);
            }
            foreach (string damageGrowth in spellIds.Where(id => id is "3f974014-72f2-407e-a8c1-28ba5b273cfa" or "f29d063b-cb41-44ee-8118-4db66b6f8195"))
                Add(damageGrowth); // Same-definition, unplayed exclusion control for source-only growth.
            foreach (string ephemeral in spellIds.Where(id => all.FindCardData(id)!.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitEphemeral")))
            {
                CardState control = Add(ephemeral); // Ephemeral must purge even when temporarily frozen.
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); control.AddTemporaryTrait(freeze, managers.GetCoreManagers());
            }
            if (spellIds.Any(id => id is "6bbebe9d-a185-412b-a2a7-4db581036ed7" or "758a0b03-3af5-4a70-82e1-4bfca40e92f0"))
            {
                Add("bae01a8c-25dd-4a08-b08a-5f17a5f3f52a"); // Native Attuned spell, retained as an independent hand control.
                Add("d81f1f43-f20e-4459-9fc4-19e5836a876f"); // Ordinary healing spell gains Attuned before numeric upgrades.
                Add("d861b6a6-d8ef-4d58-8ebd-9f0abc276456"); // Authored blacklist excludes this healing spell.
                Add(steward.GetID()); // Monster exclusion control.
                Add(energy.GetID()); // Non-Attuned spell control.
            }
            if (spellIds.Contains("a27a5954-d63a-4b7c-9cff-308aa6a91314"))
                for (int sacrificeControl = 0; sacrificeControl < 2; sacrificeControl++) Add(steward.GetID());
            if (spellIds.Any(id => all.FindCardData(id)!.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDrawType")))
                for (int drawControl = 0; drawControl < 6; drawControl++)
                    cards.AddNewCard(steward, CardPile.DeckPile, false, false, animate: false);
            if (spellIds.Any(id => all.FindCardData(id)!.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectRecursion" &&
                effect.GetTargetCardSelectionMode() == CardEffectData.CardSelectionMode.RandomToRoomUntilCapacityFull)))
                for (int recursionControl = 0; recursionControl < 6; recursionControl++)
                {
                    CardState exhausted = Add(steward.GetID());
                    cards.MoveToStandByPile(exhausted, wasPlayed: false, wasExhausted: true,
                        condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
                }
            foreach (string upgradeId in (Environment.GetEnvironmentVariable("MT2_PROBE_ISOLATED_SPELL_UPGRADES") ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                CardUpgradeData upgrade = all.FindCardUpgradeData(upgradeId) ?? throw new InvalidOperationException("Unknown isolated spell upgrade " + upgradeId);
                foreach (CardState card in created.Skip(4).Where(card => card.GetCardType() == CardType.Spell))
                    if (!CardUpgradeHelper.TryApplyUpgradeToCard(card, null, upgrade, true, false, managers.GetCoreManagers(), out _))
                        throw new InvalidOperationException("Isolated spell upgrade was rejected by its native filter: " + upgradeId);
            }
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            foreach (CardState ephemeral in created.Skip(4).Where(card => card.HasTrait<CardTraitEphemeral>()))
                cards.MoveCardFromHandToTopOfDeck(ephemeral); // Native first-turn draw keeps requested plays out of deployment discard.
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            if (spellIds.FirstOrDefault() == "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a" &&
                spellIds.Contains("6a3633b6-4ecc-43b1-bb56-548f56cf1d60"))
                CardIds = CardIds.Concat(new[] { CardIds[4] }).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ISOLATED-SPELLS-PREPARED three native anchor controls, energy and unchanged originals " + string.Join(",", spellIds));
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
