using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardStatusCompositionCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CARD_STATUS_COMPOSITION") != "1") return;
            JObject Rng()
            {
                var result = new JObject();
                foreach (RngId id in Enum.GetValues(typeof(RngId)))
                    if (id != RngId.Chatter && id != RngId.NonDeterministic) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
                return result;
            }
            var before = JToken.FromObject(trace.CaptureContext()); var rngBefore = Rng(); int frame = UnityEngine.Time.frameCount;
            UpgradeMaskStatus Project(StatusEffectStackData status) => new UpgradeMaskStatus(status.statusId, status.count, status.fromPermanentUpgrade);
            StatusEffectStackData Value(string id, int count, bool permanent = false) => new StatusEffectStackData { statusId = id, count = count, fromPermanentUpgrade = permanent };
            CardStatusCompositionState Input(List<StatusEffectStackData> initial, List<CardUpgradeState> permanent,
                List<CardUpgradeState> temporary, bool purified, bool supported) => new CardStatusCompositionState(initial.Select(Project).ToArray(),
                    permanent.Select(upgrade => (IReadOnlyList<UpgradeMaskStatus>)upgrade.GetStatusEffectUpgrades().Select(Project).ToArray()).ToArray(),
                    temporary.Select(upgrade => (IReadOnlyList<UpgradeMaskStatus>)upgrade.GetStatusEffectUpgrades().Select(Project).ToArray()).ToArray(), purified, supported);
            var rows = new List<object>();
            foreach (var card in AllGameManagers.Instance!.GetCardManager()!.GetAllCards(new List<CardState>()))
            {
                var spawn = card.GetSpawnCharacterData(); var initial = new List<StatusEffectStackData>(); spawn?.GetStartingStatusEffectsCopy(initial);
                var actual = new List<StatusEffectStackData>(); bool returned = card.TryGetStatusEffects(actual);
                rows.Add(new { Name = card.GetCardDataID(), Kind = "OwnedQuery", Input = Input(initial, card.GetCardStateModifiers().GetCardUpgrades(),
                    card.GetTemporaryCardStateModifiers().GetCardUpgrades(), card.IsPurified, card.IsEquipmentCard() || (card.IsSpawnerCard() && spawn != null)),
                    Returned = returned, Actual = actual.Select(Project).ToArray() });
            }
            var armor = Value("armor", 2); var permanentArmor = Value("armor", 2, true); var poison = Value("poison", 3);
            StatusEffectStackData[][] initialSets = { Array.Empty<StatusEffectStackData>(), new[] { armor }, new[] { permanentArmor },
                new[] { armor, permanentArmor }, new[] { Value("armor", -2), armor }, new[] { Value("armor", 0), poison },
                new[] { Value("armor", int.MaxValue), Value("armor", 1) }, new[] { Value("armor", 1), Value("poison", 1) } };
            StatusEffectStackData[][][] upgradeSets = { Array.Empty<StatusEffectStackData[]>(), new[] { new[] { Value("armor", -3) } },
                new[] { new[] { Value("armor", -3), armor } }, new[] { new[] { Value("armor", -3) }, new[] { armor } },
                new[] { new[] { poison, permanentArmor } }, new[] { new[] { permanentArmor }, new[] { Value("armor", -3, true) } },
                new[] { new[] { Value("armor", int.MaxValue) }, new[] { Value("armor", 1) }, new[] { armor } },
                new[] { new[] { armor, Value("poison", -9) }, new[] { Value("armor", -3), poison } } };
            List<CardUpgradeState> Upgrades(StatusEffectStackData[][] values) => values.Select(statuses => {
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.GetStatusEffectUpgrades().AddRange(statuses); return upgrade;
            }).ToList();
            int index = 0;
            foreach (var initial in initialSets)
            foreach (var permanentValues in upgradeSets)
            foreach (var temporaryValues in upgradeSets)
            foreach (bool purified in new[] { false, true })
            {
                var permanent = Upgrades(permanentValues); var temporary = Upgrades(temporaryValues);
                var input = Input(initial.ToList(), permanent, temporary, purified, true);
                var first = StatusEffectHelper.MergeStatusEffectDatas(false, purified ? new List<StatusEffectStackData>() : initial.ToList(), permanent);
                var actual = StatusEffectHelper.MergeStatusEffectDatas(false, first, temporary);
                rows.Add(new { Name = "controlled-" + index++, Kind = "Controlled", Input = input, Returned = true, Actual = actual.Select(Project).ToArray() });
            }
            var rngAfter = Rng(); bool unchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureContext()));
            if (!unchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Card status composition calibration changed native context/RNG/frame.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, ContextUnchanged = unchanged,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-status-composition-calibration.mt2f")))
                document.Write(stream);
        }
    }
}
