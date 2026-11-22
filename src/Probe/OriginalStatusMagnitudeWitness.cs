#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusMagnitudeWitness
    {
        internal const string Name = "original-status-magnitude-witness";
        internal const string MoonName = "original-status-magnitude-moon-witness";
        internal const string RoomSpellsName = "original-room-status-spells";
        internal const string TargetedSpellsName = "original-targeted-status-spells";
        private const string OpeningSpellsPrefix = "original-opening-spells:";
        private const string Module = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static readonly (string Id, string Key, string Status)[] Owners =
        {
            ("622860d3-b2ca-45b0-991a-b7745e1dcd5c", "DoubleMagebladeDamage", "mageblade"),
            ("aa94ad34-ee09-464e-89ff-2a2c2867c097", "StrongerSpikes", "spikes"),
            ("b9bddf11-acaa-40fb-bc40-eee83e478424", "IncreaseRageDamage", "buff"),
            ("1fd244de-7d7b-4345-974b-4e5bf287a3df", "AwokenRegen", "regen")
        };
        private static readonly (string Id, string Key, int Cost, string Status, int Stacks, int Chance)[] RoomSpells =
        {
            ("479ceb1b-fc8c-420f-8fa0-17683cee4511", "LavaFlow", 0, "pyregel", 4, 100),
            ("47df0371-7bd9-4bc2-b8e1-de61cc0060c7", "Gooeyflask", 1, "debuff", 2, 0),
            ("75ce1442-757b-4e9f-826d-8325f3dc7d9e", "EchoSnare", 1, "corrupt_poison", 5, 0),
            ("768a53f0-2743-4298-82c3-8541cbecf935", "JarOfFrost", 0, "poison", 8, 0),
            ("9ea70171-8afd-4fab-ad54-7ff5c8b478b8", "MagicJuice", 0, "spell weakness", 2, 0)
        };
        private static readonly (string Id, string Key, int Cost)[] TargetedCards =
        {
            (StewardId, "TrainStewardShield", 1),
            ("4ca4ed47-9867-4004-acf0-171c3282751f", "PerilsOfProduction", 0),
            ("a7207665-b6d1-4cd1-997e-8a7bb789c766", "EnergySiphon", 0),
            ("141494fe-0ac3-41ad-b1e6-05aeafe060f6", "ImmortalityPotion", 1),
            ("806fd915-b85c-4d0b-8490-fca08c2e0c0d", "ForevermoreSerum", 1),
            ("ec0980cf-9f88-4d38-9704-b89042f2f625", "SpikePotion", 1)
        };
        private static readonly (string Id, string Key, int Cost)[] Cards = TargetedSpells ? TargetedCards : DirectRoomSpells
            ? new[] { (StewardId, "TrainStewardShield", 1) }.Concat(RoomSpells.Select(spell => (spell.Id, spell.Key, spell.Cost))).ToArray()
            : new[]
        {
            (StewardId, "TrainStewardShield", 1),
            Moon ? ("2b7a187f-da0b-4c34-b8c5-b18755911b43", "SeleneAcolyte", 2)
                : ("63eba1e1-3118-44a3-8906-c8bfbe559667", "LunaCovenChamp2", 0),
            ("2ed78ce2-13c3-4e82-963f-99c9ea12d0f0", "RageSerum", 0),
            ("2ff4ef73-00f4-4bd5-b868-607320188980", "Sharpen", 2),
            ("d81f1f43-f20e-4459-9fc4-19e5836a876f", "StarterHeal", 1)
        };
        private static readonly List<object> Plays = new();
        private static CardData[]? definitions;
        private static CardState[]? hand;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static BattleTurnState? exchange;
        private static int[]? continuousCardIds;
        private static BattleTurnState? deploymentEndTurn;

        private static readonly string Modifier = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
        private static bool ConfiguredOpening => Modifier.StartsWith(OpeningSpellsPrefix, StringComparison.Ordinal);
        internal static bool Enabled => Modifier is Name or MoonName or RoomSpellsName or TargetedSpellsName || ConfiguredOpening;
        internal static bool Moon => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == MoonName;
        internal static bool DirectRoomSpells => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == RoomSpellsName;
        internal static bool TargetedSpells => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == TargetedSpellsName;
        internal static bool SpellCohort => DirectRoomSpells || TargetedSpells || ConfiguredOpening;
        internal static bool UsesMagnitudeRelics => Enabled && (!SpellCohort || TargetedSpells &&
            Environment.GetEnvironmentVariable("MT2_PROBE_TARGETED_STATUS_WITH_MAGNITUDE_RELICS") == "1");
        internal static bool Continuous => ConfiguredOpening || TargetedSpells || DirectRoomSpells && Environment.GetEnvironmentVariable("MT2_PROBE_ROOM_STATUS_FULL_BATTLE") == "1";
        internal static bool ContinuousPrepared => continuousCardIds != null;
        private static int DeploymentCount => SpellCohort ? 1 : 2;
        internal static bool Acquired => definitions != null;
        internal static bool ObservingExchange { get; private set; }
        internal static bool Completed { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (typeof(CardState).Module.ModuleVersionId != new Guid(Module) || save.PreviewMode || save.InTestScenario ||
                save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetCurrentDistance() != 0 ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("The original magnitude witness requires a fresh, unmodified 2.3.1 Ring 1.");
            AllGameData data = save.GetAllGameData();
            if (ConfiguredOpening)
            {
                definitions = ConfiguredDefinitions(data);
                return;
            }
            var originalOwners = (UsesMagnitudeRelics ? Owners : Owners.Take(0)).Select(expected =>
            {
                var relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == expected.Id);
                var effect = relic.GetEffects().Single();
                var status = effect.GetParamStatusEffects().Single();
                if (relic.name != expected.Key || relic.DisallowInDeploymentPhase ||
                    effect.GetEffectClassName() != nameof(RelicEffectModifyStatusMagnitude) ||
                    effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetParamInt() != 1 ||
                    effect.GetEffectConditions().Count != 0 || status.statusId != expected.Status || status.count != 0 ||
                    save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == expected.Id))
                    throw new InvalidOperationException("Original status magnitude owner changed: " + expected.Key);
                return relic;
            }).ToArray();
            definitions = Cards.Select((expected, index) =>
            {
                CardData card = data.FindCardData(expected.Id);
                if (card == null || card.name != expected.Key || card.GetCost() != expected.Cost)
                    throw new InvalidOperationException("Original status witness card changed: " + expected.Key);
                if (DirectRoomSpells && index >= DeploymentCount)
                {
                    var spell = RoomSpells[index - DeploymentCount];
                    var effect = card.GetEffects().Single();
                    var status = effect.GetParamStatusEffects().Single();
                    if (card.GetCardType() != CardType.Spell || card.GetTraits().Single().GetTraitStateName() != nameof(CardTraitExhaustState) ||
                        effect.GetEffectStateName() != nameof(CardEffectAddStatusEffect) || effect.GetTargetMode() != TargetMode.Room ||
                        effect.GetTargetTeamType() != Team.Type.Heroes || effect.GetParamInt() != spell.Chance ||
                        status.statusId != spell.Status || status.count != spell.Stacks)
                        throw new InvalidOperationException("Original room status spell changed: " + spell.Key);
                }
                if (TargetedSpells && index >= DeploymentCount)
                {
                    CardEffectData effect = card.GetEffects().First();
                    if (card.GetCardType() != CardType.Spell || effect.GetTargetMode() != TargetMode.DropTargetCharacter ||
                        effect.GetEffectStateName() != nameof(CardEffectAddStatusEffect))
                        throw new InvalidOperationException("Original targeted status spell changed: " + expected.Key);
                }
                return card;
            }).ToArray();
            foreach (var owner in originalOwners) save.AddRelic(owner);
        }

        private static CardData[] ConfiguredDefinitions(AllGameData data)
        {
            string[] ids = Modifier.Substring(OpeningSpellsPrefix.Length).Split(',');
            if (ids.Length is < 1 or > 5 || ids.Any(id => !Guid.TryParseExact(id, "D", out Guid parsed) || parsed.ToString() != id) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new InvalidOperationException("An original opening needs one to five distinct native card IDs.");

            CardData[] spells = ids.Select(id =>
            {
                CardData? card = data.FindCardData(id);
                if (card == null || card.GetID() != id || card.GetCardType() != CardType.Spell ||
                    !card.ShouldAppearInLogbook() || card.IsArchived || card.IsUnitAbility() || card.IsRoomAbility() ||
                    card.GetCostType() != CardData.CostType.Default || card.GetCost() < 0 || card.GetCardTriggers().Any() ||
                    card.GetTraits().Any(trait => trait.GetTraitStateName() is not (nameof(CardTraitExhaustState) or nameof(CardTraitPermafrost))))
                    throw new InvalidOperationException("Original opening requires a fixed-cost logbook spell: " + id);

                CardPlayRule rule = BattleActionProbe.Definition(card);
                CardEffectData[] effects = card.GetEffects().ToArray();
                if (rule.Effect != "Spell" || rule.ExternalInteractions.Count != 0 || rule.Effects.Count != effects.Length ||
                    effects.Length is < 1 or > 2 ||
                    effects[0].GetTargetMode() is not (TargetMode.DropTargetCharacter or TargetMode.Room or TargetMode.FrontInRoom) ||
                    effects.Any(effect => effect.GetUseIntRange()) ||
                    rule.Effects.Any(effect => effect.Type == "AddStatus" &&
                        (effect.Statuses.Count == 0 || effect.Statuses.All(status => status.Stacks <= 0) || effect.Value is not (0 or 100))) ||
                    effects.Select((effect, index) => effect.GetEffectStateName() switch
                    {
                        nameof(CardEffectDamage) or nameof(CardEffectHeal) or nameof(CardEffectAddStatusEffect) => index == 0 ||
                            effect.GetTargetMode() == effects[0].GetTargetMode() ||
                            effects[0].GetTargetMode() == TargetMode.DropTargetCharacter && effect.GetTargetMode() == TargetMode.LastTargetedCharacters,
                        nameof(CardEffectFloorRearrange) => index == 1 && effects[0].GetTargetMode() == TargetMode.DropTargetCharacter &&
                            effect.GetTargetMode() == TargetMode.LastTargetedCharacters && effect.GetParamInt() is 0 or 1,
                        _ => false
                    }).Any(supported => !supported))
                    throw new InvalidOperationException("Original opening spell is outside the admitted direct effects: " + card.name);

                return card;
            }).ToArray();
            if (spells.Sum(card => card.GetCost()) > 3)
                throw new InvalidOperationException("The configured opening spells exceed the stock three Ember.");
            return spells;
        }

        internal static void PrepareContinuous(AllGameManagers managers, ManualLogSource log)
        {
            var cards = managers.GetCardManager();
            var combat = managers.GetCombatManager();
            if (!Continuous || definitions == null || !combat.IsPlacementPhase || combat.GetTurnCount() != 0 ||
                cards.GetDiscardPile().Count != 0 || managers.GetPlayerManager().GetHasMoonPhaseCardEffect())
                throw new InvalidOperationException("Expected original room-status deployment before recording any inputs.");

            var controlled = definitions.Skip(ConfiguredOpening ? 0 : DeploymentCount).Select(definition =>
                cards.AddNewCard(definition, CardPile.DeckPileTop, false, false, animate: false) ??
                throw new InvalidOperationException("Original spell failed to enter draw pile: " + definition.name)).ToArray();
            List<CardState> draw = cards.GetDrawPile();
            CardState[] stock = draw.Where(card => !controlled.Contains(card)).ToArray();
            uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.CardDraw));
            var rng = new UnityRng(words[0], words[1], words[2], words[3]);
            // Shorter openings fill their unused slots from the ordinary stock-only shuffle.
            IReadOnlyList<CardState> openingStock = ConfiguredOpening && controlled.Length < 5 ? rng.Shuffle(stock).Items : stock;
            CardState[] opening = openingStock.Concat(controlled).ToArray();
            if (opening.Length != draw.Count) throw new InvalidOperationException("Original spell deck preparation lost an instance.");
            var permutation = rng.Shuffle(Enumerable.Range(0, draw.Count).ToArray()).Items;
            // Stage the controlled draws by inverting the upcoming native shuffle, leaving its RNG input untouched.
            for (int index = 0; index < opening.Length; index++) draw[permutation[index]] = opening[index];
            continuousCardIds = controlled.Select(FullBattleTrace.Active!.CardId).ToArray();
            log.LogInfo($"ORIGINAL-STATUS staged {controlled.Length} original spells in draw before recording deployment.");
        }

        internal static PlayCardAction? ChooseContinuous(BattleTurnState state, PlayCardAction? fallback)
        {
            CombatContext context = state.Spawn.Train.Context!;
            if (state.Spawn.Turn == 0 && fallback == null && context.Cards.Hand.Count > 0)
                throw new InvalidOperationException("Original status deployment must play the stock opening hand before shuffle.");
            if (state.Spawn.Turn != 1) return fallback;

            foreach (int id in continuousCardIds!)
            {
                if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") != 0) continue;
                CardToken? card = context.Cards.Hand.FirstOrDefault(item => item.InstanceId == id);
                if (card == null) throw new InvalidOperationException("Original opening shuffle did not draw all configured spells.");
                if (ConfiguredOpening) return ChooseConfiguredOpening(state, card);
                int targetId = 0;
                if (TargetedSpells)
                {
                    CardActionEffect effect = state.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).Effects[0];
                    bool hostile = effect.Statuses[0].Id == "spell weakness";
                    var targets = state.Spawn.Train.Rooms[0].Units.Where(unit => unit.Health > 0 &&
                        (hostile ? effect.AllowEnemy && unit.Team == CombatTeam.Enemy : effect.AllowPlayer && unit.Team == CombatTeam.Player));
                    targetId = (hostile ? targets.OrderByDescending(unit => unit.Attack) : targets).First().Id;
                }
                var action = new PlayCardAction(id, 0, targetUnitId: targetId);
                BattleActionResult result = BattleActionModel.PlayCard(state, action);
                if (!result.Supported) throw new InvalidOperationException("Original status spell could not be played: " + result.Reason);
                return action;
            }
            return fallback;
        }

        private static PlayCardAction ChooseConfiguredOpening(BattleTurnState state, CardToken card)
        {
            CardPlayRule rule = state.PlayRules!.Cards.Single(candidate => candidate.DataId == card.DataId);
            CardActionEffect effect = rule.Effects[0];
            CombatTeam team = effect.Type switch
            {
                "Damage" => CombatTeam.Enemy,
                "Heal" => CombatTeam.Player,
                "AddStatus" when !effect.AllowPlayer => CombatTeam.Enemy,
                "AddStatus" when !effect.AllowEnemy => CombatTeam.Player,
                // Armor protects its recipient even when the native definition allows either team.
                "AddStatus" when effect.Target == "DropTargetCharacter" &&
                    effect.Statuses.Count == 1 && effect.Statuses[0].Id == "armor" &&
                    StatusEffectManager.Instance.GetStatusEffectDataById("armor")?.GetStatusEffectStateName() == nameof(StatusEffectArmorState)
                    => CombatTeam.Player,
                "AddStatus" => StatusEffectManager.Instance.GetStatusEffectDataById(effect.Statuses.First().Id)?.GetDisplayCategory() switch
                {
                    StatusEffectData.DisplayCategory.Positive => CombatTeam.Player,
                    StatusEffectData.DisplayCategory.Negative => CombatTeam.Enemy,
                    _ => throw new InvalidOperationException("Original opening status has no unambiguous team: " + rule.AssetKey)
                },
                _ => throw new InvalidOperationException("Original opening spell lost its admitted effect: " + rule.AssetKey)
            };
            BattleActionEnumeration actions = BattleActionModel.EnumerateActions(state, card.InstanceId, includeAbilities: false);
            return actions.SupportedCandidates
                .Where(candidate => HasOpeningEffect(state, candidate, effect, team))
                .OrderBy(candidate => candidate.Action.RoomIndex)
                .Select(candidate => candidate.Action)
                .FirstOrDefault() ?? throw new InvalidOperationException("No supported effective original opening play for " + rule.AssetKey +
                    ": " + string.Join(", ", actions.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()));
        }

        private static bool HasOpeningEffect(BattleTurnState before, SupportedActionCandidate candidate, CardActionEffect effect, CombatTeam team)
        {
            PlayCardAction action = candidate.Action;
            var resultingUnits = candidate.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
            var targets = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units
                .Where(unit => !unit.IsPyre && unit.Health > 0 && unit.Team == team && (action.TargetUnitId == 0 || action.TargetUnitId == unit.Id));
            return targets.Any(unit =>
            {
                resultingUnits.TryGetValue(unit.Id, out CombatUnit? result);
                return effect.Type switch
                {
                    "Damage" => result == null || result.Health < unit.Health ||
                        new[] { "armor", "damage shield", "spell shield" }.Any(status => Stacks(result, status) < Stacks(unit, status)),
                    "Heal" => result != null && result.Health > unit.Health,
                    "AddStatus" => result != null && effect.Statuses.Any(status => Stacks(result, status.Id) > Stacks(unit, status.Id)),
                    _ => false
                };
            });
        }

        private static int Stacks(CombatUnit unit, string status) => unit.Statuses.Where(item => item.Id == status).Sum(item => item.Stacks);

        internal static bool Tick(AllGameManagers managers, ManualLogSource log)
        {
            var trace = FullBattleTrace.Active!;
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (definitions == null) throw new InvalidOperationException("Original magnitude relics were not acquired.");
            if ((!Moon || hand != null) && managers.GetPlayerManager().GetHasMoonPhaseCardEffect() != Moon)
                throw new InvalidOperationException("Original magnitude witness has unexpected native moon tracking.");
            if (Moon && deploymentEndTurn != null && !combat.IsPlacementPhase)
            {
                CaptureDeploymentEndTurn(trace, deploymentEndTurn);
                deploymentEndTurn = null;
            }
            // Card controls need floor zero. The exchange successor keeps native camera focus.
            if (exchange == null && rooms.GetSelectedRoom() != 0)
            {
                managers.GetSaveManager().StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }

            int index = Plays.Count;
            if (before != null)
            {
                var after = trace.CaptureDecision();
                CardState played = hand![combat.IsPlacementPhase ? index : index - DeploymentCount];
                if (cards.GetHand().Contains(played) || before.Energy - after.Energy != Cards[index].Cost)
                    throw new InvalidOperationException("Original magnitude play did not settle: " + Cards[index].Key);
                Plays.Add(new { Card = Cards[index].Key, Action = action, Before = before, After = after });
                before = null;
                return false;
            }

            if (exchange != null)
            {
                ObservingExchange = false;
                var after = trace.CaptureDecision();
                if (after.Spawn.Turn != 2) throw new InvalidOperationException("Original magnitude first exchange did not settle.");
                using var document = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = DirectRoomSpells ? "OriginalRoomStatusSpells" : Moon ? "OriginalMoonStatusMagnitudeWitness" : "OriginalStatusMagnitudeWitness",
                    GameVersion = UnityEngine.Application.version,
                    GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Plays, Exchange = new { Before = exchange, After = after },
                    Queries = RelicStatusMagnitudeProbe.Queries
                });
                string filename = DirectRoomSpells ? "room-status-spells.mt2f" : Moon ? "status-magnitude-moon.mt2f" : "status-magnitude.mt2f";
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, filename));
                document.Write(stream);
                Completed = true;
                log.LogInfo("ORIGINAL-STATUS-WITNESS captured " + Plays.Count + " unchanged original card plays and first native combat.");
                return false;
            }

            if (combat.IsPlacementPhase && index == DeploymentCount)
            {
                if (Moon) deploymentEndTurn = trace.CaptureDecision();
                return true;
            }
            if (index == Cards.Length)
            {
                exchange = trace.CaptureDecision();
                ObservingExchange = true;
                return true;
            }

            bool deploying = combat.IsPlacementPhase;
            if (!deploying && combat.GetTurnCount() != 1)
                throw new InvalidOperationException("Expected original spells during the first normal turn.");
            if (hand == null || !deploying && hand.Length == DeploymentCount)
            {
                hand = (deploying ? definitions.Take(DeploymentCount) : definitions.Skip(DeploymentCount)).Select(definition =>
                    cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == definition.GetID()) ??
                    cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) ??
                    throw new InvalidOperationException("Original magnitude card could not reach hand: " + definition.name)).ToArray();
                return false;
            }

            var room = rooms.GetRoom(0);
            CardState next = hand[deploying ? index : index - DeploymentCount];
            var allies = new List<CharacterState>();
            room.AddCharactersToList(allies, Team.Type.Monsters);
            CharacterState? target = null;
            if (!deploying && !DirectRoomSpells)
            {
                string targetKey = index == DeploymentCount ? definitions[1].GetSpawnCharacterData().GetAssetKey() : "TrainStewardBig";
                target = allies.Single(unit => unit.IsAlive && unit.GetSourceCharacterData().GetAssetKey() == targetKey);
            }
            SpawnPoint? point = deploying ? room.GetFirstEmptyMonsterPoint() : target?.GetSpawnPoint();
            before = trace.CaptureDecision();
            action = new PlayCardAction(trace.CardId(next), 0, deploying ? allies.Count : -1,
                targetUnitId: target == null ? 0 : trace.UnitId(target));
            int handIndex = cards.GetHand().IndexOf(next);
            if (handIndex < 0 || !cards.CanPlayHandCard(next, 0, point, null, null, out var error))
                throw new InvalidOperationException("Original native magnitude play unavailable: " + Cards[index].Key);
            if (!cards.PlayCard(handIndex, point, ref error))
                throw new InvalidOperationException("Original native magnitude play was rejected: " + error);
            return false;
        }

        private static void CaptureDeploymentEndTurn(FullBattleTrace trace, BattleTurnState before)
        {
            // Observe the first settled native turn before the spell controls
            // add cards to hand or select floor zero again.
            BattleTurnState after = trace.CaptureDecision();
            var phases = trace.TrainPhaseRecords.Where(phase => phase.Turn == before.Spawn.Turn).ToArray();
            if (before.Spawn.Turn != 0 || after.Spawn.Turn != 1 ||
                !phases.Select(phase => phase.Kind).SequenceEqual(new[] { "Combat", "Ascend" }) ||
                phases.Any(phase => phase.Actual == null))
                throw new InvalidOperationException("Original magnitude deployment did not produce complete native decision and train boundaries.");

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalMoonStatusMagnitudeDeployment",
                GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                Deployment = new { Before = before, After = after }, TrainPhases = phases
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "status-magnitude-moon-deployment.mt2f"));
            document.Write(stream);
        }
    }
}
#endif
