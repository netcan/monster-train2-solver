using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Isolated component boundary: real SetSpawnPoint calls and original
    // Conduit trackers. No acceptance of full combat or preview movement.
    internal static class ConduitMovementCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_CONDUIT_MOVEMENT") == "1";
        internal static bool Enemy => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_CONDUIT_ENEMY_MOVEMENT") == "1";
        private static CombatTeam Team => Enemy ? CombatTeam.Enemy : CombatTeam.Player;
        private static SpawnPoint NativePoint(int room, int point) => Enemy
            ? Managers.GetRoomManager().GetRoom(room).GetHeroPoint(point)
            : Managers.GetRoomManager().GetRoom(room).GetMonsterPoint(point);
        internal static bool Started, Completed;
        internal static string? Error;
        private static readonly List<object> rows = new List<object>();
        private static readonly List<object> previews = new List<object>();
        private static CharacterState actor = null!;
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        private static FullBattleTrace Trace => FullBattleTrace.Active!;
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter && id != RngId.BattleTest)
            .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        private static object Snapshot() => new { Train = Trace.CaptureTrain(),
            Contributor = ConduitProbe.CaptureUnit(actor, Trace), Rng = Rng() };
        private static SpawnPointReference? Point(SpawnPoint? point)
        {
            if (point == null) return null;
            RoomManager rooms = Managers.GetRoomManager();
            for (int room = 0; room < rooms.GetNumRooms(); room++)
            {
                var group = (SpawnPointGroup)AccessTools.Field(typeof(RoomState), Enemy ? "heroSpawnPointGroup" : "monsterSpawnPointGroup").GetValue(rooms.GetRoom(room));
                group.GetAll(out List<SpawnPoint> points, out _);
                int index = points.IndexOf(point);
                if (index >= 0) return new SpawnPointReference(room, Team, index);
            }
            throw new InvalidOperationException("Unknown Conduit movement point.");
        }
        private static void Move(string label, SpawnPoint? destination)
        {
            SpawnPointReference? beforePoint = Point(actor.GetSpawnPoint());
            object before = Snapshot();
            actor.SetSpawnPoint(destination, animate: false, setPosition: false);
            object actual = Snapshot();
            rows.Add(new { Label = label, UnitId = Trace.UnitId(actor), From = beforePoint, To = Point(destination),
                Dead = actor.IsDead, SelectedRoom = Managers.GetRoomManager().CurrentSelectedRoom, Before = before, Actual = actual });
        }
        private static void Select(int room) => AccessTools.Method(typeof(RoomUI), "InnerSetSelectedRoom")
            .Invoke(Managers.GetRoomManager().GetRoomUI(), new object[] { room, false });
        private static object[] HandUpgrades() => Managers.GetCardManager().GetHand().Select(card => (object)new
        {
            CardId = Trace.CardId(card),
            Upgrades = card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(upgrade => new
            { Damage = upgrade.GetAttackDamage(), Heal = upgrade.GetAdditionalHeal() }).ToArray()
        }).ToArray();
        private static void Preview(string label, int? room, int point)
        {
            object before = Snapshot(), upgrades = HandUpgrades();
            var temporary = AccessTools.Method(typeof(CombatManager), "SetTemporaryStateEnabled");
            temporary.Invoke(Managers.GetCombatManager(), new object[] { true, new List<CharacterState> { actor }, false });
            object entered = ConduitProbe.CaptureUnit(actor, Trace)!;
            SpawnPoint? destination = room.HasValue ? NativePoint(room.Value, point) : null;
            SpawnPointReference? target = BattleSpawnPointProbe.Reference(destination);
            actor.SetSpawnPoint(destination, false, false);
            object inside = new { Contributor = ConduitProbe.CaptureUnit(actor, Trace), Upgrades = HandUpgrades(), Rng = Rng() };
            temporary.Invoke(Managers.GetCombatManager(), new object[] { false, new List<CharacterState> { actor }, false });
            previews.Add(new { Label = label, UnitId = Trace.UnitId(actor), Target = target, Before = before,
                BeforeUpgrades = upgrades, Entered = entered, Inside = inside, Actual = Snapshot() });
        }
        internal static void Start(ManualLogSource log)
        { Started = true; Managers.GetSaveManager().StartCoroutine(Run(log)); }
        private static IEnumerator Run(ManualLogSource log)
        {
            var cards = Managers.GetCardManager();
            var data = Managers.GetAllGameData();
            var rooms = Managers.GetRoomManager();
            CardData carrier = cards.GetAllCards(new List<CardState>()).Select(card => data.FindCardData(card.GetCardDataID())!).First(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            if (Enemy)
            {
                HeroManager heroes = Managers.GetHeroManager()!;
                yield return heroes.SpawnHeroInRoom(carrier.GetSpawnCharacterData(), 1, null!);
                actor = heroes.GetCharactersReadOnly().Single();
            }
            else yield return Managers.GetMonsterManager().CreateMonsterState(carrier.GetSpawnCharacterData(), null!, 1,
                unit => actor = unit, SpawnMode.SelectedSlot, NativePoint(1, 0), isCardless: true);
            try
            {
                CardData spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectDamage" && card.GetTraits().Count == 0);
                CardState plain = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
                var trait = new CardTraitData(); trait.Setup("CardTraitStrongerMagicPower");
                // Authored carrier isolates the original multiplier trait. A
                // temporary trait has a separate, currently guarded lifecycle.
                CardData strongControl = UnityEngine.Object.Instantiate(spell);
                strongControl.name = "PojuConduitStrongerControl";
                AccessTools.Field(typeof(CardData), "id").SetValue(strongControl, "aa551021-0000-4000-8000-000000000001");
                AccessTools.Field(typeof(CardData), "traits").SetValue(strongControl, new List<CardTraitData> { trait });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(strongControl);
                CardState stronger = cards.AddNewCard(strongControl, CardPile.HandPile, false, false, animate: false);
                Trace.CardId(plain); Trace.CardId(stronger); Trace.UnitId(actor);
                actor.AddStatusEffect("conduit", 4);
                for (int selected = 0; selected < 3; selected++)
                for (int from = 0; from < 3; from++)
                for (int to = 0; to < 3; to++)
                {
                    actor.SetSpawnPoint(NativePoint(from, 0), false, false);
                    Select(selected);
                    Move($"selected-{selected}-from-{from}-to-{to}", NativePoint(to, 1));
                }
                for (int selected = 0; selected < 2; selected++)
                {
                    actor.SetSpawnPoint(NativePoint(0, 0), false, false);
                    Select(selected);
                    Move("detach-selected-" + selected, null);
                    Move("reattach-selected-" + selected, NativePoint(0, 1));
                }
                int hp = actor.GetHP(), maximum = actor.GetMaxHP();
                actor.SetSpawnPoint(NativePoint(0, 0), false, false);
                Select(0); actor.SetHealth(0, maximum);
                Move("dead-leaves-selected", NativePoint(1, 0));
                Move("dead-enters-selected", NativePoint(0, 1));
                Move("dead-detaches", null);
                actor.SetHealth(hp, maximum);
                actor.SetSpawnPoint(NativePoint(0, 0), false, false); Select(0);
                Move("same-point-tracked", actor.GetSpawnPoint());
                actor.GetRoomStateModifiers<RoomStateConduitModifier>().Single().ClearTracking(Managers.GetCoreManagers());
                Move("same-point-empty-selected", actor.GetSpawnPoint());
                actor.SetHealth(0, maximum);
                Move("dead-same-point-empty-selected", actor.GetSpawnPoint());
                actor.SetHealth(hp, maximum); actor.SetSpawnPoint(null, false, false);
                Move("detached-same-null", null);
                actor.SetSpawnPoint(NativePoint(0, 0), false, false); Select(0);
                Preview("preview-leaves-selected", 1, 0);
                Preview("preview-within-selected", 0, 1);
                Preview("preview-detaches", null, 0);
                bool rngUnchanged = rows.Concat(previews).All(row => JToken.DeepEquals(JObject.FromObject(row)["Before"]!["Rng"], JObject.FromObject(row)["Actual"]!["Rng"]));
                if (!rngUnchanged || rows.Count != 38 || previews.Count != 3 || !plain.CanScaleWithMagicPower())
                    throw new InvalidOperationException("Conduit movement controls or gameplay RNG changed.");
                using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = "2.2.1",
                    GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                    Boundary = "AfterNativeSetSpawnPointReturn", NativeCompleted = true, RngUnchanged = rngUnchanged,
                    EnemyCarrier = Enemy, PlainCardId = Trace.CardId(plain), StrongerCardId = Trace.CardId(stronger), Rows = rows, Previews = previews }))
                using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                    Enemy ? "conduit-enemy-movement-calibration.mt2f" : "conduit-movement-calibration.mt2f"))) document.Write(output);
                Completed = true;
                log.LogInfo("CONDUIT-MOVEMENT-NATIVE PASS rows=" + rows.Count + " previews=" + previews.Count + " gameplayRngUnchanged=true");
            }
            catch (Exception error) { Error = error.ToString(); log.LogError(Error); }
        }
    }
}
