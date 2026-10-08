using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitCopyCatalogProbe
    {
        internal static UnitCopyCatalog Capture(IReadOnlyCollection<CardPlayRule> reachable, EnemySpawnState spawn)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            AllGameData all = managers.GetSaveManager().GetAllGameData();
            var live = new List<CharacterState>();
            for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++)
                managers.GetRoomManager()!.GetRoom(index).AddCharactersToList(live, Team.Type.Heroes | Team.Type.Monsters);
            CombatUnit[] rootUnits = reachable.SelectMany(rule => new[] { rule.SpawnUnit, rule.Summon?.Additional?.Unit }
                .Where(unit => unit != null).Cast<CombatUnit>().Concat(rule.Summon?.Pool.Select(choice => choice.Unit) ?? Array.Empty<CombatUnit>()))
                .Concat(spawn.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units).Concat(spawn.Treasures).Select(item => item.Unit)).ToArray();
            var assets = new HashSet<string>(rootUnits.Select(unit => unit.AssetKey), StringComparer.Ordinal);
            CharacterData[] characters = all.GetAllCharacterData().Where(data => data != null && assets.Contains(data.GetAssetKey()))
                .Concat(live.Select(unit => unit.GetSourceCharacterData())).GroupBy(data => data.GetAssetKey()).Select(group => group.Last()).ToArray();
            var births = characters.Select(data =>
            {
                var errors = new List<string>();
                CombatUnit unit = BattleActionProbe.SpawnTemplate(data, true, errors);
                EnemyDefinition raw = EnemySpawningProbe.Definition(data);
                var hero = new EnemyDefinition(raw.Unit, raw.Ascends, raw.Loops,
                    raw.ExternalInteractions.Concat(spawn.ExternalInteractions).Distinct().ToArray(), raw.CompanionBoss);
                return new UnitCopyBirthDefinition(unit, errors.Distinct().ToArray(), data.GetGraftedEquipment() != null, hero);
            }).OrderBy(item => item.Unit.AssetKey, StringComparer.Ordinal).ToArray();
            string[] cardIds = reachable.Select(rule => rule.DataId).Concat(live.SelectMany(unit => unit.GetEquipment())
                .Select(card => card.GetCardDataID())).Concat(live.Select(unit => unit.GetSpawnerCard()?.GetCardDataID()).Where(id => id != null).Cast<string>())
                .Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
            CardData[] cards = cardIds.Select(id => all.FindCardData(id)!).ToArray();
            var gear = cards.Where(data => data.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAttachEquipment") ||
                data.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitGraftedEquipment")).Select(data =>
            {
                var upgrades = data.GetEffects().Where(effect => effect.GetParamCardUpgradeData() != null).Select(effect =>
                { var upgrade = new CardUpgradeState(); upgrade.Setup(effect.GetParamCardUpgradeData()); return CardModifierProbe.Upgrade(upgrade); }).ToArray();
                return new UnitCopyGearDefinition(data.GetID(), upgrades, data.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitGraftedEquipment"));
            }).ToArray();
            CardData[] abilities = cards.Where(data => data.IsUnitAbility()).Concat(characters.Select(data => data.GetUnitAbilityCardData())
                .Where(data => data != null)).Concat(live.Select(unit => unit.GetUnitAbility()).Where(data => data != null))
                .GroupBy(data => data.GetID()).Select(group => group.First()).OrderBy(data => data.GetID(), StringComparer.Ordinal).ToArray();
            return new UnitCopyCatalog(births, cards.Select(CardGenerationProbe.Creation).ToArray(), gear,
                abilities.SelectMany(data => new[] { AbilityLifecycleProbe.Change(data, false), AbilityLifecycleProbe.Change(data, true) }).ToArray());
        }
    }
}
