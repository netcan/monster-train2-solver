using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStealthSlayScenario
    {
        internal const string Name = "original-stealth-slay";
        internal const string Id = "4687feca-a053-48ed-b208-81dc6dfc6e72";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Stealth-on-slay requires isolated pre-battle acquisition.");

            var definition = data.GetAllCollectableRelicData().Single(owner => owner.GetID() == Id);
            var effects = definition.GetEffects();
            if (definition.name != "UnitsGainStealthOnSlayPlusAttack" || definition.DisallowInDeploymentPhase || effects.Count != 2 ||
                effects[0].GetEffectClassName() != "RelicEffectAdjustAttackByStatusEffect" ||
                effects[0].GetParamStatusEffects().Single().statusId != "stealth" || effects[0].GetParamInt() != 3 ||
                effects[0].GetParamSourceTeam() != Team.Type.Monsters || effects[0].GetEffectConditions().Count != 0 ||
                effects[1].GetEffectClassName() != "RelicEffectAddTrigger" || effects[1].GetParamSourceTeam() != Team.Type.Monsters ||
                !effects[1].GetParamCharacterSubtype().IsNone || !effects[1].GetParamBool() || effects[1].GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Unchanged stealth-on-slay source parameters differ.");

            bool preparedCarrier = false;
            foreach (var card in save.GetDeckState())
            {
                var cardDefinition = data.FindCardData(card.GetCardDataID());
                var unit = cardDefinition?.GetSpawnCharacterData();
                if (unit == null) continue;
                Set(unit, "health", 80);
                Set(unit, "attackDamage", 100);
                Set(unit, "size", 1);
                Set(cardDefinition!, "cost", 0);
                Set(unit, "startingStatusEffects", unit.GetStartingStatusEffects()
                    .Where(status => status.statusId != "stealth").ToArray());
                preparedCarrier = true;
            }
            if (!preparedCarrier) throw new InvalidOperationException("No deck monster can carry the stealth-on-slay witness.");
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id))
                throw new InvalidOperationException("Stealth-on-slay owner is already present.");

            save.AddRelic(definition);
            Acquired = true;
            log.LogInfo("ORIGINAL-STEALTH-SLAY-ACQUIRED unchanged owner; +1 Stealth on spawn and slay, +3 attack per friendly Stealth.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 || managers.GetSaveManager().PreviewMode)
                throw new InvalidOperationException("Stealth-on-slay missed its native deployment boundary.");

            var owner = RelicProbe.Capture(managers).Single(item => item.DataId == Id);
            var attack = owner.StatusAttackModifiers!.Single();
            var addition = owner.TriggerAdditions!.Single();
            if (owner.AssetKey != "UnitsGainStealthOnSlayPlusAttack" || owner.DisallowedInPlacementPhase != false ||
                !owner.EffectTypes.SequenceEqual(["RelicEffectAdjustAttackByStatusEffect", "RelicEffectAddTrigger"]) ||
                attack.EffectIndex != 0 || attack.StatusId != "stealth" || attack.Team != "Monsters" || attack.Multiplier != 3 ||
                addition.EffectIndex != 1 || !addition.SourcePlayer || addition.SourceEnemy || !addition.NoCountModifiersAllowed ||
                !addition.SubtypeIsNone || addition.CharacterAssetKeys.Count != 0 || addition.Conditions.Count != 0 ||
                !addition.Triggers.Select(trigger => trigger.Kind).SequenceEqual(["OnKill", "OnSpawn"]) ||
                addition.Triggers.Any(trigger => trigger.Effects.Count != 1 ||
                    trigger.Effects[0].Type != "CardEffectAddStatusEffect" ||
                    trigger.Effects[0].Action?.Statuses.SingleOrDefault()?.Id != "stealth" ||
                    trigger.Effects[0].Action?.Statuses.SingleOrDefault()?.Stacks != 1))
                throw new InvalidOperationException("Native stealth-on-slay query or trigger metadata differs.");

            Prepared = true;
            log.LogInfo("ORIGINAL-STEALTH-SLAY-PREPARED native status query and OnKill/OnSpawn triggers captured.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var plays = BattleActionModel.EnumerateSupportedPlays(state);
            var carrier = plays.FirstOrDefault(play =>
            {
                var rule = state.PlayRules!.Cards.FirstOrDefault(item =>
                    item.DataId == state.Spawn.Train.Context!.FindCard(play.CardInstanceId)?.DataId);
                return rule?.SpawnUnit != null && state.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex)
                    .Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
            });
            return carrier ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
