namespace MonsterTrain2Poju.Model
{
    // Keep small health differences when summing large terminal-score offsets
    // over many weighted hypotheses. A loose relative tolerance loses those
    // differences at the evaluator's 1e12 win/loss scale.
    internal struct CompensatedSum
    {
        private double sum;
        private double correction;
        internal double Value => sum;
        internal void Add(double value)
        {
            double adjusted = value - correction;
            double updated = sum + adjusted;
            correction = updated - sum - adjusted;
            sum = updated;
        }
    }
}
