#if DEVELOPMENT_PROBE
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
    internal static class SummonChoiceCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SUMMON_CHOICE_CALIBRATION") == "1";
        private static readonly string[] Abilities = {
            "2160ff0b-3d1f-4d77-97e0-26b5df37590e", "c0d006a1-ed15-45ff-bb51-7664a8c4f11c", "24daf0a7-40fd-4f15-b6d1-60522568cad4" };
        private static readonly List<object> Frames = new List<object>();
        private static readonly List<object> Definitions = new List<object>();
        private static readonly List<CardEffectState> Effects = new List<CardEffectState>();
        private static readonly List<SummonChoiceRule> Rules = new List<SummonChoiceRule>();
        private static readonly List<UnityEngine.Object> Owned = new List<UnityEngine.Object>();
        private static MethodInfo Collect = null!, HasSubtype = null!;
        private static bool? installedOverride;
        private static int calibrationThread;
        private static bool InstalledPrefix(ref bool __result)
        {
            if (!installedOverride.HasValue || Environment.CurrentManagedThreadId != calibrationThread) return true;
            __result = installedOverride.Value; return false;
        }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static UnityRng Rng(uint[] words) => new UnityRng(words[0], words[1], words[2], words[3]);
        private static JObject AllRng() => JObject.FromObject(Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
            .ToDictionary(id => id.ToString(), id => RngCalibration.Words(RandomManager.GetState(id))));

        private static void Prepare(AllGameManagers managers)
        {
            if (Effects.Count > 0) return;
            Type type = AccessTools.TypeByName("CardEffectChooseMonsterToSpawnInBack") ??
                throw new InvalidOperationException("Native summon-choice effect is unavailable.");
            Collect = AccessTools.Method(type, "CollectChoices"); HasSubtype = AccessTools.Method(type, "RoomAlreadyHasSubtype");
            calibrationThread = Environment.CurrentManagedThreadId;
            var installed = AccessTools.Method(ShinyShoe.AppManager.PlatformServices.GetType(), "IsDlcInstalled", new[] { typeof(ShinyShoe.DLC) });
            new Harmony("MonsterTrain2Poju.SummonChoiceCalibration.PlatformInput").Patch(installed,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(SummonChoiceCalibration), nameof(InstalledPrefix))));
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            void Add(string label, CardEffectData data, string? abilityId)
            {
                var effect = new CardEffectState(); effect.Setup(data, Team.Type.Monsters, save);
                Effects.Add(effect);
                for (int profile = 0; profile < 2; profile++)
                {
                    installedOverride = profile == 0 ? (bool?)null : true;
                    var pool = new List<CardData>(); effect.GetFilteredCardListFromPool(managers.GetRelicManager(), ref pool);
                    var rule = new SummonChoiceRule(pool.Select(card => new SummonChoiceCandidate(card.GetID(),
                        card.GetSpawnCharacterData() != null, card.GetSpawnCharacterData()?.GetSize() ?? 0)).ToArray(),
                        effect.GetParamInt(), effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key);
                    Definitions.Add(new { Label = label, Profile = profile == 0 ? "native-platform" : "installed-control",
                        AbilityDataId = abilityId, Native = RuleCatalogProbe.Effect(data), Rule = rule });
                    Rules.Add(rule);
                }
                installedOverride = null;
            }
            foreach (string id in Abilities)
            {
                var card = all.FindCardData(id) ?? throw new InvalidOperationException("Missing original GrimFlight ability.");
                Add(card.name, card.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectChooseMonsterToSpawnInBack"), id);
            }
            CardEffectData Control(string name, params CardData[] cards)
            {
                var pool = UnityEngine.ScriptableObject.CreateInstance<CardPool>(); pool.name = name; Owned.Add(pool);
                object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
                foreach (var card in cards) AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { card });
                var effect = new CardEffectData("CardEffectChooseMonsterToSpawnInBack", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramCardPool", pool); Set(effect, "paramInt", 3);
                return effect;
            }
            var token = all.FindCardData("6c3eb0bb-4871-438d-9b5c-fcef1f94bf70")!;
            var spell = all.FindCardData("22d7850e-4168-4b92-b114-395d7b870b9b")!;
            Add("spell-filter-control", Control("PojuSummonChoiceMixedPool", token, spell), null);
            Add("duplicate-reference-control", Control("PojuSummonChoiceDuplicatePool", token,
                all.FindCardData("c847d9b0-5af4-4076-9d99-111b81224ef5")!, token), null);
            Add("empty-pool-control", Control("PojuSummonChoiceEmptyPool"), null);
        }

        internal static void Capture(FullBattleTrace trace, AllGameManagers managers, BattleTurnState before, string label)
        {
            if (!Enabled) return;
            if (managers.GetSaveManager().PreviewMode || before.Spawn.Train.Context?.SpawnPoints == null)
                throw new InvalidOperationException("Summon choice calibration requires settled primary physical positions.");
            JToken decision = JToken.FromObject(before); JObject rngBefore = AllRng();
            int frame = UnityEngine.Time.frameCount;
            var battleRng = (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { RngId.Battle });
            var originalRng = new HadesRNG(battleRng);
            var records = new List<object>(); var slots = new List<object>();
            try
            {
                Prepare(managers);
                foreach (var room in before.Spawn.Train.Rooms)
                {
                    var nativeRoom = managers.GetRoomManager().GetRoom(room.RoomIndex);
                    foreach (int size in new[] { 1, 2, 1000 })
                    {
                        bool allowed = managers.GetMonsterManager().GetCanSpawnMonster(room.RoomIndex, null!, size);
                        bool predicted = room.Context!.SpawnPoints!.FirstEmpty(room.RoomIndex, CombatTeam.Player) >= 0;
                        if (allowed != predicted) throw new InvalidOperationException("Null-position native summon slot differs.");
                        slots.Add(new { RoomIndex = room.RoomIndex, Size = size, Allowed = allowed });
                    }
                    for (int ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
                    foreach (int count in new[] { -1, 0, 1, 2, 3, 10 })
                    foreach (int seed in new[] { 0, 41, 20261115 })
                    {
                        installedOverride = ruleIndex % 2 == 0 ? (bool?)null : true;
                        var effect = Effects[ruleIndex / 2]; effect.SetParamInt(count); battleRng.Init(seed);
                        uint[] initial = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
                        bool blocked = (bool)HasSubtype.Invoke(null, new object[] { effect, nativeRoom });
                        var choices = new List<CardData>();
                        if (!blocked) Collect.Invoke(null, new object[] { effect, new CardEffectParams { selectedRoom = room.RoomIndex },
                            managers.GetCoreManagers(), choices });
                        var offers = choices.Select(card => card.GetID()).ToArray();
                        uint[] actualRng = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
                        SummonChoiceDraft predicted = SummonChoiceModel.Plan(room, Rules[ruleIndex].WithCount(count), Rng(initial));
                        if (!predicted.Supported || predicted.BlockedBySubtype != blocked || !predicted.Offers.SequenceEqual(offers) ||
                            !predicted.Rng.Equals(Rng(actualRng)))
                            throw new InvalidOperationException("Native summon choices, subtype limit or complete RNG differ.");
                        records.Add(new { RoomIndex = room.RoomIndex, RuleIndex = ruleIndex, Count = count, Seed = seed,
                            BeforeRng = initial, AfterRng = actualRng, Blocked = blocked, Offers = offers });
                    }
                }
            }
            finally { installedOverride = null; battleRng.Init(originalRng); }
            JObject rngAfter = AllRng();
            bool unchanged = JToken.DeepEquals(decision, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged || frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Summon choice calibration changed native decision, RNG or frame.");
            Frames.Add(new { Label = label, Before = before, DecisionUnchanged = unchanged, FrameBefore = frame,
                FrameAfter = UnityEngine.Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter, Slots = slots, Samples = records });
            using (var document = NativeFixtureCapture.Capture(new {
                Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Scope = "SummonChoiceOffersOnly", Definitions, Frames }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "summon-choice-calibration.mt2f")))
                document.Write(output);
        }
    }
}

#endif
