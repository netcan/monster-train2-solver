using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossContinuationCalibration
    {
        internal static void Capture(IReadOnlyList<CharacterState> actors, Func<object> snapshot)
        {
            if (!TrueFinalBossTurnProbe.Enabled) return;
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            CharacterState dominion = actors[0];
            RoomState room = managers.GetRoomManager().GetRoom(3);
            var characters = new List<CharacterState>(); room.AddCharactersToList(characters, Team.Type.Monsters);
            CharacterState pyre = characters.Single(character => character.IsPyreHeart());
            if (dominion.GetHP() != 1555 || pyre.GetHP() != 80 || dominion.GetCurrentRoomIndex() != 3 ||
                (bool)AccessTools.Field(typeof(CombatManager), "isKillCamActivated").GetValue(combat) ||
                managers.GetHeroManager().HasNextBoss() || !managers.GetRoomManager().GetRoom(0).IsRoomEnabled())
                throw new InvalidOperationException("Continuation calibration requires the original born Dominion/Pyre room without pending boss or kill cam.");
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object oldDominion = primary.GetValue(dominion), oldPyre = primary.GetValue(pyre);
            Type stateType = oldDominion.GetType();
            var hp = AccessTools.Property(stateType, "Hp");
            var storedHp = AccessTools.Field(stateType, "hp");
            var statuses = AccessTools.Field(stateType, "statusEffects");
            MethodInfo clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var sample = ((Dictionary<string, CharacterState.StatusEffectStack>)statuses.GetValue(oldDominion))["lifelink"];
            object Shadow(object original, int health, bool lifeLink, bool untouchable)
            {
                object shadow = clone.Invoke(original, null);
                var map = new Dictionary<string, CharacterState.StatusEffectStack>((Dictionary<string, CharacterState.StatusEffectStack>)statuses.GetValue(original));
                map.Remove("lifelink"); map.Remove("untouchable");
                if (lifeLink) map["lifelink"] = sample;
                if (untouchable) map["untouchable"] = sample;
                // MemberwiseClone shares the mutable ObfuscatedNumber wrapper.
                // Give the controlled state its own storage before setting HP.
                storedHp.SetValue(shadow, Activator.CreateInstance(storedHp.FieldType, new object[] { (double)health }));
                statuses.SetValue(shadow, map); hp.SetValue(shadow, health); return shadow;
            }
            object Rng() => new { Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray(),
                Unity = RngCalibration.Words(UnityEngine.Random.state) };
            MethodInfo query = AccessTools.Method(typeof(CombatManager), "CheckIfUnitCombatShouldContinue");
            CombatUnit Input(CharacterState actor, CombatTeam team, bool isPyre)
            {
                var relevant = new List<CombatStatus>();
                foreach (string status in new[] { "lifelink", "untouchable" })
                    if (actor.HasStatusEffect(status)) relevant.Add(new CombatStatus(status, 1));
                return new CombatUnit(FullBattleTrace.Active!.UnitId(actor), actor.GetSourceCharacterData().GetAssetKey(),
                    team, 0, actor.GetHP(), actor.GetMaxHP(), true, isPyre, !isPyre, relevant, Array.Empty<CombatTrigger>());
            }
            object before = snapshot(), rng = Rng(); int frame = UnityEngine.Time.frameCount;
            var rows = new List<object>();
            try
            {
                foreach (int enemyHealth in new[] { 0, 1, 1555 })
                foreach (int pyreHealth in new[] { 0, 1, 80 })
                foreach (bool enemyLifeLink in new[] { false, true })
                foreach (bool pyreLifeLink in new[] { false, true })
                foreach (bool enemyUntouchable in new[] { false, true })
                foreach (bool pyreUntouchable in new[] { false, true })
                foreach (bool relentless in new[] { false, true })
                {
                    primary.SetValue(dominion, Shadow(oldDominion, enemyHealth, enemyLifeLink, enemyUntouchable));
                    primary.SetValue(pyre, Shadow(oldPyre, pyreHealth, pyreLifeLink, pyreUntouchable));
                    object[] arguments = { room, relentless, false };
                    bool actual = (bool)query.Invoke(combat, arguments);
                    rows.Add(new { RelentlessRoom = relentless,
                        // Project only this predicate's native inputs. Detached
                        // status controls deliberately do not carry a complete
                        // gameplay status dictionary or callback registry.
                        Before = new RoomCombatState(3, false, new[] { Input(dominion, CombatTeam.Enemy, false),
                            Input(pyre, CombatTeam.Player, true) }, Array.Empty<string>()),
                        Actual = actual, ShouldSpawnNextBoss = (bool)arguments[2] });
                    primary.SetValue(dominion, oldDominion); primary.SetValue(pyre, oldPyre);
                }
            }
            finally { primary.SetValue(dominion, oldDominion); primary.SetValue(pyre, oldPyre); }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(snapshot())) &&
                JToken.DeepEquals(JToken.FromObject(rng), JToken.FromObject(Rng()));
            if (!unchanged) throw new InvalidOperationException("Continuation queries changed native actors, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "OriginalTrueFinalBossCombatContinuation", Route = TrueFinalBossBirthProbe.Control,
                DetachedHealthAndStatusControls = true, OriginalUnitsAndRooms = true,
                LiveWorldAndFrameAndRngUnchanged = unchanged, NativeCompleted = true, Rows = rows
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "true-final-boss-continuation-calibration.mt2f"))) document.Write(output);
        }
    }
}
