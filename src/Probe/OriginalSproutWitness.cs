#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSproutWitness
    {
        private const string CarrierCardId = "b9788a77-50da-4654-acfa-960c23649984";
        private const string GravityId = "87e45427-921e-45d3-b4c4-9c156a83cedf";
        private const string Hunter = "RAny_Attacker_Hunter_Hivemind_Ver2";
        private const string KillerStatus = "hivemindkiller";
        private const string NativeGrowth = "RoomStateHivemindKillerModifierUpgrade";
        private static int CarrierRoom => Tier == 1 ? 2 : 0;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<Observation> observations = new();
        private static readonly List<DeathObservation> deaths = new();
        private static bool requestedFloor, deployed, drawnForKill;
        private static BattleTurnState? before, afterWave, beforeKill;
        private static PlayCardAction? killAction;
        private static CharacterState? killTarget;
        private static object? originalTriggers;
        private static int firstWaveObservation;
        private static int firstKillObservation;
        internal static bool Acquired { get; private set; }
        internal static int Tier => int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_SPROUT_WITNESS"), out int tier) ? tier : 0;
        internal static bool Enabled => Tier is >= 1 and <= 3;
        private static (string Owner, string Upgrade, int Value) Definition => Tier switch
        {
            1 => ("89f86b55-f867-42d4-9128-0ebc6c82bcaf", "7bdad45c-2981-452c-ac06-3aa5137f8ce4", 0),
            2 => ("f6bfdfc3-b8ff-466a-8e5e-7a080eb9c9be", "581706b5-1402-4f66-8324-9b76601ce360", 2),
            3 => ("da7a1058-271b-4436-a567-102c7566c991", "52c9e5c1-afc3-4096-94bc-a784ebd905da", 4),
            _ => throw new InvalidOperationException("Expected an original Sprout tier.")
        };

        internal static void Acquire(SaveManager save)
        {
            var (ownerId, upgradeId, value) = Definition;
            if (UnityEngine.Application.version != "2.3.1" || typeof(CardState).Assembly.ManifestModule.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 3 ||
                save.GetCurrentScenarioData()?.name != "SoulSavior_R2_Battle_TroopBuffFeed" ||
                save.GetSelectedSouls().Any(owner => owner.GetRelicDataID() == ownerId))
                throw new InvalidOperationException("Expected the original TroopBuffFeed checkpoint before adding Sprout.");
            SoulData soul = save.GetAllGameData().FindSoulData(ownerId);
            CardUpgradeData upgrade = soul.GetSoulCardUpgrade();
            var triggers = upgrade.GetCharacterTriggerUpgrades();
            if (soul.name != "SproutT" + Tier || soul.GetTierLevel() != Tier ||
                soul.GetEffects() is not [var effect] || effect.GetEffectClassName() != nameof(RelicEffectCardUpgrade) ||
                effect.GetParamCardType() != CardType.Monster || effect.GetParamInt() != value ||
                upgrade.GetID() != upgradeId || (Tier == 1 ? triggers.Count != 0 :
                    triggers is not [var death] || death.GetTrigger() != CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor) ||
                upgrade.GetStatusEffectUpgrades() is not [{ statusId: KillerStatus, count: 1 }])
                throw new InvalidOperationException("The original Sprout binding payload changed.");
            originalTriggers = triggers.Select(trigger => new
            {
                Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetEffects().Select(RuleCatalogProbe.Effect).ToArray()
            }).ToArray();

            CardState card = save.GetDeckState().Single(candidate => candidate.GetCardDataID() == CarrierCardId);
            save.AddSelectedSoul(soul);
            SoulState owner = save.GetSelectedSouls().Single(item => item.GetRelicDataID() == ownerId);
            SoulHelper.AttachSoulEffectToCard(owner, card, save);
            if (!card.GetCardStateModifiers().HasUpgrade(upgradeId))
                throw new InvalidOperationException("The original Soul Helper did not bind Sprout to the native carrier.");
            Acquired = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager();
            RoomManager rooms = managers.GetRoomManager();
            CardManager cards = managers.GetCardManager();
            if (!Acquired || save.PreviewMode) return false;
            int turn = combat.GetTurnCount();
            if (turn == 0 && !deployed)
            {
                if (rooms.GetSelectedRoom() != CarrierRoom)
                {
                    if (!requestedFloor)
                    {
                        requestedFloor = true;
                        save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(CarrierRoom));
                    }
                    return false;
                }
                CardState card = cards.GetHand().Single(candidate => candidate.GetCardDataID() == CarrierCardId &&
                    candidate.GetCardStateModifiers().HasUpgrade(Definition.Upgrade));
                SpawnPoint point = rooms.GetRoom(CarrierRoom).GetMonsterPoint(0);
                if (!cards.CanPlayHandCard(card, CarrierRoom, point, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(card), point, ref error))
                    throw new InvalidOperationException("The original Sprout carrier could not deploy: " + error);
                deployed = true;
                return false;
            }
            if (turn == 0)
            {
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }
            CharacterState carrier = managers.GetMonsterManager().GetCharactersReadOnly().Single(IsCarrier);
            if (carrier.GetCurrentRoomIndex() != CarrierRoom || carrier.GetStatusEffectStacks(KillerStatus) != 1)
                throw new InvalidOperationException("The active native Sprout carrier lost its room or status.");
            if (Tier > 1 && !carrier.GetTriggers().Any(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor))
                throw new InvalidOperationException("The active Sprout carrier lacks its original death callback.");
            if (turn == 1)
            {
                before = trace.CaptureDecision();
                firstWaveObservation = observations.Count;
                advanceTurn();
                return false;
            }

            int carrierUnitId = trace.UnitId(carrier);
            if (afterWave == null)
            {
                afterWave = trace.CaptureDecision();
                int expected = afterWave.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(actor => actor.Team == CombatTeam.Enemy && actor.Health > 0)
                    .Sum(actor => actor.Status("hivemind")?.Stacks ?? 0);
                Observation? final = observations.Skip(firstWaveObservation).LastOrDefault(row => row.ActorId == carrierUnitId);
                if (turn != 2 || before == null || expected <= 0 || final == null || final.TowerStacks != expected ||
                    final.After.ModifierAttack != expected || ReadGrowth(carrier).ModifierAttack != expected ||
                    final.After.TotalAttack - final.Before.TotalAttack != final.After.ModifierAttack - final.Before.ModifierAttack)
                    throw new InvalidOperationException("The native Sprout modifier did not track the original Hivemind wave.");
            }
            BattleTurnState? afterKill = null;
            if (Tier > 1)
            {
                if (killAction == null)
                {
                    PlayHunterKill(trace, save, rooms, cards);
                    return false;
                }
                afterKill = trace.CaptureDecision();
                if (deaths.Count == 0 || deaths.Any(row => row.CarrierId != carrierUnitId || row.DyingId != killAction.TargetUnitId) ||
                    afterKill.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == killAction.TargetUnitId && unit.Health > 0))
                    throw new InvalidOperationException("The original paid Hunter kill did not dispatch the bound Sprout callback.");
            }
            var (ownerId, upgradeId, value) = Definition;
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativeSproutT" + Tier + "Hivemind", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = Module, OwnerId = ownerId, UpgradeId = upgradeId, CarrierCardId, CarrierRoom, CarrierUnitId = carrierUnitId,
                Before = before, FirstWaveObservation = firstWaveObservation, Observations = observations, Actual = afterWave,
                Kill = Tier == 1 ? null : new { Value = value, Triggers = originalTriggers, DrawnForKill = drawnForKill,
                    FirstGrowthObservation = firstKillObservation, Before = beforeKill, Action = killAction, Actual = afterKill, Deaths = deaths }
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, $"sprout-t{Tier}-hivemind.mt2f"));
            document.Write(output);
            return true;
        }

        private static void PlayHunterKill(FullBattleTrace trace, SaveManager save, RoomManager rooms, CardManager cards)
        {
            if (rooms.GetSelectedRoom() != CarrierRoom)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(CarrierRoom));
                return;
            }
            CardState? gravity = cards.GetHand().SingleOrDefault(card => card.GetCardDataID() == GravityId);
            if (gravity == null)
            {
                gravity = cards.GetDrawPile().Concat(cards.GetDiscardPile()).Single(card => card.GetCardDataID() == GravityId);
                if (!cards.DrawSpecificCard(gravity)) throw new InvalidOperationException("The donor's native Gravity could not be drawn.");
                drawnForKill = true;
                return;
            }
            var enemies = new List<CharacterState>();
            rooms.GetRoom(CarrierRoom).AddCharactersToList(enemies, Team.Type.Heroes);
            killTarget = enemies.First(enemy => enemy.IsAlive && enemy.GetSourceCharacterData().GetAssetKey() == Hunter &&
                enemy.GetHP() == 4 && enemy.GetStatusEffectStacks("hivemind") == 2);
            SpawnPoint drop = killTarget.GetSpawnPoint();
            if (!cards.CanPlayHandCard(gravity, CarrierRoom, drop, null, null, out var error))
                throw new InvalidOperationException("The native Gravity input was unavailable: " + error);
            beforeKill = trace.CaptureDecision();
            killAction = new PlayCardAction(trace.CardId(gravity), CarrierRoom, targetUnitId: trace.UnitId(killTarget));
            firstKillObservation = observations.Count;
            if (!cards.PlayCard(cards.GetHand().IndexOf(gravity), drop, ref error))
                throw new InvalidOperationException("The native Gravity input was rejected: " + error);
        }

        private static bool IsCarrier(CharacterState actor) => actor.GetSpawnerCard()?.GetCardDataID() == CarrierCardId &&
            actor.GetSpawnerCard()?.GetCardStateModifiers().HasUpgrade(Definition.Upgrade) == true;

        private static GrowthState ReadGrowth(CharacterState carrier) => new(
            carrier.GetAppliedCardUpgrades().SingleOrDefault(upgrade => upgrade.GetCardUpgradeDataId() == NativeGrowth)?.GetAttackDamage(),
            carrier.GetAttackDamage(), RngCalibration.Words(RandomManager.GetState(RngId.Battle)),
            RngCalibration.Words(RandomManager.GetState(RngId.BattleTest)), RngCalibration.Words(RandomManager.GetState(RngId.CardDraw)));

        private sealed class GrowthState(int? modifierAttack, int totalAttack, uint[] battle, uint[] battleTest, uint[] cardDraw)
        {
            public int? ModifierAttack { get; } = modifierAttack;
            public int TotalAttack { get; } = totalAttack;
            public uint[] Battle { get; } = battle;
            public uint[] BattleTest { get; } = battleTest;
            public uint[] CardDraw { get; } = cardDraw;
        }

        private sealed class Observation(int turn, int actorId, int towerStacks, GrowthState before, GrowthState after)
        {
            public int Turn { get; } = turn;
            public int ActorId { get; } = actorId;
            public int TowerStacks { get; } = towerStacks;
            public GrowthState Before { get; } = before;
            public GrowthState After { get; } = after;
        }

        private sealed class DeathObservation(int carrierId, int dyingId, int triggerCount, GrowthState before, GrowthState after)
        {
            public int CarrierId { get; } = carrierId;
            public int DyingId { get; } = dyingId;
            public int TriggerCount { get; } = triggerCount;
            public GrowthState Before { get; } = before;
            public GrowthState After { get; } = after;
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class DeathPatch
        {
            private static bool Prepare() => Tier > 1;

            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState dyingCharacter, bool canFireTriggers, bool fromRunningTriggerQueue, int triggerCount, ref IEnumerator __result)
            {
                if (killAction != null && ReferenceEquals(dyingCharacter, killTarget) && canFireTriggers && fromRunningTriggerQueue &&
                    trigger == CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor && IsCarrier(__instance) &&
                    !AllGameManagers.Instance.GetSaveManager().PreviewMode && FullBattleTrace.Active is FullBattleTrace trace)
                    __result = Observe(__result, __instance, trace.UnitId(__instance), killAction.TargetUnitId, triggerCount);
            }

            private static IEnumerator Observe(IEnumerator native, CharacterState carrier, int carrierId, int dyingId, int count)
            {
                GrowthState before = ReadGrowth(carrier);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                deaths.Add(new DeathObservation(carrierId, dyingId, count, before, ReadGrowth(carrier)));
            }
        }

        [HarmonyPatch(typeof(RoomStateHivemindKillerModifier), "SyncUnitUpgradeStats")]
        private static class GrowthPatch
        {
            private static void Postfix(CharacterState character, int hivemindStacks, ref IEnumerator __result)
            {
                if (Enabled && Acquired && FullBattleTrace.Active is FullBattleTrace trace && IsCarrier(character) &&
                    !AllGameManagers.Instance.GetSaveManager().PreviewMode)
                    __result = Observe(__result, trace, character, hivemindStacks);
            }

            private static IEnumerator Observe(IEnumerator native, FullBattleTrace trace, CharacterState actor, int stacks)
            {
                int turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount();
                int id = trace.UnitId(actor);
                GrowthState before = ReadGrowth(actor);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                observations.Add(new Observation(turn, id, stacks, before, ReadGrowth(actor)));
            }
        }
    }
}
#endif
