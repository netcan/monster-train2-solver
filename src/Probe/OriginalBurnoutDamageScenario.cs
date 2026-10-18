using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBurnoutDamageScenario
    {
        internal const string Name = "original-burnout-death-damage";
        private const string CarrierId = "fc119000-0000-4000-8000-000000000601";
        private const string ControlId = "fc119000-0000-4000-8000-000000000602";
        private const string KillId = "fc119000-0000-4000-8000-000000000603";
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
        internal static object Evidence => new { OwnerId = RelicFriendlyTriggerDamageModel.BurnoutDataId,
            AssetKey = RelicFriendlyTriggerDamageModel.BurnoutAssetKey, CarrierId, ControlId, KillId, CardIds,
            EnemyUnitIds, InitialEnemyHealth = new[] { 12, 6, 4 }, Damage = 5 };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == RelicFriendlyTriggerDamageModel.BurnoutDataId))
                throw new InvalidOperationException("Burnout damage requires one isolated pre-battle acquisition.");
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicFriendlyTriggerDamageModel.BurnoutDataId);
            CheckOriginal(relic);
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)!;
            Set(steward.GetSpawnCharacterData()!, "health", 80); Set(steward.GetSpawnCharacterData()!, "attackDamage", 30);
            Set(steward.GetSpawnCharacterData()!, "size", 1); Set(steward, "cost", 0);
            save.GetDeckState().Clear();
            for (int i = 0; i < 2; i++) save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-BURNOUT-DAMAGE-ACQUIRED unchanged ordered OnDeath/Ephemeral and OnBurnout room-damage effects.");
        }

        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (Prepared || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Expected one deployment setup.");
            CardIds = managers.GetCardManager()!.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                .Take(2).Select(FullBattleTrace.Active!.CardId).ToArray();
            if (CardIds.Length != 2) throw new InvalidOperationException("Missing deployment Stewards.");
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
            CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicFriendlyTriggerDamageModel.BurnoutDataId));
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save,
                    save.GetGeneratedBoss())).First(character => !character.IsMiniboss());
            bool suppressed = combat.SuppressCombatPreviewUpdates; combat.SuppressCombatPreviewUpdates = true;
            try
            {
                int index = 0;
                foreach (int health in new[] { 12, 6, 4 })
                {
                    CharacterData enemy = UnityEngine.Object.Instantiate(ordinary); enemy.name = "PojuBurnoutDamageEnemy" + index;
                    Set(enemy, "id", "fc119000-0000-4000-8000-00000000060" + (4 + index++));
                    Set(enemy, "health", health); Set(enemy, "attackDamage", 0); Set(enemy, "triggers", new List<CharacterTriggerData>());
                    Set(enemy, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(enemy, "unitAbility", null!);
                    ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(enemy);
                    yield return managers.GetHeroManager()!.SpawnHeroInRoom(enemy, 1, null!);
                }
            }
            finally { combat.SuppressCombatPreviewUpdates = suppressed; }
            yield return combat.RunTriggerQueue();
            var enemies = new List<CharacterState>(); managers.GetRoomManager()!.GetRoom(1).AddCharactersToList(enemies, Team.Type.Heroes);
            if (!enemies.Select(unit => unit.GetHP()).SequenceEqual(new[] { 12, 6, 4 })) throw new InvalidOperationException("Missing three enemy carriers.");
            FullBattleTrace trace = FullBattleTrace.Active!; EnemyUnitIds = enemies.Select(trace.UnitId).ToArray();
            CardData template = data.GetAllCardData().First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CardData carrier = UnityEngine.Object.Instantiate(template);
            CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData());
            unit.name = "PojuBurnoutCarrier"; Set(unit, "id", CarrierId); Set(unit, "health", 80); Set(unit, "attackDamage", 0);
            Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null!);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "ephemeral", count = 1 } });
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
            carrier.name = "PojuBurnoutCarrierCard"; Set(carrier, "id", CarrierId);
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            Set(spawn, "targetMode", TargetMode.Room); Set(spawn, "paramCharacterData", unit);
            Set(carrier, "effects", new List<CardEffectData> { spawn });
            Set(carrier, "cost", 0); Set(carrier, "traits", new List<CardTraitData>()); Set(carrier, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            CardData control = UnityEngine.Object.Instantiate(carrier); control.name = "PojuNonBurnoutControl"; Set(control, "id", ControlId);
            CharacterData plain = UnityEngine.Object.Instantiate(unit); plain.name = "PojuNonBurnoutCarrier"; Set(plain, "id", ControlId);
            Set(plain, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            var controlSpawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            Set(controlSpawn, "targetMode", TargetMode.Room); Set(controlSpawn, "paramCharacterData", plain);
            Set(control, "effects", new List<CardEffectData> { controlSpawn });
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(plain);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(control);
            CardData spell = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility()));
            spell.name = "PojuBurnoutKillSpell"; Set(spell, "id", KillId); Set(spell, "cost", 0); Set(spell, "costType", CardData.CostType.Default);
            Set(spell, "targetless", false); Set(spell, "targetsRoom", true); Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
            var kill = new CardEffectData("CardEffectKill", null!, Team.Type.Monsters); Set(kill, "targetMode", TargetMode.DropTargetCharacter);
            Set(spell, "effects", new List<CardEffectData> { kill }); Set(spell, "traits", new List<CardTraitData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>()); Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(spell);
            CardIds = CardIds.Concat(new[] { carrier, carrier, control, spell, spell }
                .Select(card => trace.CardId(cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false)))).ToArray();
            trace.InvalidateCardRules(); Set(combat, "combatStateChanged", true); CombatPrepared = true;
            log.LogInfo("ORIGINAL-BURNOUT-DAMAGE-PREPARED three 12/6/4-HP Heroes, two Ephemeral carriers, non-Ephemeral sacrifice control and natural burnout.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, PlayCardAction? fallback)
        {
            if (step < CardIds.Length)
            {
                int room = step == 0 ? 0 : step == 1 ? 2 : 1;
                PlayCardAction? witness = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == CardIds[step] && play.RoomIndex == room && (step < 5 ||
                        source.Spawn.Train.Rooms.Single(floor => floor.RoomIndex == room).Units.Any(unit => unit.Id == play.TargetUnitId &&
                            (step == 5 ? unit.AssetKey == "PojuNonBurnoutCarrier" : unit.AssetKey == "PojuBurnoutCarrier"))));
                if (witness != null) { step++; actionMark = true; return witness; }
            }
            return fallback != null && CardIds.Contains(fallback.CardInstanceId)
                ? BattleActionModel.ChooseUnitAndJunkPlay(source, card => !CardIds.Contains(card.InstanceId)) : fallback;
        }

        internal static bool IsScenarioAction(PlayCardAction action)
        { if (!actionMark || !CardIds.Contains(action.CardInstanceId)) return false; actionMark = false; return true; }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            if (relic.name != RelicFriendlyTriggerDamageModel.BurnoutAssetKey || relic.DisallowInDeploymentPhase || relic.GetEffects().Count != 2)
                throw new InvalidOperationException("Original burnout damage owner changed.");
            for (int index = 0; index < 2; index++)
            {
                RelicEffectData effect = relic.GetEffects()[index];
                if (effect.GetEffectClassName() != RelicFriendlyTriggerDamageModel.Effect || effect.GetParamInt() != 5 || effect.GetParamInt2() != 0 ||
                    effect.GetParamTrigger() != (index == 0 ? CharacterTriggerData.Trigger.OnDeath : CharacterTriggerData.Trigger.OnBurnout) ||
                    effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetTargetMode() != TargetMode.Room ||
                    !effect.GetParamStatusEffects().Select(status => status.statusId).SequenceEqual(index == 0 ? new[] { "ephemeral" } : Array.Empty<string>()) ||
                    effect.GetTraits().Count != 0 || effect.GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original burnout damage effect changed.");
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
