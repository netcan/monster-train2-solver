using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRelicCountAttackScenario
    {
        internal const string Name = "original-relic-count-attack";
        internal const string Id = "79122f7a-6895-443a-830c-2db23027776c";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static readonly System.Collections.Generic.List<object> Controls = new System.Collections.Generic.List<object>();
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log, bool authorUnits = true)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle) throw new InvalidOperationException("Relic-count owner requires pre-battle acquisition.");
            foreach (var card in save.GetDeckState().Where(_ => authorUnits))
            {
                var definition = data.FindCardData(card.GetCardDataID()); var unit = definition?.GetSpawnCharacterData();
                if (unit == null) continue;
                Set(unit, "health", 80); Set(unit, "attackDamage", 20); Set(unit, "size", 1); Set(definition!, "cost", 0);
            }
            var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == Id);
            if (owner.GetEffects().Count != 2 || owner.GetEffects().Any(effect => effect.GetEffectClassName() != "RelicEffectModifyCardAttack" || effect.GetEffectConditions().Count != 0)) throw new InvalidOperationException("Original count owner changed.");
            save.AddRelic(owner); Acquired = true; log.LogInfo("ORIGINAL-RELIC-COUNT-ACQUIRED unchanged MoreOrLessArtifactsRelic.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var relics = managers.GetRelicManager();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0) throw new InvalidOperationException("Relic-count controls require deployment.");
            var raw = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            var pyre = managers.GetRoomManager().GetPyreRoom().GetPyreHeart();
            int pyreHealth = pyre.GetHP(), pyreMaximum = pyre.GetMaxHP(), saveHealth = save.GetTowerHP(), saveMaximum = save.GetMaxTowerHP();
            object originalConditions = AccessTools.Field(typeof(BaseSaveData), "startingConditions").GetValue(raw);
            var noop = data.GetAllCollectableRelicData().Single(owner => owner.GetID() == OriginalNullRelicScenario.Id);
            var wurm = data.GetAllClassDatas().Single(clan => clan.IsStarterRelic(noop.GetID()));
            var foreign = data.GetAllClassDatas().First(clan => !clan.IsStarterRelic(noop.GetID()));
            var boss = data.GetAllCollectableRelicData().Single(owner => owner.GetID() == "f9cc17ee-b7e5-45de-a02f-bd271e2dcd1a");
            if (!boss.IsBossGivenRelic()) throw new InvalidOperationException("Original boss exclusion witness changed.");
            var witnessData = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())).Where(card => card != null).ToArray();
            var witnesses = new[] { new CardState(witnessData.First(card => card!.GetCardType() == CardType.Monster)!, save),
                new CardState(witnessData.First(card => card!.GetCardType() == CardType.Spell)!, save),
                new CardState(data.GetAllCardData().First(card => card.GetSpawnCharacterData()?.IsChampion() == true), save, setupStartingUpgrades: false) };
            JObject Stable() => JObject.FromObject(new { Main = raw.GetMainClassID(), Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(), Gold = save.GetGold(), Health = save.GetTowerHP(),
                Maximum = save.GetMaxTowerHP(), Pyre = new { Health = pyre.GetHP(), Maximum = pyre.GetMaxHP() },
                Draw = save.GetStartOfTurnCards(), Owners = save.GetCollectedRelics().Select(owner => owner.GetRelicDataID()).ToArray(), Rng = Enum.GetValues(typeof(RngId)).Cast<RngId>()
                    .Where(id => id != RngId.NonDeterministic && id != RngId.Chatter).Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray() });
            var before = Stable();
            void Remove(CollectableRelicData owner) => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == owner.GetID()));
            void Samples(string phase)
            {
                foreach (var card in witnesses)
                foreach (bool reset in new[] { true, false, true })
                {
                    relics.ApplyCardStateModifiers(card, resetTempCardModifiers: reset);
                    Controls.Add(new { Phase = phase, Main = raw.GetMainClassID(), Card = card.GetCardDataID(), CardType = card.GetCardType().ToString(), Reset = reset,
                        AdditionalAttack = CardModifierProbe.Modifiers(card.GetTemporaryCardStateModifiers()).Offsets.Damage,
                        Roles = RelicProbe.Capture(managers).Single(owner => owner.DataId == Id).CardAttackModifiers![0].CountRoles });
                }
            }
            CaptureReady = true;
            try
            {
                Samples("base"); AccessTools.Field(typeof(BaseSaveData), "startingConditions").SetValue(raw, raw.GetStartingConditionsCopy());
                raw.SetMainClass(foreign.GetID(), 0, ""); save.AddRelic(noop); Samples("foreign-starter");
                raw.SetMainClass(wurm.GetID(), 0, ""); Samples("active-starter");
                save.AddRelic(boss); Samples("boss-excluded"); Remove(boss); Remove(noop);
                AccessTools.Field(typeof(BaseSaveData), "startingConditions").SetValue(raw, originalConditions); Samples("removed");
            }
            finally
            {
                foreach (var owner in new[] { boss, noop }) if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == owner.GetID())) Remove(owner);
                AccessTools.Field(typeof(BaseSaveData), "startingConditions").SetValue(raw, originalConditions);
                raw.SetMaxTowerHP(saveMaximum); raw.SetTowerHP(saveHealth); pyre.SetHealth(pyreHealth, pyreMaximum);
            }
            if (!JToken.DeepEquals(before, Stable())) throw new InvalidOperationException("Relic-count controls changed recorded class/deck/resource/RNG state.");
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-RELIC-COUNT-PREPARED actual foreign/active starter and boss exclusions, reset/repeat and Spell controls; original active class restored.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
