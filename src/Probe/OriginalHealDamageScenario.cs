using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHealDamageScenario
    {
        internal const string Name = "original-heal-to-damage";
        private const string IdPrefix = "fc119000-0000-4000-8000-000000000";
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
        internal static object Evidence => new { OwnerId = RelicHealDamageModel.DataId, AssetKey = RelicHealDamageModel.AssetKey,
            CardIds, EnemyUnitIds, InitialEnemyHealth = new[] { 12, 6 }, ActualHeals = new[] { 0, 4, 8, 6 }, Multiplier = 1 };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == RelicHealDamageModel.DataId))
                throw new InvalidOperationException("Heal damage requires one isolated pre-battle acquisition.");
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicHealDamageModel.DataId);
            CheckOriginal(relic);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "health", 80); Set(unit, "attackDamage", 30); Set(unit, "size", 1); Set(steward, "cost", 0);
            save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-HEAL-DAMAGE-ACQUIRED unchanged Monsters/OnHeal/front-Hero actual-heal multiplier one.");
        }

        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected one deployment setup.");
            CardIds = managers.GetCardManager()!.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                .Take(2).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (CardIds.Length != 2) throw new InvalidOperationException("Missing deployment carriers.");
            Prepared = true;
        }

        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            if (Started || !Prepared || managers.GetCombatManager()!.GetTurnCount() != 1) throw new InvalidOperationException("Expected formal turn one.");
            Started = true; managers.GetSaveManager().StartCoroutine(Protect(Prepare(managers, log), log));
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
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!; CombatManager combat = managers.GetCombatManager()!;
            CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicHealDamageModel.DataId));
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save,
                    save.GetGeneratedBoss())).First(character => !character.IsMiniboss());
            bool suppressed = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                int index = 0;
                foreach (int health in new[] { 12, 6 })
                {
                    CharacterData enemy = UnityEngine.Object.Instantiate(ordinary); enemy.name = "PojuHealDamageEnemy" + index;
                    Set(enemy, "id", IdPrefix + (621 + index++)); Set(enemy, "health", health); Set(enemy, "attackDamage", 0);
                    Set(enemy, "triggers", new List<CharacterTriggerData>()); Set(enemy, "unitAbility", null!);
                    Set(enemy, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                    ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(enemy);
                    yield return managers.GetHeroManager()!.SpawnHeroInRoom(enemy, 1, null!);
                }
            }
            finally { combat.SuppressCombatPreviewUpdates = suppressed; }
            yield return combat.RunTriggerQueue();
            var enemies = new List<CharacterState>(); managers.GetRoomManager()!.GetRoom(1).AddCharactersToList(enemies, Team.Type.Heroes);
            if (!enemies.Select(unit => unit.GetHP()).SequenceEqual(new[] { 12, 6 })) throw new InvalidOperationException("Missing 12/6-HP enemy carriers.");
            FullBattleTrace trace = FullBattleTrace.Active!; EnemyUnitIds = enemies.Select(trace.UnitId).ToArray();
            CardData template = data.GetAllCardData().Where(card => card.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility())
                .OrderBy(card => card.GetID(), StringComparer.Ordinal).First();
            var definitions = new List<CardData>();
            foreach (var spec in new (int Wound, int Heal, bool Enemy)[] { (0, 6, false), (4, 9, false), (8, 8, false),
                (3, 9, true), (6, 9, false), (2, 9, false), (3, 9, false) })
            {
                CardData spell = UnityEngine.Object.Instantiate(template); spell.name = "PojuHealDamageSpell" + definitions.Count;
                Set(spell, "id", IdPrefix + (623 + definitions.Count)); Set(spell, "cardType", CardType.Spell);
                Set(spell, "cost", 0); Set(spell, "costType", CardData.CostType.Default); Set(spell, "targetless", true); Set(spell, "targetsRoom", true);
                Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
                var effects = new List<CardEffectData>();
                Team.Type team = spec.Enemy ? Team.Type.Heroes : Team.Type.Monsters;
                if (spec.Wound > 0) effects.Add(Effect("CardEffectDamage", team, spec.Wound));
                effects.Add(Effect("CardEffectHeal", team, spec.Heal));
                Set(spell, "effects", effects); Set(spell, "traits", new List<CardTraitData>());
                Set(spell, "triggers", new List<CardTriggerEffectData>()); Set(spell, "effectTriggers", new List<CharacterTriggerData>());
                Set(spell, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
                definitions.Add(spell);
            }
            CardIds = CardIds.Concat(definitions.Select(card => trace.CardId(cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false)))).ToArray();
            trace.InvalidateCardRules(); Set(combat, "combatStateChanged", true); CombatPrepared = true;
            log.LogInfo("ORIGINAL-HEAL-DAMAGE-PREPARED actual heals 0/4/8/6, front/back kills, enemy-heal control and two empty-room/reset callbacks.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, PlayCardAction? fallback)
        {
            if (step < CardIds.Length)
            {
                int room = step == 1 || step == 8 ? 2 : 1;
                PlayCardAction? witness = BattleActionModel.EnumerateSupportedPlays(source)
                    .FirstOrDefault(play => play.CardInstanceId == CardIds[step] && play.RoomIndex == room);
                if (witness != null) { step++; actionMark = true; return witness; }
            }
            return fallback != null && CardIds.Contains(fallback.CardInstanceId)
                ? BattleActionModel.ChooseUnitAndJunkPlay(source, card => !CardIds.Contains(card.InstanceId)) : fallback;
        }
        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark || !CardIds.Contains(action.CardInstanceId)) return false; actionMark = false; return true; }
        private static CardEffectData Effect(string type, Team.Type team, int amount)
        {
            var effect = new CardEffectData(type, null!, team); Set(effect, "targetMode", TargetMode.FrontInRoom); Set(effect, "paramInt", amount); return effect;
        }
        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != RelicHealDamageModel.AssetKey || relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != RelicHealDamageModel.Effect ||
                effect.GetParamInt() != 1 || effect.GetParamInt2() != 0 || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnHeal || effect.GetTargetMode() != TargetMode.FrontInRoom ||
                effect.GetEffectConditions().Count != 0) throw new InvalidOperationException("Original HealToDamage definition changed.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
