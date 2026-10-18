using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalConsumeDamageScenario
    {
        internal const string Name = "original-consume-damage";
        internal const string SpellId = "fc119000-0000-4000-8000-000000000581";
        internal const string EnemyId = "fc119000-0000-4000-8000-000000000582";
        private static int step;
        private static bool actionMark;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Started { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static string? Error { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] EnemyUnitIds { get; private set; } = Array.Empty<int>();
        internal static object Evidence => new { OwnerId = RelicConsumeDamageModel.DataId,
            AssetKey = RelicConsumeDamageModel.AssetKey, SpellId, EnemyId, CardIds, EnemyUnitIds,
            InitialEnemyHealth = 31, Damage = 30, PositiveSpells = 4, ControlRoom = 1 };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == RelicConsumeDamageModel.DataId))
                throw new InvalidOperationException("Consume damage requires one pre-battle acquisition.");
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicConsumeDamageModel.DataId);
            CheckOriginal(relic);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData carrier = steward.GetSpawnCharacterData()!;
            Set(carrier, "health", 80); Set(carrier, "attackDamage", 30); Set(carrier, "size", 1); Set(steward, "cost", 0);
            save.GetDeckState().Clear();
            for (int i = 0; i < 3; i++)
                save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-CONSUME-DAMAGE-ACQUIRED unchanged Consume/front-Hero/30 damage owner; isolated three-Steward deck.");
        }

        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            if (Started || !Acquired || managers.GetSaveManager().PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Consume damage setup requires deployment.");
            Started = true;
            managers.GetSaveManager().StartCoroutine(Protect(Prepare(managers, log), log));
        }

        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            try
            {
                while (true)
                {
                    bool next;
                    try { next = native.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                    if (!next) break;
                    yield return native.Current;
                }
            }
            finally { (native as IDisposable)?.Dispose(); }
        }

        private static IEnumerator Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CombatManager combat = managers.GetCombatManager()!;
            CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicConsumeDamageModel.DataId));
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                    save, save.GetGeneratedBoss())).First(character => !character.IsMiniboss());
            CharacterData enemy = UnityEngine.Object.Instantiate(ordinary);
            enemy.name = "PojuConsumeDamageEnemy";
            Set(enemy, "id", EnemyId); Set(enemy, "health", 31); Set(enemy, "attackDamage", 0);
            Set(enemy, "triggers", new List<CharacterTriggerData>());
            Set(enemy, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(enemy, "unitAbility", null!);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(enemy);
            bool previewUpdates = combat.SuppressCombatPreviewUpdates;
            combat.SuppressCombatPreviewUpdates = true;
            try
            {
                for (int i = 0; i < 2; i++) yield return managers.GetHeroManager()!.SpawnHeroInRoom(enemy, 0, null!);
            }
            finally { combat.SuppressCombatPreviewUpdates = previewUpdates; }
            yield return combat.RunTriggerQueue();
            FullBattleTrace trace = FullBattleTrace.Active!;
            var enemies = new List<CharacterState>();
            managers.GetRoomManager()!.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
            if (enemies.Count != 2 || enemies.Any(unit => unit.GetHP() != 31))
                throw new InvalidOperationException("Consume damage did not create exactly two 31-HP enemy carriers.");
            EnemyUnitIds = enemies.Select(trace.UnitId).ToArray();

            CardData template = data.GetAllCardData().Where(card => card != null && card.GetCardType() == CardType.Spell &&
                !card.IsUnitAbility() && !card.IsRoomAbility()).OrderBy(card => card.GetID(), StringComparer.Ordinal).First();
            CardData spell = UnityEngine.Object.Instantiate(template);
            spell.name = "PojuConsumeDamageEmptySpell";
            Set(spell, "id", SpellId); Set(spell, "cardType", CardType.Spell); Set(spell, "cost", 0);
            Set(spell, "costType", CardData.CostType.Default); Set(spell, "targetless", true); Set(spell, "targetsRoom", true);
            Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
            Set(spell, "effects", new List<CardEffectData>()); Set(spell, "traits", new List<CardTraitData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>()); Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
            var ids = new List<int> { trace.CardId(cards.GetHand().First(card => card.GetCardType() == CardType.Monster)) };
            ids.Add(trace.CardId(cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false)));
            CardData consuming = UnityEngine.Object.Instantiate(spell);
            consuming.name = "PojuConsumeDamageConsumeSpell";
            Set(consuming, "id", "fc119000-0000-4000-8000-000000000583");
            CardTraitData exhaust = data.GetAllCardData().SelectMany(card => card.GetTraits())
                .First(trait => trait.GetTraitStateName() == "CardTraitExhaustState");
            Set(consuming, "traits", new List<CardTraitData> { exhaust });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(consuming);
            for (int i = 0; i < 6; i++)
                ids.Add(trace.CardId(cards.AddNewCard(consuming, CardPile.HandPile, false, false, animate: false)));
            CardIds = ids.ToArray(); step = 0; actionMark = false; Prepared = true;
            trace.InvalidateCardRules(); Set(combat, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CONSUME-DAMAGE-PREPARED two 31-HP Heroes, Monster/non-Consume controls, empty-room Consume, four positive Consume Spells and post-kill reset.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, PlayCardAction? fallback)
        {
            if (step < CardIds.Length)
            {
                int room = step == 2 ? 1 : 0;
                PlayCardAction? witness = BattleActionModel.EnumerateSupportedPlays(source)
                    .FirstOrDefault(play => play.CardInstanceId == CardIds[step] && play.RoomIndex == room);
                if (witness != null) { step++; actionMark = true; return witness; }
            }
            return fallback != null && CardIds.Contains(fallback.CardInstanceId)
                ? BattleActionModel.ChooseUnitAndJunkPlay(source, card => !CardIds.Contains(card.InstanceId)) : fallback;
        }

        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!actionMark || !CardIds.Contains(action.CardInstanceId)) return false;
            actionMark = false; return true;
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != RelicConsumeDamageModel.AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicConsumeDamageModel.Effect || effect.GetParamInt() != 30 ||
                effect.GetParamInt2() != 0 || effect.GetParamCardType() != CardType.Spell ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetTargetMode() != TargetMode.FrontInRoom ||
                effect.GetTraits().Count != 0 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original DamageOnConsume definition changed.");
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
