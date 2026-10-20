using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class JackInTheBoxProbe
    {
        internal const string CardId = "5fdc790d-961b-4c95-ad0c-bbb945da4e39";
        internal const string Effect = "CardEffectUnboxJackInTheBoxUnitCard";
        internal static bool Enabled => OriginalSupportSpellsScenario.IsolatedIds?.Contains(CardId) == true;
        internal static int[]? Capture(CardEffectState effect, CharacterState owner)
        {
            if (effect.GetCardEffect() is not CardEffectUnboxJackInTheBoxUnitCard jack) return null;
            var pool = jack.GetBoxedCharacterDatas();
            if (pool == null) return null;
            var boxed = owner.GetSpawnerCard()!.GetCardStateModifiers().BoxedCardStates.ToList();
            return pool.Select(item => boxed.IndexOf(item.Item2)).ToArray();
        }
        internal static void Prepare(AllGameManagers managers, IEnumerable<CardState> created, string[] ids)
        {
            if (!ids.Contains(CardId)) return;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData();
            CardState box = created.Single(card => card.GetCardDataID() == CardId);
            CardData spear = all.FindCardData("38e70e6a-20f3-4e36-8d31-c5110be07f87")!;
            TriggeredSummonProbe.Register(spear.GetSpawnCharacterData());
            if (ids.Contains("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")) return; // Independent empty-box control.
            foreach (int attack in new[] { 3, 7 })
            {
                var card = new CardState(spear, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAttackDamage(attack); upgrade.SetAdditionalHP(50);
                card.ApplyPermanentUpgrade(upgrade, save);
                box.GetCardStateModifiers().BoxCardState(save.AddBoxedCardState(card), save);
            }
            var spell = new CardState(all.FindCardData("f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")!, save);
            box.GetCardStateModifiers().BoxCardState(save.AddBoxedCardState(spell), save);
        }
    }
}
