using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBalatroEventCavendishScenario
    {
        internal const string Name = "original-balatro-event-cavendish";
        internal const string OwnerId = "24c4ad26-f2b3-4a95-bddc-caab94bd4767";
        internal const string OwnerKey = "BalatroEventCavendish";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static int MonsterCardId { get; private set; }
        internal static int ChampionCardId { get; private set; }
        internal static int SpellCardId { get; private set; }
        internal static bool EndCombatRestored { get; private set; }
        internal static bool CardlessCallbacksRestored { get; private set; }
        internal static List<object> EndCombatChecks { get; } = new List<object>();
        internal static List<object> CardlessCallbacks { get; } = new List<object>();
        private static readonly List<object> endCombatEvents = new List<object>();
        private static readonly List<int> endCombatRolls = new List<int>();
        private static bool capturingEndCombat;
        private static SubtypeData? championSubtype;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("BalatroEventCavendish must be acquired once before battle in an isolated save.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            CheckOriginal(relic);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-BALATRO-EVENT-CAVENDISH-ACQUIRED unchanged owner=" + relic.name + " id=" + relic.GetID() + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("BalatroEventCavendish requires one unchanged owner at deployment.");
            CollectableRelicData definition = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            CheckOriginal(definition);

            CardState[] owned = managers.GetCardManager()!.GetAllCards(new System.Collections.Generic.List<CardState>()).ToArray();
            CardState monster = owned.FirstOrDefault(card => card.GetCardType() == CardType.Monster && !card.IsChampionCard() &&
                card.GetSpawnCharacterData()?.GetAttackDamage() > 0 && !HasAttackOverride(card))
                ?? throw new InvalidOperationException("BalatroEventCavendish needs an ordinary positive-attack Monster card.");
            CardState champion = owned.FirstOrDefault(card => card.GetCardType() == CardType.Monster && card.IsChampionCard() &&
                card.GetSpawnCharacterData() != null)
                ?? throw new InvalidOperationException("BalatroEventCavendish needs an owned champion Monster control.");
            CardState spell = owned.FirstOrDefault(card => card.GetCardType() == CardType.Spell)
                ?? throw new InvalidOperationException("BalatroEventCavendish needs an owned Spell control.");
            championSubtype = champion.GetSpawnCharacterData()!.GetSubtypes().FirstOrDefault(item => item.IsChampion)
                ?? throw new InvalidOperationException("BalatroEventCavendish champion card lacks its native Champion subtype.");

            CaptureReady = true;
            RelicManager relicManager = managers.GetRelicManager();
            foreach (CardState card in new[] { monster, champion, spell })
            {
                relicManager.ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                relicManager.ApplyCardStateModifiers(card);
            }
            CaptureEndCombatPaths(managers, definition, log);
            MonsterCardId = FullBattleTrace.Active!.CardId(monster);
            ChampionCardId = FullBattleTrace.Active.CardId(champion);
            SpellCardId = FullBattleTrace.Active.CardId(spell);
            CaptureReady = false;
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged")!.SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-BALATRO-EVENT-CAVENDISH-PREPARED unchanged 3x unmodified Monster attack; ordinary=" + MonsterCardId +
                " champion-control=" + ChampionCardId + " spell-control=" + SpellCardId + ".");
        }

        private static void CaptureEndCombatPaths(AllGameManagers managers, CollectableRelicData definition, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            RelicManager relicManager = managers.GetRelicManager();
            RelicState owner = save.GetCollectedRelics().Single(item => item.GetRelicDataID() == OwnerId);
            var attack = owner.GetEffects().OfType<RelicEffectModifyCardAttack>().Single();
            var contributions = AppliedCardAttacks(attack);
            object[] initialDeck = DeckSnapshot(save);
            CaptureBranch(1, "miss", owner, contributions, initialDeck, managers);

            if (!save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Cavendish miss unexpectedly removed its owner.");
            CaptureBranch(0, "hit", owner, contributions, initialDeck, managers);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Cavendish hit did not remove its owner.");

            save.AddRelic(definition);
            if (save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1 ||
                !DeckSnapshot(save).SequenceEqual(initialDeck))
                throw new InvalidOperationException("Reacquiring Cavendish did not restore its original tracked deck modifiers.");
            EndCombatRestored = true;
            log.LogInfo("ORIGINAL-BALATRO-EVENT-CAVENDISH-END-COMBAT PASS: deterministic miss retained the owner; hit removed it and undid tracked deck attack; reacquisition restored the modifier.");
        }

        internal static void CaptureCardlessCallbacks(AllGameManagers managers, ManualLogSource log)
        {
            if (!Prepared || FullBattleTrace.Active?.NativeWon != true || CardlessCallbacks.Count != 0)
                throw new InvalidOperationException("Cavendish cardless callbacks require one terminal capture after preparation.");

            SaveManager save = managers.GetSaveManager();
            RelicState owner = save.GetCollectedRelics().Single(item => item.GetRelicDataID() == OwnerId);
            RelicEffectModifyCardAttack attack = owner.GetEffects().OfType<RelicEffectModifyCardAttack>().Single();
            SubtypeData nativeChampionSubtype = championSubtype
                ?? throw new InvalidOperationException("Cavendish native Champion subtype was not retained from preparation.");

            var candidates = new List<CharacterState>();
            managers.GetMonsterManager()!.AddCharactersInTowerToList(candidates);
            CharacterState ordinary = candidates.FirstOrDefault(unit => unit.IsAlive && !unit.IsDestroyed && !unit.IsPyreHeart() &&
                !unit.IsChampion() && !unit.GetIsClone() && !unit.HasStatusEffect("cardless") && unit.GetUnmodifiedAttackDamage() > 0)
                ?? throw new InvalidOperationException("Cavendish cardless callback probe needs a surviving positive-attack ordinary Monster.");

            CardlessCallbacks.Add(CaptureCardlessCallback("ordinary", ordinary, false, null, attack, owner, managers));
            CardlessCallbacks.Add(CaptureCardlessCallback("champion-control", ordinary, true, nativeChampionSubtype, attack, owner, managers));
            CardlessCallbacksRestored = true;
            log.LogInfo("ORIGINAL-BALATRO-EVENT-CAVENDISH-CARDLESS PASS: native OnCharacterAdded applied 2x unmodified attack to a cardless ordinary unit and excluded the Champion subtype; actor state restored.");
        }

        private static object CaptureCardlessCallback(string branch, CharacterState character, bool champion,
            SubtypeData? championSubtype, RelicEffectModifyCardAttack attack, RelicState owner, AllGameManagers managers)
        {
            var subtypeField = AccessTools.Field(typeof(CharacterState), "subtypes")!;
            var originalSubtypes = (List<SubtypeData>)subtypeField.GetValue(character);
            if (champion != (championSubtype != null))
                throw new InvalidOperationException("Cavendish Champion control requires exactly one native Champion subtype.");
            if (champion)
            {
                if (character.IsChampion()) throw new InvalidOperationException("Cavendish ordinary callback actor unexpectedly has a Champion subtype.");
                var controlSubtypes = new List<SubtypeData>(originalSubtypes) { championSubtype! };
                subtypeField.SetValue(character, controlSubtypes);
            }

            bool isChampion = character.IsChampion();
            if (isChampion != champion || character.HasStatusEffect("cardless"))
                throw new InvalidOperationException("Cavendish cardless callback actor did not enter its isolated branch.");
            int attackBefore = character.GetAttackDamage();
            int unmodifiedBefore = character.GetUnmodifiedAttackDamage();
            try
            {
                character.AddStatusEffect("cardless", 1);
                Drain(attack.OnCharacterAdded(character, null!, managers.GetCoreManagers()));
                int attackAfter = character.GetAttackDamage();
                int unmodifiedAfter = character.GetUnmodifiedAttackDamage();
                int observed = unmodifiedAfter - unmodifiedBefore;
                int expected = champion ? 0 : unchecked(2 * unmodifiedBefore);
                if (observed != expected)
                    throw new InvalidOperationException("Cavendish " + branch + " callback changed unmodified attack by " + observed + ", expected " + expected + ".");
                return new
                {
                    Branch = branch,
                    Callback = "RelicEffectModifyCardAttack.OnCharacterAdded",
                    IsCardless = character.HasStatusEffect("cardless"),
                    IsChampion = isChampion,
                    IsClone = character.GetIsClone(),
                    AttackBefore = attackBefore,
                    AttackAfter = attackAfter,
                    UnmodifiedAttackBefore = unmodifiedBefore,
                    UnmodifiedAttackAfter = unmodifiedAfter,
                    ExpectedModifier = expected,
                    ObservedModifier = observed
                };
            }
            finally
            {
                int remainingBuff = character.GetUnmodifiedAttackDamage() - unmodifiedBefore;
                if (remainingBuff > 0) character.DebuffDamage(remainingBuff, owner);
                if (character.HasStatusEffect("cardless")) character.RemoveStatusEffect("cardless", 1);
                if (champion) subtypeField.SetValue(character, originalSubtypes);
                if (character.GetUnmodifiedAttackDamage() != unmodifiedBefore || character.IsChampion())
                    throw new InvalidOperationException("Cavendish cardless callback probe failed to restore its actor.");
            }
        }

        private static void CaptureBranch(int expectedRoll, string branch, RelicState owner,
            object[] contributions, object[] initialDeck, AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            RelicManager relicManager = managers.GetRelicManager();
            int seed = ResetBattleRngForRoll(expectedRoll);
            uint[] beforeRng = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            object[] beforeDeck = DeckSnapshot(save);
            endCombatEvents.Clear(); endCombatRolls.Clear(); capturingEndCombat = true;
            try
            {
                object parametersSource = AccessTools.Field(typeof(RelicManager), "relicEffectParams").GetValue(relicManager);
                var parameters = new EndOfTurnRelicEffectParams((RelicEffectParams)parametersSource);
                RelicEffectChanceToDestroySelf destroy = owner.GetEffects().OfType<RelicEffectChanceToDestroySelf>().Single();
                RelicEffectBalatroBananaShake attempt = owner.GetEffects().OfType<RelicEffectBalatroBananaShake>().Single();
                Drain(destroy.ApplyEffectEndOfCombat(parameters, managers.GetCoreManagers()));
                Drain(attempt.ApplyEffectEndOfCombat(parameters, managers.GetCoreManagers()));
            }
            finally { capturingEndCombat = false; }
            uint[] afterRng = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            EndCombatChecks.Add(new
            {
                Branch = branch,
                Seed = seed,
                ExpectedRoll = expectedRoll,
                ObservedRolls = endCombatRolls.ToArray(),
                BattleRngBefore = beforeRng,
                BattleRngAfter = afterRng,
                OwnerPresentBefore = true,
                OwnerPresentAfter = save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId),
                Contributions = contributions,
                InitialDeck = initialDeck,
                DeckBefore = beforeDeck,
                DeckAfter = DeckSnapshot(save),
                Events = endCombatEvents.ToArray()
            });
        }

        private static int ResetBattleRngForRoll(int expectedRoll)
        {
            RandomState state = (RandomState)AccessTools.Field(typeof(RandomManager), "_randomState").GetValue(null);
            HadesRNG battle = state.FindState(RngId.Battle).rng;
            for (int seed = 1; seed < 100000; seed++)
            {
                battle.Init(seed);
                int roll = RandomManager.Range(0, 100, RngId.Battle);
                if (roll == expectedRoll)
                {
                    battle.Init(seed);
                    return seed;
                }
            }
            throw new InvalidOperationException("Could not seed the isolated Battle RNG for Cavendish roll " + expectedRoll + ".");
        }

        private static object[] AppliedCardAttacks(RelicEffectModifyCardAttack effect)
        {
            var tracking = (IEnumerable)AccessTools.Field(typeof(RelicEffectModifyCardAttack), "_appliedEffects").GetValue(effect);
            return tracking.Cast<object>().Select(item =>
            {
                Type tuple = item.GetType();
                CardState card = (CardState)tuple.GetField("Item1")!.GetValue(item);
                return new { CardId = card.GetID(), CardInstanceId = FullBattleTrace.Active!.CardId(card),
                    Amount = (int)tuple.GetField("Item2")!.GetValue(item),
                    InDeckState = AllGameManagers.Instance!.GetSaveManager().GetDeckState().Contains(card) };
            }).OrderBy(item => item.CardId, StringComparer.Ordinal).Cast<object>().ToArray();
        }

        private static object[] DeckSnapshot(SaveManager save) => save.GetDeckState().OrderBy(card => card.GetID(), StringComparer.Ordinal)
            .Select(card => (object)new { CardId = card.GetID(), CardInstanceId = FullBattleTrace.Active!.CardId(card),
                Attack = card.GetTotalAttackDamage(),
                TemporaryDamage = card.GetTemporaryCardStateModifiers().GetAdditionalDamage().Sum() }).ToArray();

        private static void Drain(IEnumerator routine)
        {
            try { while (routine.MoveNext()) if (routine.Current is IEnumerator nested) Drain(nested); }
            finally { (routine as IDisposable)?.Dispose(); }
        }

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.GetID() != OwnerId || relic.name != OwnerKey || relic.DisallowInDeploymentPhase || effects.Length != 3)
                throw new InvalidOperationException("The original BalatroEventCavendish identity or effect order changed.");
            RelicEffectData attack = effects[0];
            if (attack.GetEffectClassName() != "RelicEffectModifyCardAttack" || attack.GetParamString().Length != 0 ||
                attack.GetParamInt() != 0 || attack.GetParamFloat() != 0 || attack.GetParamInt2() != 3 ||
                attack.GetParamCardType() != CardType.Monster || !attack.GetParamBool3() || attack.GetParamBool() ||
                attack.GetParamBool2() || attack.GetParamBool4() || attack.GetParamCardFilter() != null ||
                attack.GetParamCharacterSubtype()?.IsNone != true || attack.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("The original BalatroEventCavendish attack payload changed.");
            if (effects[1].GetEffectClassName() != "RelicEffectChanceToDestroySelf" || effects[1].GetParamInt() != 1 ||
                effects[1].GetEffectConditions().Count != 0 || effects[2].GetEffectClassName() != "RelicEffectBalatroBananaShake" ||
                effects[2].GetEffectConditions().Count != 0)
                throw new InvalidOperationException("The original BalatroEventCavendish terminal payload changed.");
        }

        private static bool HasAttackOverride(CardState card)
        {
            var statuses = new System.Collections.Generic.List<StatusEffectStackData>();
            if (!card.TryGetStatusEffects(statuses)) return true;
            return statuses.Any(status => status.statusId is "fixedattack" or "equalizer" or "armorattack");
        }

        [HarmonyPatch(typeof(RandomManager), nameof(RandomManager.Range), new[] { typeof(int), typeof(int), typeof(RngId) })]
        private static class BattleRangePatch
        {
            private static void Postfix(int min, int max, RngId rngId, int __result)
            {
                if (capturingEndCombat && min == 0 && max == 100 && rngId == RngId.Battle)
                    endCombatRolls.Add(__result);
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class TriggeredPatch
        {
            private static void Prefix(RelicState triggeredRelic, IRelicEffect triggeredEffect)
            {
                if (capturingEndCombat && triggeredRelic.GetRelicDataID() == OwnerId)
                    endCombatEvents.Add(new { Kind = "Triggered", Effect = triggeredEffect.GetType().Name });
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggerAttempted))]
        private static class AttemptedPatch
        {
            private static void Prefix(RelicState triggeredRelic, IRelicEffect triggeredEffect)
            {
                if (capturingEndCombat && triggeredRelic.GetRelicDataID() == OwnerId)
                    endCombatEvents.Add(new { Kind = "Attempted", Effect = triggeredEffect.GetType().Name });
            }
        }
    }
}
