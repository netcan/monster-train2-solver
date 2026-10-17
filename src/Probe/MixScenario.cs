using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MixScenario
    {
        internal const string Name = "card-effect-mix";
        internal const string TinkermasterName = "original-tinkermaster-mix";
        internal const string ProgressName = "original-progress-pacemaker-mix";
        internal const string NefariousOverflowName = "original-nefarious-overflow";
        internal const string CopyFirstSpellName = "original-copy-first-spell-played";
        internal const string CopyFirstConsumeName = "original-copy-first-consume-played";
        internal const string CopyConsumedCardName = "original-copy-consumed-card";
        internal const string MonsterSpellTargetingName = "original-monster-spell-targeting";
        private const string TinkermasterId = "db2193eb-a476-4cc4-9a26-2d0e35f46b74";
        private const string ProgressId = "4446fd0c-f7c3-4a9d-96c3-bd12e4426a76";
        private const string NefariousOverflowId = "b0575799-1511-489d-a05d-b5b2e6bf84c2";
        private const string CopyFirstSpellId = "0f4a9e72-cd34-4667-95f6-6a0ca69832f8";
        private const string CopyFirstConsumeId = "b7207406-3c55-4771-b9c9-ed99dd9e1f9b";
        private const string CopyConsumedCardId = "51368ce4-b037-49ef-801c-545405b78800";
        private const string MonsterSpellTargetingId = "070c87f3-a48f-49d6-a288-04854ab3b0cf";
        private const string MonsterSpellWitnessId = "d9042b99-f328-4ae9-8995-1bb242a970c4";
        private const string MixologyId = "9582724d-2d5e-4349-a642-9720bbb833e9";
        private const string BlastInfusionId = "3cf25a9b-daa1-42cd-8174-eb0f59cd590f";
        private const string CopyWitnessSpellId = "03b20d75-b919-4f87-96e1-4d35cd556520";
        private const string CopyWitnessUpgradeId = "80a5edf4-6dc8-41a7-ab4e-9d0e914e0848";
        private static ManualLogSource? logger;
        private static int sourceId;
        private static bool played;
        private static int playedOpeningElixirId;
        private static bool copySourcePreludeMarked;
        private static bool copyControlPreludeMarked;
        private static int consumeCopyId;
        private static int drafts;
        private static int firstOpeningChoice;
        private static int secondOpeningChoice;
        private static bool explosionWitness;
        private static bool seededPoison;
        private static bool monsterSpellTargetingActionMarked;
        internal static bool Prepared { get; private set; }
        private static IReadOnlyList<ElixirMixSelection> pendingSelections = Array.Empty<ElixirMixSelection>();
        private static int pendingSelectionIndex;
        internal static bool Acquired { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is Name or TinkermasterName or ProgressName or NefariousOverflowName or CopyFirstSpellName or CopyFirstConsumeName or CopyConsumedCardName or MonsterSpellTargetingName;
        internal static bool TinkermasterMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == TinkermasterName;
        internal static bool ProgressMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == ProgressName;
        internal static bool NefariousOverflowMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == NefariousOverflowName;
        internal static bool CopyFirstSpellMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == CopyFirstSpellName;
        internal static bool CopyFirstConsumeMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == CopyFirstConsumeName;
        internal static bool CopyConsumedCardMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == CopyConsumedCardName;
        internal static bool MonsterSpellTargetingMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == MonsterSpellTargetingName;
        private static bool CopyConsumeMode => CopyFirstConsumeMode || CopyConsumedCardMode;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Original Mix relic must be acquired before battle setup.");
            string id = MonsterSpellTargetingMode ? MonsterSpellTargetingId : CopyConsumedCardMode ? CopyConsumedCardId : CopyFirstConsumeMode ? CopyFirstConsumeId : CopyFirstSpellMode ? CopyFirstSpellId : NefariousOverflowMode ? NefariousOverflowId : TinkermasterMode ? TinkermasterId : ProgressId;
            string key = MonsterSpellTargetingMode ? "SingleToTeamMonsterSpellTargeting" : CopyConsumedCardMode ? "CopyConsumedCard" : CopyFirstConsumeMode ? "CopyFirstConsumePlayed" : CopyFirstSpellMode ? "CopyFirstSpellPlayed" : NefariousOverflowMode ? "NefariousOverflow" : TinkermasterMode ? "TinkermasterTools" : "ProgressPacemaker";
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
            RelicEffectData effect = relic.GetEffects().Single();
            bool validNefariousOverflow = !NefariousOverflowMode ||
                effect.GetEffectClassName() == RelicUnstableRebalanceModel.Effect && effect.GetParamSourceTeam() == (Team.Type)255 &&
                effect.GetParamInt() == 0 && effect.GetParamInt2() == 1 && effect.GetParamMinInt() == 0 &&
                !effect.GetParamBool() && effect.GetParamStatusEffects().Single() is var unstable &&
                unstable.statusId == "unstable" && unstable.count == 1;
            bool validMixRelic = !(TinkermasterMode || ProgressMode) ||
                effect.GetEffectClassName() == "RelicEffectAdjustMixAmount" &&
                effect.GetParamInt() == (TinkermasterMode ? 1 : 0) &&
                effect.GetParamInt2() == (TinkermasterMode ? 0 : 1) &&
                effect.GetEffectConditions().Count == (TinkermasterMode ? 0 : 1);
            bool validCopyFirstSpell = !CopyFirstSpellMode || ValidCopyFirstSpell(effect);
            bool validCopyFirstConsume = !CopyFirstConsumeMode || ValidCopyFirstConsume(effect);
            bool validCopyConsumedCard = !CopyConsumedCardMode || ValidCopyConsumedCard(effect);
            bool validMonsterSpellTargeting = !MonsterSpellTargetingMode || ValidMonsterSpellTargeting(effect);
            if (relic.name != key || relic.DisallowInDeploymentPhase || !validNefariousOverflow || !validCopyFirstSpell || !validCopyFirstConsume || !validCopyConsumedCard ||
                !validMonsterSpellTargeting || !validMixRelic ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id))
                throw new InvalidOperationException("Original scenario relic definition or acquisition changed.");
            save.AddRelic(relic);
            Acquired = true;
            if (MonsterSpellTargetingMode)
                log.LogInfo("ORIGINAL-MONSTER-SPELL-TARGETING-ACQUIRED unchanged effect=" + effect.GetEffectClassName() +
                    " conditions=" + effect.GetEffectConditions().Count);
            else if (CopyFirstSpellMode)
                log.LogInfo("ORIGINAL-COPY-FIRST-SPELL-ACQUIRED unchanged count=" + effect.GetParamInt() +
                    " cardType=" + effect.GetParamCardType() + " upgrade=" + effect.GetParamCardUpgradeData()?.GetID() +
                    " conditions=" + effect.GetEffectConditions().Count);
            else if (CopyFirstConsumeMode)
                log.LogInfo("ORIGINAL-COPY-FIRST-CONSUME-ACQUIRED unchanged count=" + effect.GetParamInt() +
                    " cardType=" + effect.GetParamCardType() + " permanent=" + effect.GetParamBool() +
                    " requiredTraits=" + string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())) +
                    " conditions=" + effect.GetEffectConditions().Count);
            else if (CopyConsumedCardMode)
                log.LogInfo("ORIGINAL-COPY-CONSUMED-CARD-ACQUIRED unchanged count=" + effect.GetParamInt() +
                    " cardType=" + effect.GetParamCardType() + " permanent=" + effect.GetParamBool() +
                    " requiredTraits=" + string.Join(",", effect.GetTraits().Select(trait => trait.GetTraitStateName())) +
                    " conditions=" + effect.GetEffectConditions().Count);
            else if (NefariousOverflowMode)
                log.LogInfo("ORIGINAL-NEFARIOUS-OVERFLOW-ACQUIRED unchanged team=" + (int)effect.GetParamSourceTeam() +
                    " proc=" + effect.GetParamInt() + " explosionPerStack=" + effect.GetParamInt2() +
                    " growth=" + effect.GetParamMinInt() + " onlyOne=" + effect.GetParamBool());
            else
                log.LogInfo("ORIGINAL-MIX-RELIC-ACQUIRED unchanged " + key + " amount=" + effect.GetParamInt() +
                    " extraMixes=" + effect.GetParamInt2() + " followupAmount=" + effect.GetParamMinInt());
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if ((TinkermasterMode || ProgressMode || NefariousOverflowMode || CopyFirstSpellMode || CopyConsumeMode || MonsterSpellTargetingMode) && (!Acquired ||
                managers.GetSaveManager().GetCollectedRelics().Count(item => item.GetRelicDataID() ==
                    (MonsterSpellTargetingMode ? MonsterSpellTargetingId : CopyConsumedCardMode ? CopyConsumedCardId : CopyFirstConsumeMode ? CopyFirstConsumeId : CopyFirstSpellMode ? CopyFirstSpellId : NefariousOverflowMode ? NefariousOverflowId : TinkermasterMode ? TinkermasterId : ProgressId)) != 1))
                throw new InvalidOperationException("Original Mix relic is missing or duplicated at battle setup.");
            Prepared = false;
            logger = log;
            played = false;
            playedOpeningElixirId = 0;
            copySourcePreludeMarked = false;
            copyControlPreludeMarked = false;
            monsterSpellTargetingActionMarked = false;
            consumeCopyId = 0;
            seededPoison = false;
            drafts = 0;
            string opening = Environment.GetEnvironmentVariable("MT2_PROBE_MIX_CHOICES") ?? "00";
            if (opening.Length != 2 || opening.Any(choice => choice != '0' && choice != '1'))
                throw new InvalidOperationException("Mix opening choices must contain two binary indices.");
            firstOpeningChoice = opening[0] - '0';
            secondOpeningChoice = opening[1] - '0';
            explosionWitness = Environment.GetEnvironmentVariable("MT2_PROBE_MIX_UNSTABLE_EXPLOSION") == "1";
            if (explosionWitness && opening != "01")
                throw new InvalidOperationException("Unstable explosion fixture requires Mix choices 01.");
            pendingSelections = Array.Empty<ElixirMixSelection>();
            pendingSelectionIndex = 0;
            CardManager cards = managers.GetCardManager()!;
            AllGameData gameData = managers.GetSaveManager().GetAllGameData();
            CardData definition = MonsterSpellTargetingMode ? AddMonsterSpellWitness(gameData) : CopyFirstSpellMode || CopyConsumeMode ? AddCopyWitnessSpell(gameData, CopyConsumeMode) : gameData.FindCardData(MixologyId) ??
                throw new InvalidOperationException("Mix fixture requires original Mixology.");
            if ((!MonsterSpellTargetingMode && !CopyFirstSpellMode && !CopyConsumeMode && definition.GetEffects().Count(effect => effect.GetEffectStateName() == typeof(CardEffectMix).Name) != 2) ||
                !cards.GetBelowHandSize())
                throw new InvalidOperationException("Mix fixture requires its original effects and a free hand slot.");
            CardPile destination = MonsterSpellTargetingMode ? CardPile.DeckPileTop : CardPile.HandPile;
            CardState added = cards.AddNewCard(definition, destination, false, false, animate: false);
            if (added == null || !(MonsterSpellTargetingMode ? cards.GetDrawPile() : cards.GetHand()).Contains(added))
                throw new InvalidOperationException("Mix fixture could not place its scenario card in the requested pile.");
            sourceId = FullBattleTrace.Active!.CardId(added);
            Prepared = true;
            log.LogInfo(MonsterSpellTargetingMode
                ? "ORIGINAL-MONSTER-SPELL-TARGETING-PREPARED unchanged relic, direct 1-damage Monster-target witness card=" + sourceId
                : CopyFirstSpellMode
                ? "ORIGINAL-COPY-FIRST-SPELL-PREPARED unchanged relic, deterministic null-Spell witness card=" + sourceId
                : CopyFirstConsumeMode
                ? "ORIGINAL-COPY-FIRST-CONSUME-PREPARED unchanged relic, Consume Spell witness card=" + sourceId
                : CopyConsumedCardMode
                ? "ORIGINAL-COPY-CONSUMED-CARD-PREPARED unchanged relic, two-copy Consume Spell witness card=" + sourceId
                : NefariousOverflowMode
                ? "ORIGINAL-NEFARIOUS-OVERFLOW-PREPARED unchanged relic, original Mixology witness card=" + sourceId
                : "MIX-PREPARED original Mixology card=" + sourceId);
        }

        internal static bool IsScenarioPrelude(PlayCardAction action)
        {
            if (MonsterSpellTargetingMode && !monsterSpellTargetingActionMarked && action.CardInstanceId == sourceId)
            {
                monsterSpellTargetingActionMarked = true;
                return true;
            }
            if (CopyConsumeMode)
            {
                if (!copySourcePreludeMarked && action.CardInstanceId == sourceId)
                {
                    copySourcePreludeMarked = true;
                    return true;
                }
                if (!copyControlPreludeMarked && consumeCopyId != 0 && action.CardInstanceId == consumeCopyId)
                {
                    copyControlPreludeMarked = true;
                    return true;
                }
                return false;
            }
            if (CopyFirstSpellMode)
            {
                if (!copySourcePreludeMarked && action.CardInstanceId == sourceId)
                {
                    copySourcePreludeMarked = true;
                    return true;
                }
                if (!copyControlPreludeMarked && playedOpeningElixirId != 0 && action.CardInstanceId == playedOpeningElixirId)
                {
                    copyControlPreludeMarked = true;
                    return true;
                }
                return false;
            }
            return action.CardInstanceId == sourceId || action.CardInstanceId == playedOpeningElixirId;
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (MonsterSpellTargetingMode)
            {
                if (!played)
                {
                    IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
                    foreach (RoomCombatState room in state.Spawn.Train.Rooms)
                    {
                        int[] viable = room.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 &&
                            unit.Status("untouchable") == null).Select(unit => unit.Id).ToArray();
                        if (viable.Length < 2) continue;
                        PlayCardAction? witness = plays.FirstOrDefault(action => action.CardInstanceId == sourceId &&
                            action.RoomIndex == room.RoomIndex && viable.Contains(action.TargetUnitId));
                        if (witness != null)
                        {
                            played = true;
                            return witness;
                        }
                    }
                }
                PlayCardAction? normal = BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
                if (normal?.CardInstanceId != sourceId) return normal;
                return BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(action => action.CardInstanceId != sourceId &&
                    state.PlayRules?.Cards.FirstOrDefault(rule => rule.DataId == state.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId)?.Effect == "SpawnMonster");
            }
            if (CopyConsumeMode)
            {
                if (!played)
                {
                    CardToken? source = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == sourceId);
                    if (source != null)
                    {
                        played = true;
                        return new PlayCardAction(sourceId, 0);
                    }
                }
                if (consumeCopyId == 0)
                {
                    CombatContext context = state.Spawn.Train.Context!;
                    CardInstanceState? copy = context.CardInstances?.FirstOrDefault(card => card.DataId == CopyWitnessSpellId &&
                        card.InstanceId != sourceId && context.Cards.Hand.Any(item => item.InstanceId == card.InstanceId));
                    if (copy != null)
                    {
                        PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state)
                            .FirstOrDefault(item => item.CardInstanceId == copy.InstanceId);
                        if (action != null)
                        {
                            consumeCopyId = copy.InstanceId;
                            return action;
                        }
                    }
                }
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
            }
            if (CopyFirstSpellMode)
            {
                if (!played)
                {
                    CardToken? source = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == sourceId);
                    if (source != null)
                    {
                        played = true;
                        return new PlayCardAction(sourceId, 0);
                    }
                }
                if (playedOpeningElixirId == 0)
                {
                    CombatContext context = state.Spawn.Train.Context!;
                    CardInstanceState? copy = context.CardInstances?.FirstOrDefault(card => card.DataId == CopyWitnessSpellId &&
                        card.InstanceId != sourceId && card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == CopyWitnessUpgradeId) &&
                        context.Cards.Hand.Any(token => token.InstanceId == card.InstanceId));
                    if (copy != null)
                    {
                        PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state)
                            .FirstOrDefault(play => play.CardInstanceId == copy.InstanceId);
                        if (action != null)
                        {
                            playedOpeningElixirId = copy.InstanceId;
                            return action;
                        }
                    }
                }
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
            }
            if (!played)
            {
                CardToken? source = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == sourceId);
                if (source != null && state.Energy >= 2)
                {
                    played = true;
                    var selections = new List<ElixirMixSelection> {
                        new ElixirMixSelection(0, 0, firstOpeningChoice) };
                    if (ProgressMode) selections.Add(new ElixirMixSelection(0, 1, 0));
                    selections.Add(new ElixirMixSelection(1, 0, secondOpeningChoice));
                    return new PlayCardAction(sourceId, 0, mixSelections: selections);
                }
            }
            if (playedOpeningElixirId == 0 && firstOpeningChoice == 0 && secondOpeningChoice == 1)
            {
                CardToken? elixir = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == BlastInfusionId);
                if (elixir != null)
                {
                    var target = state.Spawn.Train.Rooms
                        .SelectMany(room => room.Units.Select(unit => (room.RoomIndex, unit)))
                        .FirstOrDefault(item => item.unit.Team == CombatTeam.Player && !item.unit.IsPyre && item.unit.Health > 5);
                    if (target.unit != null)
                    {
                        playedOpeningElixirId = elixir.InstanceId;
                        return new PlayCardAction(elixir.InstanceId, target.RoomIndex, targetUnitId: target.unit.Id);
                    }
                }
            }
            if (explosionWitness && !seededPoison && playedOpeningElixirId != 0 && state.Spawn.Turn == 1)
            {
                RoomCombatState room = state.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
                CombatUnit target = room.Units.Single(unit => unit.Team == CombatTeam.Player &&
                    unit.Status("unstable")?.Stacks == 5);
                if (!room.Units.Any(unit => unit.Team == CombatTeam.Enemy))
                    throw new InvalidOperationException("Unstable explosion fixture requires an enemy in the target room.");
                var nativeUnits = new List<CharacterState>();
                AllGameManagers.Instance!.GetRoomManager()!.GetRoom(0).AddCharactersToList(nativeUnits,
                    Team.Type.Heroes | Team.Type.Monsters);
                CharacterState nativeTarget = nativeUnits.Single(unit => FullBattleTrace.Active!.UnitId(unit) == target.Id);
                int prior = nativeTarget.GetStatusEffectStacks("poison");
                nativeTarget.AddStatusEffect("poison", 12, triggeredByCharacter: null,
                    allowModification: false, isFromHiddenTrigger: true);
                if (nativeTarget.GetStatusEffectStacks("poison") != prior + 12)
                    throw new InvalidOperationException("Original Frostbite seeding did not add twelve stacks.");
                seededPoison = true;
                logger!.LogInfo("MIX-UNSTABLE-EXPLOSION-SEEDED unit=" + target.Id +
                    " room=0 poison=" + prior + "->" + nativeTarget.GetStatusEffectStacks("poison") +
                    " enemies=" + room.Units.Count(unit => unit.Team == CombatTeam.Enemy));
            }
            if (playedOpeningElixirId != 0 && state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && unit.Status("unstable")?.Stacks > 0))
                return null;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        internal static void Expect(PlayCardAction action)
        {
            pendingSelections = action.MixSelections;
            pendingSelectionIndex = 0;
        }

        private static bool ValidCopyFirstSpell(RelicEffectData effect)
        {
            if (effect.GetEffectClassName() != RelicModel.CopyCardOnCardPlay || effect.GetParamInt() != 1 ||
                effect.GetParamCardType() != CardType.Spell || effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetTraits().Count != 0 || effect.GetParamCardUpgradeData()?.GetID() != "80a5edf4-6dc8-41a7-ab4e-9d0e914e0848")
                return false;
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData()!;
            if (!upgrade.HasTrait<CardTraitEphemeral>()) return false;
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            return (bool)Field("paramTrackTriggerCount") && Field("paramEntryDuration").ToString() == "ThisTurn" &&
                Convert.ToInt32(Field("paramComparator")) == 2 && (int)Field("paramInt") == 1 &&
                !(bool)Field("allowMultipleTriggersPerDuration");
        }

        private static bool ValidCopyFirstConsume(RelicEffectData effect)
        {
            if (effect.GetEffectClassName() != RelicModel.CopyCardOnCardPlay || effect.GetParamInt() != 1 ||
                effect.GetParamCardType() != CardType.Spell || !effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetParamCardUpgradeData() != null ||
                !effect.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" }))
                return false;
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            return (bool)Field("paramTrackTriggerCount") && Field("paramEntryDuration").ToString() == "ThisBattle" &&
                Convert.ToInt32(Field("paramComparator")) == 2 && (int)Field("paramInt") == 1 &&
                !(bool)Field("allowMultipleTriggersPerDuration");
        }

        private static bool ValidCopyConsumedCard(RelicEffectData effect)
        {
            if (effect.GetEffectClassName() != RelicModel.CopyCardOnCardPlay || effect.GetParamInt() != 2 ||
                effect.GetParamCardType() != CardType.Invalid || effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetParamCardUpgradeData() != null ||
                !effect.GetTraits().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitExhaustState" }))
                return false;
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            return (bool)Field("paramTrackTriggerCount") && Field("paramEntryDuration").ToString() == "ThisBattle" &&
                Field("paramTrackedValue").ToString() == "SubtypeInDeck" && Field("paramCardType").ToString() == "Any" &&
                Convert.ToInt32(Field("paramComparator")) == 6 && (int)Field("paramInt") == 1 &&
                !(bool)Field("allowMultipleTriggersPerDuration");
        }

        private static bool ValidMonsterSpellTargeting(RelicEffectData effect)
        {
            if (effect.GetEffectClassName() != RelicMonsterSpellTargetingModel.Effect ||
                effect.GetEffectConditions().Count != 1) return false;
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            return (bool)Field("paramTrackTriggerCount") && Field("paramEntryDuration").ToString() == "ThisTurn" &&
                Field("paramTrackedValue").ToString() == "SubtypeInDeck" &&
                Field("paramSubtype").ToString() == "SubtypesData_None" &&
                Convert.ToInt32(Field("paramComparator")) == 6 && (int)Field("paramInt") == 1 &&
                !(bool)Field("allowMultipleTriggersPerDuration");
        }

        private static CardData AddMonsterSpellWitness(AllGameData gameData)
        {
            if (gameData.FindCardData(MonsterSpellWitnessId) != null)
                throw new InvalidOperationException("Monster spell-targeting witness card id is already registered.");
            CardData template = gameData.GetAllCardData().FirstOrDefault(card => card.GetCardType() == CardType.Spell) ??
                throw new InvalidOperationException("Monster spell-targeting requires an original Spell template.");
            CardData witness = UnityEngine.Object.Instantiate(template);
            witness.name = "PojuMonsterSpellTargetingWitness";
            Set(witness, "id", MonsterSpellWitnessId);
            Set(witness, "cardType", CardType.Spell);
            Set(witness, "cost", 0);
            Set(witness, "costType", CardData.CostType.Default);
            Set(witness, "isUnitAbility", false);
            Set(witness, "isRoomAbility", false);
            Set(witness, "targetless", false);
            Set(witness, "targetsRoom", true);
            Set(witness, "traits", new List<CardTraitData>());
            Set(witness, "effectTriggers", new List<CharacterTriggerData>());
            Set(witness, "triggers", new List<CardTriggerEffectData>());
            Set(witness, "startingUpgrades", new List<CardUpgradeData>());
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(damage, "paramInt", 1);
            Set(witness, "effects", new List<CardEffectData> { damage });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gameData)!).Add(witness);
            return witness;
        }

        private static CardData AddCopyWitnessSpell(AllGameData gameData, bool withConsume)
        {
            if (gameData.FindCardData(CopyWitnessSpellId) != null)
                throw new InvalidOperationException("Copy-card-on-play witness card id is already registered.");
            CardData template = gameData.GetAllCardData().FirstOrDefault(card => card.GetCardType() == CardType.Spell) ??
                throw new InvalidOperationException("CopyFirstSpellPlayed requires an original Spell template.");
            CardData witness = UnityEngine.Object.Instantiate(template);
            witness.name = "PojuCopyFirstSpellWitness";
            Set(witness, "id", CopyWitnessSpellId);
            Set(witness, "cardType", CardType.Spell);
            Set(witness, "cost", 0);
            Set(witness, "costType", CardData.CostType.Default);
            Set(witness, "isUnitAbility", false);
            Set(witness, "isRoomAbility", false);
            Set(witness, "targetless", true);
            Set(witness, "targetsRoom", false);
            var traits = new List<CardTraitData>();
            if (withConsume)
            {
                var consume = new CardTraitData();
                Set(consume, "traitStateName", "CardTraitExhaustState");
                traits.Add(consume);
            }
            Set(witness, "traits", traits);
            Set(witness, "effects", new List<CardEffectData>());
            Set(witness, "effectTriggers", new List<CharacterTriggerData>());
            Set(witness, "triggers", new List<CardTriggerEffectData>());
            Set(witness, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gameData)!).Add(witness);
            return witness;
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        private static IEnumerator SelectDraft(ElixirDraftScreen screen, string kind)
        {
            yield return null;
            FieldInfo itemsField = AccessTools.Field(typeof(ElixirDraftScreen), "draftItems") ??
                throw new MissingFieldException(typeof(ElixirDraftScreen).FullName, "draftItems");
            IList items = (IList)itemsField.GetValue(screen);
            if (pendingSelectionIndex >= pendingSelections.Count)
                throw new InvalidOperationException("Native Mix opened a draft without a modeled choice.");
            int choiceIndex = pendingSelections[pendingSelectionIndex++].ChoiceIndex;
            if (choiceIndex < 0 || choiceIndex >= items.Count || items[choiceIndex] is not CardUI selected)
                throw new InvalidOperationException("Original Mix draft lacks its modeled card choice.");
            string choices = string.Join(",", items.Cast<CardUI>().Select(item =>
                item.GetCardState().GetCardDataID() + ":" + (item.GetCardUpgrade()?.GetID() ?? "none")));
            drafts++;
            logger!.LogInfo("MIX-DRAFT index=" + drafts + " kind=" + kind + " choice=" + choiceIndex + " choices=" + choices +
                " selected=" + selected.GetCardState().GetCardDataID() +
                " upgrade=" + (selected.GetCardUpgrade()?.GetID() ?? "none"));
            MethodInfo apply = AccessTools.Method(typeof(ElixirDraftScreen), "ApplyDraft") ??
                throw new MissingMethodException(typeof(ElixirDraftScreen).FullName, "ApplyDraft");
            CardManager cards = AllGameManagers.Instance!.GetCardManager()!;
            var before = new HashSet<CardState>(cards.GetHand());
            apply.Invoke(screen, new object[] { selected });
            CardState generated = cards.GetHand().Single(card => !before.Contains(card));
            logger.LogInfo("MIX-RESULT index=" + drafts + " card=" + FullBattleTrace.Active!.CardId(generated) +
                " data=" + generated.GetCardDataID() + " upgrades=" +
                string.Join(",", generated.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetAssetName())) +
                " hand=" + cards.GetHand().Count);
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForNewElixir))]
        private static class NewDraftPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ElixirDraftScreen __instance)
            {
                if (logger != null && sourceId > 0) __instance.StartCoroutine(SelectDraft(__instance, "new"));
            }
        }

        [HarmonyPatch(typeof(ElixirDraftScreen), nameof(ElixirDraftScreen.SetupForElixirUpgrade))]
        private static class UpgradeDraftPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ElixirDraftScreen __instance)
            {
                if (logger != null && sourceId > 0) __instance.StartCoroutine(SelectDraft(__instance, "upgrade"));
            }
        }
    }
}
