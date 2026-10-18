using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEatenDamageScenario
    {
        internal const string Name = "original-eaten-damage";
        internal const string SpellId = "fc119000-0000-4000-8000-000000000591";
        internal const string EnemyId = "fc119000-0000-4000-8000-000000000592";
        private static int step;
        private static bool actionMark;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Started { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CombatPrepared { get; private set; }
        internal static string? Error { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] EnemyUnitIds { get; private set; } = Array.Empty<int>();
        internal static object Evidence => new { OwnerId = RelicFriendlyTriggerDamageModel.DataId,
            AssetKey = RelicFriendlyTriggerDamageModel.AssetKey, SpellId, EnemyId, CardIds, EnemyUnitIds,
            InitialEnemyHealth = new[] { 12, 6 }, Damage = 5, PositiveSpells = 5, ControlRoom = 2 };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == RelicFriendlyTriggerDamageModel.DataId))
                throw new InvalidOperationException("Eaten damage requires one pre-battle acquisition.");
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicFriendlyTriggerDamageModel.DataId);
            CheckOriginal(relic);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData carrier = steward.GetSpawnCharacterData()!;
            Set(carrier, "health", 80); Set(carrier, "attackDamage", 30); Set(carrier, "size", 1); Set(steward, "cost", 0);
            save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++)
                save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-EATEN-DAMAGE-ACQUIRED unchanged Monsters OnEaten/weakest Hero/5 damage owner; isolated two-Steward deck.");
        }

        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            if (Started || !Acquired || managers.GetSaveManager().PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 1)
                throw new InvalidOperationException("Eaten damage setup requires the first non-deployment turn.");
            Started = true;
            managers.GetSaveManager().StartCoroutine(Protect(Prepare(managers, log), log));
        }

        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Eaten damage deployment must run once.");
            CardIds = managers.GetCardManager()!.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                .Take(2).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (CardIds.Length != 2) throw new InvalidOperationException("Missing two deployment carriers.");
            Prepared = true;
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
            CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicFriendlyTriggerDamageModel.DataId));
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                    save, save.GetGeneratedBoss())).First(character => !character.IsMiniboss());
            CharacterData enemy = UnityEngine.Object.Instantiate(ordinary);
            enemy.name = "PojuEatenDamageEnemy";
            Set(enemy, "id", EnemyId); Set(enemy, "health", 12); Set(enemy, "attackDamage", 0);
            Set(enemy, "triggers", new List<CharacterTriggerData>());
            Set(enemy, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(enemy, "unitAbility", null!);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(enemy);
            bool previewUpdates = combat.SuppressCombatPreviewUpdates;
            combat.SuppressCombatPreviewUpdates = true;
            try
            {
                yield return managers.GetHeroManager()!.SpawnHeroInRoom(enemy, 1, null!);
                CharacterData back = UnityEngine.Object.Instantiate(enemy);
                back.name = "PojuEatenDamageBackEnemy";
                Set(back, "id", "fc119000-0000-4000-8000-000000000593"); Set(back, "health", 6);
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(back);
                yield return managers.GetHeroManager()!.SpawnHeroInRoom(back, 1, null!);
            }
            finally { combat.SuppressCombatPreviewUpdates = previewUpdates; }
            yield return combat.RunTriggerQueue();
            FullBattleTrace trace = FullBattleTrace.Active!;
            var enemies = new List<CharacterState>();
            managers.GetRoomManager()!.GetRoom(1).AddCharactersToList(enemies, Team.Type.Heroes);
            if (enemies.Count != 2 || !enemies.Select(unit => unit.GetHP()).SequenceEqual(new[] { 12, 6 }))
                throw new InvalidOperationException("Eaten damage did not create the 12/6-HP enemy carriers.");
            EnemyUnitIds = enemies.Select(trace.UnitId).ToArray();

            CardData template = data.GetAllCardData().Where(card => card != null && card.GetCardType() == CardType.Spell &&
                !card.IsUnitAbility() && !card.IsRoomAbility()).OrderBy(card => card.GetID(), StringComparer.Ordinal).First();
            CardData spell = UnityEngine.Object.Instantiate(template);
            spell.name = "PojuEatenDamageTriggerSpell";
            Set(spell, "id", SpellId); Set(spell, "cardType", CardType.Spell); Set(spell, "cost", 0);
            Set(spell, "costType", CardData.CostType.Default); Set(spell, "targetless", true); Set(spell, "targetsRoom", true);
            Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
            var trigger = new CardEffectData("CardEffectPlayUnitTrigger", null!, Team.Type.Monsters);
            Set(trigger, "targetMode", TargetMode.FrontInRoom); Set(trigger, "paramTrigger", CharacterTriggerData.Trigger.OnEaten);
            Set(spell, "effects", new List<CardEffectData> { trigger }); Set(spell, "traits", new List<CardTraitData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>()); Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
            var ids = CardIds.ToList();
            ids.Add(trace.CardId(cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false)));
            CardData enemyTrigger = UnityEngine.Object.Instantiate(spell);
            enemyTrigger.name = "PojuEatenDamageEnemyTrigger";
            Set(enemyTrigger, "id", "fc119000-0000-4000-8000-000000000594");
            var wrongTeam = new CardEffectData("CardEffectPlayUnitTrigger", null!, Team.Type.Heroes);
            Set(wrongTeam, "targetMode", TargetMode.FrontInRoom); Set(wrongTeam, "paramTrigger", CharacterTriggerData.Trigger.OnEaten);
            Set(enemyTrigger, "effects", new List<CardEffectData> { wrongTeam });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(enemyTrigger);
            ids.Add(trace.CardId(cards.AddNewCard(enemyTrigger, CardPile.HandPile, false, false, animate: false)));
            for (int i = 0; i < 6; i++)
                ids.Add(trace.CardId(cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false)));
            CardIds = ids.ToArray(); actionMark = false; CombatPrepared = true;
            trace.InvalidateCardRules(); Set(combat, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-EATEN-DAMAGE-PREPARED 12/6-HP Heroes, two friendly carriers, empty-room/enemy-actor controls, five OnEaten positives and post-kill reset.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, PlayCardAction? fallback)
        {
            if (step < CardIds.Length)
            {
                int room = step == 1 || step == 2 ? 2 : 1;
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
            if (relic.name != RelicFriendlyTriggerDamageModel.AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicFriendlyTriggerDamageModel.Effect || effect.GetParamInt() != 5 ||
                effect.GetParamInt2() != 0 || effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnEaten ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetTargetMode() != TargetMode.Weakest ||
                effect.GetTraits().Count != 0 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original DamageOnEaten definition changed.");
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
