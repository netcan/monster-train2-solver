#if DEVELOPMENT_PROBE
using System;
using System.Threading;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AdvisorRapidContinuationProbe
    {
        private static readonly ManualResetEventSlim release = new ManualResetEventSlim(false);
        private static int armed, verificationThread, timedOut;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_RAPID_CONTINUATION") == "1";
        internal static int MainThread { get; private set; }
        internal static int VerificationThread => Volatile.Read(ref verificationThread);
        internal static bool Blocked => VerificationThread != 0 && !release.IsSet && Volatile.Read(ref timedOut) == 0;

        internal static void Install(Harmony harmony)
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
            harmony.Patch(AccessTools.Method(typeof(VerifiedAdviceContinuation), "MatchObserved"),
                prefix: new HarmonyMethod(typeof(AdvisorRapidContinuationProbe), nameof(HoldFirstVerification)));
        }

        internal static void Arm() => Volatile.Write(ref armed, 1);
        internal static void Release() => release.Set();

        internal static bool AwaitingBarrier()
        {
            if (Volatile.Read(ref timedOut) != 0)
                throw new InvalidOperationException("The native rapid-continuation acceptance timed out while verification was held.");
            return !Blocked;
        }

        private static void HoldFirstVerification()
        {
            if (Interlocked.Exchange(ref armed, 0) == 0) return;
            int thread = Thread.CurrentThread.ManagedThreadId;
            if (thread == MainThread)
                throw new InvalidOperationException("The native player verification must run off the Unity thread.");
            Volatile.Write(ref verificationThread, thread);
            if (!release.Wait(TimeSpan.FromSeconds(15))) Volatile.Write(ref timedOut, 1);
        }
    }
}
#endif
