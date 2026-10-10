using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardUpgradeMaskCalibration
    {
        private static void SetMaskField(CardUpgradeMaskData mask, string field, object value)
        {
            var member = AccessTools.Field(typeof(CardUpgradeMaskData), field);
            if (member.FieldType.IsEnum) value = Enum.ToObject(member.FieldType, value);
            else if (!member.FieldType.IsInstanceOfType(value) && member.FieldType.IsGenericType &&
                member.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                value = Activator.CreateInstance(member.FieldType, new[] { value });
            member.SetValue(mask, value);
        }
        internal static CardUpgradeMaskCard Card(ICardDataAccessible card, RelicManager? manager)
        {
            CharacterData? spawn = card.GetSpawnCharacterData();
            var traits = card.GetTraits();
            var effects = card.GetEffects();
            var effectNames = effects.Select(effect => effect.GetEffectStateName()).ToList();
            void CastEffects(IEnumerable<CardTriggerEffectData> triggers)
            {
                foreach (var trigger in triggers.Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast))
                    effectNames.AddRange(trigger.GetCardEffects().Select(effect => effect.GetEffectStateName()));
            }
            CastEffects(card.GetCardTriggers());
            foreach (var trait in traits)
                if (trait.GetCardUpgradeDataParam() != null) CastEffects(trait.GetCardUpgradeDataParam().GetCardTriggerUpgrades());
            var statuses = new List<StatusEffectStackData>();
            card.TryGetStatusEffects(statuses);
            bool ignoreTemporaryCost = false;
            var state = card as CardState;
            if (state != null && manager != null && manager.HasPermanentUpgradeChangeRelicEffects(state, out bool applied)) ignoreTemporaryCost = applied;
            UpgradeMaskStatus Status(StatusEffectStackData status) => new UpgradeMaskStatus(status.statusId, status.count, status.fromPermanentUpgrade);
            return new CardUpgradeMaskCard(card.GetID(), card.GetCardType().ToString(), card.GetRarity().ToString(), card.IsSpawnerCard(),
                spawn != null && spawn.GetCanAttack(), spawn?.GetSubtypes().Select(subtype => subtype.Key).ToArray() ?? Array.Empty<string>(),
                statuses.Select(Status).ToArray(), effects.Select(effect => (IReadOnlyList<UpgradeMaskStatus>)
                    effect.GetParamStatusEffectStackData().Select(Status).ToArray()).ToArray(),
                traits.Select(trait => trait.GetTraitStateName()).ToArray(), effectNames, card.GetLinkedClassID(),
                card.GetCostWithoutTraits(ignoreTemporaryCost), card.IsConsumeRemainingEnergyCostType(), card.GetSize(false),
                (int)card.GetCardTargetMode(), state != null,
                state != null && (spawn?.GetUnitAbilityCardData() != null || state.HasUnitAbilityUpgrade()),
                state != null && ((!state.IsPurified && spawn?.GetGraftedEquipment() != null) || state.GetCardStateModifiers().GraftedEquipmentCardState != null),
                state?.GetVisibleUpgradeCount(true, false) ?? 0,
                state == null ? Array.Empty<string>() : state.GetCardStateModifiers().GetCardUpgrades()
                    .Concat(state.GetTemporaryCardStateModifiers().GetCardUpgrades()).Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray());
        }

        internal static CardUpgradeMaskCharacter Character(CharacterState character)
        {
            var statuses = new List<CharacterState.StatusEffectStack>();
            character.GetStatusEffects(ref statuses, includeZeroStacks: true);
            return new CardUpgradeMaskCharacter(character.GetSubtypes().Select(subtype => subtype.Key).ToArray(),
                statuses.Select(status => status.State.GetStatusId()).ToArray(), character.GetSize());
        }

        private static object[] EdgeCases(CardData[] cards, CardState[] owned, CharacterState target, RelicManager relics, AllGameManagers managers)
        {
            var rows = new List<object>();
            var objects = new List<UnityEngine.Object>();
            object targetState = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(target);
            var statusField = AccessTools.Field(targetState.GetType(), "statusEffects");
            var originalStatuses = (Dictionary<string, CharacterState.StatusEffectStack>)statusField.GetValue(targetState);
            try
            {
                var source = cards.First(card => card.IsSpawnerCard() && card.GetSpawnCharacterData()!.GetSubtypes().Count >= 2);
                var unitData = UnityEngine.Object.Instantiate(source.GetSpawnCharacterData()!); objects.Add(unitData);
                var unitCard = UnityEngine.Object.Instantiate(source); objects.Add(unitCard);
                var cardEffects = source.GetEffects().Select(effect => (CardEffectData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(effect, null)).ToList();
                AccessTools.Field(typeof(CardData), "effects").SetValue(unitCard, cardEffects);
                AccessTools.Field(typeof(CardEffectData), "paramCharacterData").SetValue(cardEffects.Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster"), unitData);
                var ownedUnit = (CardState)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(owned.First(card => card.IsSpawnerCard()), null);
                AccessTools.Field(typeof(CardState), "cardModifiers").SetValue(ownedUnit, new CardStateModifiers());
                AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(ownedUnit, new CardStateModifiers());
                var host = new UnityEngine.GameObject("Poju isolated upgrade mask manager") { hideFlags = UnityEngine.HideFlags.HideAndDontSave };
                host.SetActive(false); objects.Add(host);
                var markerManager = host.AddComponent<RelicManager>();
                var markerHeroes = host.AddComponent<HeroManager>();
                var marker = new RelicState(); marker.GetEffects().Add(new RelicEffectMonstersAreAllSubtypes());
                markerHeroes.GetHeroBlessings().Add(marker);
                AccessTools.Field(typeof(RelicManager), "heroManager").SetValue(markerManager, markerHeroes);
                if (markerManager.GetRelicEffect<RelicEffectMonstersAreAllSubtypes>() == null) throw new InvalidOperationException("Isolated all-subtypes marker missing.");
                void CardCase(string name, ICardDataAccessible? card, Action<Action<string, object>> configure, bool allTypes = false)
                {
                    var mask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>(); objects.Add(mask); mask.name = name;
                    void Set(string field, object value)
                    {
                        SetMaskField(mask, field, value);
                    }
                    configure(Set);
                    RelicManager manager = allTypes ? markerManager : relics;
                    rows.Add(new { Name = name, Kind = "Card", Mask = CardUpgradeMaskProbe.Rule(mask),
                        Card = card == null ? null : Card(card, manager), Character = (CardUpgradeMaskCharacter?)null,
                        MonstersAreAllSubtypes = allTypes, Actual = mask.FilterCard(card!, manager) });
                }
                void CharacterCase(string name, Action<Action<string, object>> configure, bool allTypes = false)
                {
                    var mask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>(); objects.Add(mask); mask.name = name;
                    void Set(string field, object value)
                    {
                        SetMaskField(mask, field, value);
                    }
                    configure(Set);
                    rows.Add(new { Name = name, Kind = "Character", Mask = CardUpgradeMaskProbe.Rule(mask), Card = (CardUpgradeMaskCard?)null,
                        Character = Character(target), MonstersAreAllSubtypes = allTypes,
                        Actual = mask.FilterCharacter(target, allTypes ? markerManager : relics) });
                }
                StatusEffectStackData[] Armor(int count) => new[] { new StatusEffectStackData { statusId = "armor", count = count } };
                foreach (int actual in new[] { 0, 2, 3 })
                foreach (int threshold in new[] { 2, 3 })
                foreach (bool excluded in new[] { false, true })
                {
                    AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(unitData, Armor(actual));
                    CardCase($"status-{(excluded ? "exclude" : "require")}-{threshold}-actual-{actual}", unitCard,
                        set => set(excluded ? "excludedStatusEffects" : "requiredStatusEffects", Armor(threshold)));
                }
                string subtype = unitData.GetSubtypes()[0].Key;
                foreach (int op in new[] { 0, 1 })
                    CardCase("excluded-subtype-operator-" + op, unitCard, set => { set("excludedSubtypes", new List<string> { subtype }); set("excludedSubtypesOperator", op); });
                foreach (bool allTypes in new[] { false, true })
                    CardCase("all-subtypes-" + allTypes, unitCard, set => set("requiredSubtypes", new List<string> { "missing-native-subtype" }), allTypes);
                CardCase("conflicting-required-sizes", unitCard, set => set("requiredSizes", new List<int> { unitCard.GetSize(), unitCard.GetSize() + 1 }));
                foreach (bool x in new[] { false, true })
                foreach (int cost in new[] { -5, 2, 9 })
                {
                    AccessTools.Field(typeof(CardState), "cost").SetValue(ownedUnit, cost);
                    AccessTools.Field(typeof(CardState), "costType").SetValue(ownedUnit, x ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
                    CardCase($"cost-{cost}-x-{x}", ownedUnit, set => set("costRange", new UnityEngine.Vector2(2.5f, 8.5f)));
                }
                foreach (int flags in new[] { 1, 3, 7 })
                    CardCase("target-flags-" + flags, cards.First(card => card.GetCardType() == CardType.Spell), set => set("cardTargetMode", flags));
                foreach (string flag in new[] { "excludeIfHasUnitAbility", "excludeIfHasAnyUpgrades", "excludeIfHasNoUpgrades" })
                {
                    CardCase("raw-definition-" + flag, unitCard, set => set(flag, true));
                    CardCase("owned-no-upgrade-" + flag, ownedUnit, set => set(flag, true));
                }
                var abilityData = managers.GetSaveManager().GetAllGameData().GetAllCardUpgradeData().First(upgrade => upgrade.GetUnitAbilityUpgrade() != null);
                var ability = new CardUpgradeState(); ability.Setup(abilityData);
                ownedUnit.GetTemporaryCardStateModifiers().GetCardUpgrades().Add(ability);
                foreach (string flag in new[] { "excludeIfHasUnitAbility", "excludeIfHasAnyUpgrades", "excludeIfHasNoUpgrades" })
                    CardCase("owned-ability-upgrade-" + flag, ownedUnit, set => set(flag, true));
                foreach (bool excluded in new[] { false, true })
                {
                    CardCase("raw-upgrade-id-" + excluded, unitCard, set => set(excluded ? "excludedCardUpgrades" : "requiredCardUpgrades", new List<CardUpgradeData> { abilityData }));
                    CardCase("owned-upgrade-id-" + excluded, ownedUnit, set => set(excluded ? "excludedCardUpgrades" : "requiredCardUpgrades", new List<CardUpgradeData> { abilityData }));
                }
                var visible = managers.GetSaveManager().GetAllGameData().GetAllCardUpgradeData().Where(upgrade => upgrade.GetBonusDamage() > 0)
                    .Select(upgrade => { var state = new CardUpgradeState(); state.Setup(upgrade); return state; }).First(upgrade => upgrade.ShouldShowIcon());
                ownedUnit.GetCardStateModifiers().GetCardUpgrades().Add(visible);
                foreach (string flag in new[] { "excludeIfHasAnyUpgrades", "excludeIfHasNoUpgrades" })
                    CardCase("owned-visible-upgrade-" + flag, ownedUnit, set => set(flag, true));
                CardCase("owned-no-graft", ownedUnit, set => set("excludeIfHasGraftedEquipment", true));
                AccessTools.Property(typeof(CardStateModifiers), "GraftedEquipmentCardState").SetValue(ownedUnit.GetCardStateModifiers(), owned[0]);
                CardCase("owned-permanent-graft", ownedUnit, set => set("excludeIfHasGraftedEquipment", true));
                AccessTools.Field(typeof(CardState), "purified").SetValue(ownedUnit, true);
                CardCase("owned-purified-permanent-graft", ownedUnit, set => set("excludeIfHasGraftedEquipment", true));
                CardCase("raw-graft-restriction", unitCard, set => set("excludeIfHasGraftedEquipment", true));
                CardCase("null-conflicting-cost-and-type", null, set => { set("cardType", (int)CardType.Spell); set("requireXCost", true); set("excludeXCost", true); });
                foreach (int count in new[] { 0, 3 })
                {
                    var copy = new Dictionary<string, CharacterState.StatusEffectStack>(originalStatuses);
                    copy["armor"] = new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create("armor", target)!, count);
                    statusField.SetValue(targetState, copy);
                    foreach (bool excluded in new[] { false, true })
                    foreach (int op in new[] { 0, 1 })
                        CharacterCase($"registered-armor-{count}-excluded-{excluded}-op-{op}", set =>
                        { set(excluded ? "excludedStatusEffects" : "requiredStatusEffects", Armor(999)); set(excluded ? "excludedStatusEffectsOperator" : "requiredStatusEffectsOperator", op); });
                    CharacterCase("character-ignores-card-fields-" + count, set => { set("cardType", (int)CardType.Spell); set("requireXCost", true); set("excludeXCost", true); set("requiredStatusEffects", Armor(999)); });
                }
                foreach (bool allTypes in new[] { false, true })
                    CharacterCase("character-all-subtypes-" + allTypes, set => set("requiredSubtypes", new List<string> { "missing-native-subtype" }), allTypes);
                return rows.ToArray();
            }
            finally
            {
                statusField.SetValue(targetState, originalStatuses);
                foreach (var item in objects.AsEnumerable().Reverse()) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_UPGRADE_MASKS") != "1") return;
            AllGameManagers managers = AllGameManagers.Instance!;
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            RelicManager relics = managers.GetRelicManager();
            var cards = data.GetAllCardData().ToArray();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            var characters = new List<CharacterState>();
            for (int room = 0; room < managers.GetRoomManager()!.GetNumRooms(); room++)
            foreach (Team.Type team in new[] { Team.Type.Monsters, Team.Type.Heroes })
                managers.GetRoomManager()!.GetRoom(room).AddCharactersToList(characters, team);
            var originalRelics = ((IEnumerable)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(data)).Cast<RelicData>().ToArray();
            var relicMasks = originalRelics.SelectMany(relic => relic.GetEffects()).Select(effect => effect.GetParamCardUpgradeData())
                .Where(upgrade => upgrade != null).SelectMany(upgrade => upgrade.GetFilters()).Distinct().ToArray();
            var masks = data.GetAllCardUpgradeData().SelectMany(upgrade => upgrade.GetFilters())
                .Concat(relicMasks).Concat(cards.SelectMany(card => card.GetEffects()).Select(effect => effect.GetParamCardFilter()).Where(mask => mask != null))
                .Distinct().OrderBy(mask => mask.name, StringComparer.Ordinal).ToArray();
            JObject Rng()
            {
                var value = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) value[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return value;
            }
            var beforeRng = Rng();
            var beforeContext = JToken.FromObject(trace.CaptureContext());
            var beforeUnits = JToken.FromObject(characters.Select(character => trace.CaptureUnit(character)).ToArray());
            int frame = UnityEngine.Time.frameCount;
            var facts = cards.Select(card => Card(card, relics)).ToArray();
            var ownedFacts = owned.Select(card => Card(card, relics)).ToArray();
            var characterFacts = characters.Select(Character).ToArray();
            var rows = masks.Select(mask => new
            {
                Mask = CardUpgradeMaskProbe.Rule(mask), OriginalRelicUpgradeMask = relicMasks.Contains(mask),
                NullCard = mask.FilterCard<CardData>(null!, relics),
                CardDataResults = cards.Select(card => mask.FilterCard(card, relics)).ToArray(),
                CardStateResults = owned.Select(card => mask.FilterCard(card, relics)).ToArray(),
                CharacterResults = characters.Select(character => mask.FilterCharacter(character, relics)).ToArray()
            }).ToArray();
            var edgeCases = EdgeCases(cards, owned, characters.Single(character => character.IsPyreHeart()), relics, managers);
            var afterRng = Rng();
            bool contextUnchanged = JToken.DeepEquals(beforeContext, JToken.FromObject(trace.CaptureContext())) &&
                JToken.DeepEquals(beforeUnits, JToken.FromObject(characters.Select(character => trace.CaptureUnit(character)).ToArray()));
            if (!contextUnchanged || !JToken.DeepEquals(beforeRng, afterRng) || frame != UnityEngine.Time.frameCount)
                throw new InvalidOperationException("Card upgrade mask calibration changed native context, units, RNG or frame.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 2, GameVersion = UnityEngine.Application.version, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = beforeRng, RngAfter = afterRng,
                ContextUnchanged = contextUnchanged, MonstersAreAllSubtypes = relics.GetRelicEffect<RelicEffectMonstersAreAllSubtypes>() != null,
                CardData = facts, CardStates = ownedFacts, Characters = characterFacts, Rows = rows, EdgeCases = edgeCases
            }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-upgrade-mask-calibration.mt2f")))
                document.Write(stream);
        }
    }
}
