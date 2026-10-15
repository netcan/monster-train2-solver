using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignBattleResourceCalibration
    {
        private static readonly string[] Ids = { "28772b0a-6f5c-44a7-98dd-260a851dd391", "26c9bc93-f5a6-439d-8239-14c22cbeb02e" };
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_BATTLE_RESOURCES") != "1") return;
            var save = managers.GetSaveManager(); var relics = managers.GetRelicManager(); var data = save.GetAllGameData();
            var owners = Ids.Select(id => data.GetAllCollectableRelicData().Single(owner => owner.GetID() == id)).ToArray();
            if (save.PreviewMode || save.GetAllRelics().Any(owner => Ids.Contains(owner.GetRelicDataID()) ||
                owner.GetEffects().Any(effect => effect is ITurnPhaseEndOfBattleRelicEffect)))
                throw new InvalidOperationException("End-battle calibration requires isolated original owners.");
            var characters = new List<CharacterState>(); managers.GetRoomManager().GetRoom(3).AddCharactersToList(characters, Team.Type.Monsters);
            CharacterState pyre = characters.Single(unit => unit.IsPyreHeart());
            int originalGold = save.GetGold(), originalCap = save.GetDragonsHoardCap(), originalHoard = save.GetDragonsHoardAmount();
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            JObject Snapshot() => JObject.FromObject(new { Relics = save.GetCollectedRelics().Select(owner => owner.GetRelicDataID()).ToArray(),
                Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(), Gold = save.GetGold(), Cap = save.GetDragonsHoardCap(),
                Hoard = save.GetDragonsHoardAmount(), Health = save.GetTowerHP(), Maximum = save.GetMaxTowerHP(), Frame = UnityEngine.Time.frameCount, Rng = Rng() });
            JObject before = Snapshot(); var rows = new List<object>();
            var parameters = new RelicEffectParams { saveManager = save, relicManager = relics, screenManager = managers.GetScreenManager() };
            void Remove(CollectableRelicData owner) => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == owner.GetID()));
            void Reset()
            {
                foreach (var owner in save.GetCollectedRelics().Where(item => Ids.Contains(item.GetRelicDataID())))
                foreach (var effect in owner.GetEffects().OfType<ITurnPhaseStartOfBattleRelicEffect>())
                    if (effect.TestEffectStartOfBattle(parameters, managers.GetCoreManagers())) Drain(effect.ApplyEffectStartOfBattle(parameters, managers.GetCoreManagers()));
            }
            void Samples(string phase)
            {
                foreach (string scenario in new[] { "idle", "zero", "negative", "wrong-trigger", "null-character", "damage", "reset-after-damage" })
                foreach (int cap in new[] { 0, 2, 5 })
                foreach (int amountCase in new[] { 0, 1, 2 })
                {
                    Reset();
                    if (scenario != "idle") Drain(relics.ApplyCharacterTriggerRelicEffect(scenario == "wrong-trigger" ? CharacterTriggerData.Trigger.OnDeath : CharacterTriggerData.Trigger.OnHit,
                        scenario == "null-character" ? null! : pyre, scenario == "zero" ? 0 : scenario == "negative" ? -1 : 1, 0, null));
                    if (scenario == "reset-after-damage") Reset();
                    foreach (int repetition in new[] { 0, 1 })
                    {
                        int amount = amountCase == 0 ? 0 : amountCase == 1 ? Math.Max(0, cap - 1) : cap;
                        save.SetGold(123, isTotalReset: true); save.SetDragonsHoardCap(cap); save.SetDragonsHoardAmount(amount);
                        var damage = save.GetCollectedRelics().Where(item => Ids.Contains(item.GetRelicDataID())).Select(owner => new {
                            Id = owner.GetRelicDataID(), Damaged = (bool)AccessTools.Field(owner.GetEffects().Single().GetType(), "pyreWasDamaged").GetValue(owner.GetEffects().Single()) }).ToArray();
                        object rngBefore = Rng(); relics.ApplyEndOfBattleRelicEffects(null);
                        rows.Add(new { Phase = phase, Scenario = scenario, AmountCase = amountCase, Repetition = repetition,
                            Owned = damage, Cap = cap, BeforeGold = 123, BeforeHoard = amount, Gold = save.GetGold(), Hoard = save.GetDragonsHoardAmount(),
                            Multiplier = ((MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save)).GetRule(MutableRules.Condition.GoldRewardMultiplier, 1f),
                            RngBefore = rngBefore, RngAfter = Rng() });
                    }
                }
            }
            try
            {
                Samples("baseline"); save.AddRelic(owners[0]); Samples("gold"); save.AddRelic(owners[1]); Samples("both");
                Remove(owners[0]); Samples("hoard"); save.AddRelic(owners[0]); Samples("reversed"); Remove(owners[1]); Remove(owners[0]); Samples("removed");
            }
            finally
            {
                foreach (var owner in owners) if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == owner.GetID())) Remove(owner);
                save.SetGold(originalGold, isTotalReset: true); save.SetDragonsHoardCap(originalCap); save.SetDragonsHoardAmount(originalHoard);
            }
            JObject after = Snapshot();
            if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("End-battle recorded state/frame/RNG changed.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after,
                Owners = owners.Select(owner => new { Id = owner.GetID(), Key = owner.name, DisallowedInPlacementPhase = owner.DisallowInDeploymentPhase,
                    Effects = owner.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Bool = effect.GetParamBool(),
                        Conditions = effect.GetEffectConditions().Count }).ToArray() }).ToArray(), Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-battle-resource-calibration.mt2f"))) document.Write(stream);
        }
        private static void Drain(IEnumerator routine)
        {
            try { while (routine.MoveNext()) { if (routine.Current is IEnumerator nested) Drain(nested); else if (routine.Current != null) throw new InvalidOperationException("Calibration requires asynchronous native consumer."); } }
            finally { (routine as IDisposable)?.Dispose(); }
        }
    }
}
