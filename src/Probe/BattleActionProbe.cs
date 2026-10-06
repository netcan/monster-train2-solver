using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class BattleActionProbe
    {
        private readonly FullBattleTrace trace;
        private readonly UnitPlayModelProbe projection;
        private readonly ManualLogSource log;
        private readonly List<Record> records = new List<Record>();
        private BattlePlayRules? rules;
        internal IReadOnlyList<Record> Records => records;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);
        internal BattleActionProbe(FullBattleTrace trace, UnitPlayModelProbe projection, ManualLogSource log)
        { this.trace = trace; this.projection = projection; this.log = log; }

        internal BattlePlayRules CaptureRules(EnemySpawnState spawn)
        {
            if (rules != null) return rules;
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            var nativeCards = cards.GetAllCards(new List<CardState>()).ToArray();
            var definitions = nativeCards.Select(card => card.GetCardDataID()).Concat(spawn.Waves.SelectMany(wave => wave.Candidates)
                .SelectMany(group => group.Units).Concat(spawn.Treasures).SelectMany(unit => unit.Unit.Triggers)
                .SelectMany(trigger => trigger.Effects).SelectMany(effect => effect.CardPool)).Distinct().ToArray();
            RoomManager rooms = managers.GetRoomManager()!;
            var roomRules = new List<RoomPlayRule>();
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                RoomState room = rooms.GetRoom(index);
                CapacityInfo capacity = room.GetCapacityInfo(Team.Type.Monsters);
                roomRules.Add(new RoomPlayRule(index, capacity.max, capacity.numSpawnPoints,
                    room.IsRoomEnabled(), room.IsRoomSummonBlocked(managers.GetRelicManager()), room.GetIsPyreRoom(),
                    room.GetCapacityInfo(Team.Type.Heroes).max));
            }
            var reachable = new Dictionary<string, CardPlayRule>(StringComparer.Ordinal);
            var pending = new Queue<string>(definitions);
            while (pending.Count > 0)
            {
                string id = pending.Dequeue(); if (reachable.ContainsKey(id)) continue;
                CardPlayRule rule = Definition(save.GetAllGameData().FindCardData(id)!); reachable.Add(id, rule);
                foreach (string child in rule.Effects.Where(effect => effect.Generation != null).SelectMany(effect => effect.Generation!.Pool).Select(card => card.DataId)
                    .Concat(rule.SpawnUnit?.Triggers.SelectMany(trigger => trigger.Effects).SelectMany(effect => effect.CardPool) ?? Array.Empty<string>()))
                    pending.Enqueue(child);
            }
            rules = new BattlePlayRules(roomRules, reachable.Values.OrderBy(rule => rule.DataId, StringComparer.Ordinal).ToArray(),
                new[] { "armor", "valor", "pyregel" }.Select(id => Status(id, 1)).ToArray());
            return rules;
        }

        internal static CardPlayRule Definition(CardData data)
        {
            var interactions = new List<string>();
            var upgradeInteractions = data.GetTraits().Where(trait => !DamageScalingProbe.Known(trait.GetTraitStateName()))
                .Select(trait => "Upgrade trait " + trait.GetTraitStateName()).ToArray();
            if (data.GetCardTriggers().Count > 0) interactions.Add("Card triggers");
            if (data.GetTraits().Any(trait => !DamageScalingProbe.Known(trait.GetTraitStateName()))) interactions.Add("Card traits");
            bool selfPurge = data.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitSelfPurge");
            CardEffectData[] effects = data.GetEffects().ToArray();
            string kind = effects.Length == 1 ? effects[0].GetEffectStateName() : "MultipleEffects";
            CombatUnit? template = null;
            var spellEffects = new List<CardActionEffect>();
            string destination = selfPurge ? "Purged" : "Discard";
            if (kind == "CardEffectSpawnMonster")
            {
                CardEffectData effect = effects[0];
                CharacterData? unit = effect.GetParamCharacterData();
                if (unit == null || effect.GetParamAdditionalCharacterData() != null ||
                    effect.GetParamCharacterDataPool().Count > 0 || effect.GetParamInt() > 1 ||
                    effect.GetParamBool() || effect.GetParamCardUpgradeData() != null ||
                    effect.GetTargetMode() != TargetMode.Room || selfPurge)
                    interactions.Add("Additional/modified unit spawn");
                if (unit != null)
                {
                    EnemyDefinition definition = EnemySpawningProbe.Definition(unit);
                    interactions.AddRange(definition.ExternalInteractions);
                    if (unit.GetGraftedEquipment() != null) interactions.Add("Grafted equipment");
                    if (unit.GetUnitAbilityCardData() != null) interactions.Add("Spawned unit ability");
                    CombatUnit source = definition.Unit;
                    template = new CombatUnit(0, source.AssetKey, CombatTeam.Player, source.BaseAttack, source.Health,
                        source.MaxHealth, source.CanAttack, false, false, source.Statuses, source.Triggers, size: source.Size,
                        statusImmunities: source.StatusImmunities, subtypes: source.Subtypes, modifiers: source.Modifiers, isBoss: source.IsBoss);
                }
                kind = "SpawnMonster"; destination = "Standby";
            }
            else if (kind == "CardEffectNULL") kind = "Null";
            else if (data.GetCardType() == CardType.Spell && effects.Length > 0 && effects.All(effect =>
                new[] { "CardEffectDamage", "CardEffectDraw", "CardEffectDiscardHand", "CardEffectAddBattleCard", "CardEffectHeal", "CardEffectBuffDamage", "CardEffectDebuffDamage", "CardEffectBuffMaxHealth", "CardEffectDebuffMaxHealth", "CardEffectAddStatusEffect", "CardEffectFloorRearrange", "CardEffectAddCardUpgradeToUnits",
                    "CardEffectAddTempCardUpgradeToUnits", "CardEffectRemoveTempUpgradeFromUnit",
                    "CardEffectAddTempCardUpgradeToCardsInHand", "CardEffectAddPermanentCardUpgradeToCardsInHand" }.Contains(effect.GetEffectStateName())))
            {
                kind = "Spell";
                for (int index = 0; index < effects.Length; index++)
                {
                    CardEffectData effect = effects[index];
                    bool handUpgrade = effect.GetEffectStateName() == "CardEffectAddTempCardUpgradeToCardsInHand" ||
                        effect.GetEffectStateName() == "CardEffectAddPermanentCardUpgradeToCardsInHand";
                    var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
                    if (effect.GetUseStatusEffectStackMultiplier() || effect.GetUseHealthMissingStackMultiplier() ||
                        effect.GetUseMagicPowerMultiplier() || !effect.GetParamSubtype().IsNone ||
                        (handUpgrade ? effect.GetTargetMode() != TargetMode.Hand && effect.GetTargetMode() != TargetMode.Room :
                            !CardTargetModel.Supports(effect.GetTargetMode().ToString())))
                        interactions.Add("Spell scaling or target filters");
                    string type = effect.GetEffectStateName() == "CardEffectDamage" ? "Damage" :
                        effect.GetEffectStateName() == "CardEffectDraw" ? "Draw" :
                        effect.GetEffectStateName() == "CardEffectDiscardHand" ? "DiscardHand" :
                        effect.GetEffectStateName() == "CardEffectAddBattleCard" ? "Generate" :
                        effect.GetEffectStateName() == "CardEffectBuffDamage" ? "BuffAttack" :
                        effect.GetEffectStateName() == "CardEffectDebuffDamage" ? "DebuffAttack" :
                        effect.GetEffectStateName() == "CardEffectBuffMaxHealth" ? "BuffHealth" :
                        effect.GetEffectStateName() == "CardEffectDebuffMaxHealth" ? "DebuffHealth" :
                        effect.GetEffectStateName() == "CardEffectHeal" ? "Heal" :
                        effect.GetEffectStateName() == "CardEffectFloorRearrange" ? "FloorRearrange" :
                        effect.GetEffectStateName() == "CardEffectAddStatusEffect" ? "AddStatus" :
                        handUpgrade ? "HandUpgrade" : effect.GetEffectStateName() == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" : "UnitUpgrade";
                    CardUpgradeModifier? upgrade = null;
                    if (effect.GetUseIntRange() && !new[] { "Damage", "Heal", "AddStatus", "BuffAttack", "DebuffAttack", "BuffHealth", "DebuffHealth", "Draw", "DiscardHand", "Generate" }.Contains(type))
                        interactions.Add("Unimplemented integer range consumer " + type);
                    string lifetime = "";
                    if (type == "BuffHealth") lifetime = ((UnitUpgradeLifetimeTempOnly)effect.GetAdditionalParamInt1()).ToString();
                    if (type == "UnitUpgrade" || type == "RemoveUnitUpgrade" || handUpgrade)
                    {
                        if (effect.GetParamCardUpgradeData() == null) interactions.Add("Missing unit upgrade data");
                        else
                        {
                            var upgradeState = new CardUpgradeState(); upgradeState.Setup(effect.GetParamCardUpgradeData());
                            upgrade = CardModifierProbe.Upgrade(upgradeState);
                            interactions.AddRange(upgrade.ExternalInteractions);
                        }
                        lifetime = handUpgrade ? effect.GetEffectStateName() == "CardEffectAddPermanentCardUpgradeToCardsInHand" ?
                            "Permanent" : "TemporaryUntilEndOfBattle" : ((UnitUpgradeLifetime)effect.GetAdditionalParamInt1()).ToString();
                        if (effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnits" && effect.GetParamBool())
                            interactions.Add("Scaling unit upgrade instances");
                    }
                    var statuses = new List<CombatStatus>();
                    if (type == "AddStatus")
                    {
                        if (effect.GetParamStatusEffects().Length == 0) interactions.Add("Empty status effect pool");
                        foreach (StatusEffectStackData status in effect.GetParamStatusEffects())
                        {
                            if (!StatusEffectManager.Instance.GetStatusEffectDataById(status.statusId)!.IsStackable()) interactions.Add("Nonstackable status legality");
                            statuses.Add(Status(status.statusId, status.count));
                        }
                    }
                    spellEffects.Add(new CardActionEffect(type, effect.GetTargetMode().ToString(), effect.GetParamInt(),
                        effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters), statuses,
                        upgrade, lifetime, new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                            effect.GetShouldCancelSubsequentEffectsIfTestFails(), type == "AddStatus" && effect.GetParamBool(),
                            ((ICardEffect)Activator.CreateInstance(typeof(CardEffectBase).Assembly.GetType(effect.GetEffectStateName())!)!).CanPlayAfterBossDead),
                        effect.GetUseIntRange() ? new CardEffectRange(effect.GetParamMinInt(), effect.GetParamMaxInt(), effect.GetParamMultiplier()) : null,
                        new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                            effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                            effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                            excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()),
                        type == "Generate" ? CardGenerationProbe.Definition(effect) : null));
                }
            }
            else interactions.Add("Unimplemented play effect " + kind);
            return new CardPlayRule(data.GetID(), data.name, data.GetCost(), kind, destination, template,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), spellEffects, upgradeInteractions,
                HandInteractions(data, false), HandInteractions(data, true));
        }

        private static string[] HandInteractions(CardData data, bool consume)
        {
            var interactions = new List<string>();
            if (data.GetCardTriggers().Count > 0) interactions.Add("Hand removal card triggers");
            foreach (var trait in data.GetTraits())
            {
                Type? type = typeof(CardTraitState).Assembly.GetType(trait.GetTraitStateName());
                if (type == null) { interactions.Add("Unknown hand removal trait"); continue; }
                if (consume)
                {
                    if (typeof(CardTraitSalvage).IsAssignableFrom(type)) interactions.Add("Salvage consumption callback");
                    if (type == typeof(CardTraitGraftedEquipment)) interactions.Add("Grafted equipment standby trait state");
                }
                else if (type == typeof(CardTraitEphemeral) || type == typeof(CardTraitInfusion) || type == typeof(CardTraitPersistent) || type == typeof(CardTraitTreasure) ||
                    type != typeof(CardTraitSelfPurge) && type.GetMethod("OnCardDiscarded")?.DeclaringType != typeof(CardTraitState))
                    interactions.Add(type.Name + " hand discard callback/routing");
            }
            return interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        internal static CombatStatus Status(string id, int count)
        {
            StatusEffectData rule = StatusEffectManager.Instance.GetStatusEffectDataById(id)!;
            return new CombatStatus(id, count, rule.GetParamInt(), rule.GetRemoveWhenTriggered(), rule.GetRemoveStackAtEndOfTurn(),
                rule.GetRemoveAtEndOfTurn(), rule.GetRemoveAtEndOfTurnAfterPostCombat(), false, rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                TriggeredVfx(rule, -1f), TriggeredVfx(rule, 1f), rule.IsStackable());
        }

        internal static bool TriggeredVfx(StatusEffectData rule, float facing) => rule.GetOnTriggeredVFX()?.GetVfxPrefab(facing) != null;

        internal void Begin(PlayCardAction action)
        {
            try
            {
                if (records.Any(record => record.Actual == null)) throw new InvalidOperationException("Previous card action is pending.");
                BattleTurnState before = trace.CaptureDecision();
                records.Add(new Record { Index = records.Count, Before = before, Action = action,
                    Predicted = BattleActionModel.PlayCard(before, action) });
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal void Complete()
        {
            Record? record = records.LastOrDefault(item => item.Actual == null);
            if (record == null) return;
            try
            {
                record.Actual = trace.CaptureDecision();
                record.ActualOutcome = trace.NativeWon == null ? RoomOutcome.Exchanged :
                    trace.NativeWon.Value ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
                if (record.Predicted.Supported)
                {
                    record.Difference = record.Predicted.Outcome != record.ActualOutcome ? "Card play terminal outcome differs" :
                        JToken.DeepEquals(BattleTurnProbe.Comparable(record.Predicted.State!),
                        BattleTurnProbe.Comparable(record.Actual)) ? null : "Card play decision state differs";
                    log.LogInfo("ACTION-MODEL-" + (record.Difference == null ? "MATCH" : "MISMATCH") + " index=" + record.Index);
                }
                else log.LogInfo("ACTION-MODEL-UNSUPPORTED " + record.Predicted.Reason);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal sealed class Record
        {
            public int Index { get; set; }
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction Action { get; set; } = null!;
            public BattleActionResult Predicted { get; set; } = null!;
            public BattleTurnState? Actual { get; set; }
            public RoomOutcome ActualOutcome { get; set; }
            public string? Difference { get; set; }
        }
    }
}
