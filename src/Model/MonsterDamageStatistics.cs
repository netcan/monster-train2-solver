namespace MonsterTrain2Poju.Model
{
    // Introduced in 2.3.1. Absence on BattleStatistics preserves the older rules.
    public sealed class MonsterDamageStatistics
    {
        public int ThisTurn { get; }
        public int ThisBattle { get; }
        public int FriendlyThisTurn { get; }
        public int FriendlyThisBattle { get; }

        public MonsterDamageStatistics(int thisTurn, int thisBattle, int friendlyThisTurn, int friendlyThisBattle)
        { ThisTurn = thisTurn; ThisBattle = thisBattle; FriendlyThisTurn = friendlyThisTurn; FriendlyThisBattle = friendlyThisBattle; }

        internal MonsterDamageStatistics Increment(int amount, int friendlyAmount) => new MonsterDamageStatistics(
            unchecked(ThisTurn + (amount > 0 ? amount : 0)), unchecked(ThisBattle + (amount > 0 ? amount : 0)),
            unchecked(FriendlyThisTurn + (friendlyAmount > 0 ? friendlyAmount : 0)),
            unchecked(FriendlyThisBattle + (friendlyAmount > 0 ? friendlyAmount : 0)));
        internal MonsterDamageStatistics NextTurn() => new MonsterDamageStatistics(0, ThisBattle, 0, FriendlyThisBattle);
        internal string Signature() => ThisTurn + ":" + ThisBattle + ":" + FriendlyThisTurn + ":" + FriendlyThisBattle;
    }
}
