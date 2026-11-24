#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSavageryCombatWitness
    {
        private const string ChampionId = "19705414-816d-4549-bcf5-866f338a551f";
        private static readonly string[] Pool = { "ea3192c8-1973-4f0c-b25a-9793b2a64c82", "e1d5efa3-b3d0-41af-aed7-d3218f364207", "c87050f8-46f8-453b-99c1-de97d6070922" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_SAVAGERY_COMBAT") == "1";
        internal static bool Prepared { get; private set; }
        private static CardState? champion;
        private static BattleTurnState? beforeDeployment;
        private static PlayCardAction? deployment;

        internal static void Prepare(AllGameManagers managers)
        {
            CampaignRelicCalibration.CaptureSavagery(managers);
            Prepared = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (!Prepared || save.PreviewMode || !combat.IsPlacementPhase)
                throw new InvalidOperationException("Expected the live original Savagery placement decision.");
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            RoomState room = rooms.GetRoom(0);
            if (champion == null)
            {
                champion = cards.GetHand().Single(card => card.GetCardDataID() == ChampionId &&
                    card.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).SequenceEqual(Pool));
                var point = room.GetFirstEmptyMonsterPoint() ?? throw new InvalidOperationException("Missing the original champion placement point.");
                if (!cards.CanPlayHandCard(champion, 0, point, null, null, out var error))
                    throw new InvalidOperationException("Original Savagery champion is unavailable: " + error);
                beforeDeployment = trace.CaptureDecision();
                deployment = new PlayCardAction(trace.CardId(champion), 0, playerPosition: point.GetIndexInRoom());
                if (!cards.PlayCard(cards.GetHand().IndexOf(champion), point, ref error))
                    throw new InvalidOperationException("Original Savagery champion deployment failed: " + error);
                return false;
            }

            var units = new List<CharacterState>();
            room.AddCharactersToList(units, Team.Type.Monsters);
            CharacterState actor = units.Single(unit => unit.IsAlive && ReferenceEquals(unit.GetSpawnerCard(), champion));
            var modifier = actor.GetRoomStateModifiers().OfType<RoomStateSelfDamagePerMissingHp>().Single();
            int unitId = trace.UnitId(actor);
            var healthy = Sample();
            var afterDeployment = trace.CaptureDecision();
            int paid = champion.GetLastPlayedCost();
            const int wound = 3;
            if (cards.GetHand().Contains(champion) || beforeDeployment!.Energy - afterDeployment.Energy != paid ||
                healthy.Hp != healthy.MaxHp || healthy.Hp <= wound || healthy.StatsBonus != 0)
                throw new InvalidOperationException("Expected a settled paid champion with the original healthy attack query.");

            // Read the attack contribution at a controlled health state without triggering damage reactions.
            actor.SetHealth(healthy.Hp - wound, healthy.MaxHp);
            var wounded = Sample();
            var afterWound = trace.CaptureDecision();
            if (wounded.Hp != healthy.Hp - wound || wounded.MaxHp != healthy.MaxHp || wounded.StatsBonus != 2 * wound ||
                wounded.Attack - healthy.Attack != wounded.StatsBonus || healthy.CombatBonus != 0 || wounded.CombatBonus != 0 ||
                !healthy.Rng.Zip(wounded.Rng, (before, after) => before.SequenceEqual(after)).All(equal => equal))
                throw new InvalidOperationException("The original wounded Savagery attack query differed.");

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSavageryChampionCombat", GameVersion = Application.version,
                GameModuleMvid = typeof(CharacterState).Module.ModuleVersionId, ChampionId, Pool, unitId, PaidCost = paid,
                DeploymentBefore = beforeDeployment, Deployment = deployment, DeploymentAfter = afterDeployment,
                ControlledMissingHealth = wound, Healthy = healthy, Wounded = wounded, AfterWound = afterWound
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "original-savagery-champion-combat.mt2f"));
            document.Write(stream);
            return true;

            AttackSample Sample() => new AttackSample(actor.GetHP(), actor.GetMaxHP(), actor.GetAttackDamage(),
                    modifier.GetModifiedAttackDamage(Damage.Type.Default, actor, true, managers.GetCoreManagers()),
                    modifier.GetModifiedAttackDamage(Damage.Type.Default, actor, false, managers.GetCoreManagers()),
                    new[] { RngId.Battle, RngId.BattleTest }.Select(id => RngCalibration.Words(RandomManager.GetState(id))).ToArray());
        }

        private sealed class AttackSample(int hp, int maxHp, int attack, int statsBonus, int combatBonus, uint[][] rng)
        {
            public int Hp { get; } = hp;
            public int MaxHp { get; } = maxHp;
            public int Attack { get; } = attack;
            public int StatsBonus { get; } = statsBonus;
            public int CombatBonus { get; } = combatBonus;
            public uint[][] Rng { get; } = rng;
        }
    }
}
#endif
