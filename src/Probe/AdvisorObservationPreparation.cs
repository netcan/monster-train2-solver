using System.Threading.Tasks;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AdvisorObservationPreparation
    {
        internal static PreparedBattleObservation? ReadIfCurrent(Task<PreparedBattleObservation> completed, bool current)
        {
            if (current) return completed.GetAwaiter().GetResult();

            // A fault after an input or reset belongs to the retired snapshot.
            _ = completed.Exception;
            return null;
        }
    }
}
