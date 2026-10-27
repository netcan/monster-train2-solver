# Keep both upper and lower opening policies

Sample25 Ring7 has a complete59-input bottom-floor model win at Pyre46.
The original286 default10000ms/256-node/two-worker opening instead returns
a61-input complete defeat, Rax730. Both forecasts retain the exact same native
deployment root/RNG and input SHA256. Separate actual285 default60-input
native defeat is retained; neither offline forecast counts as native success.

The old portfolio kept the tactical worker on the preferred top floor while
the ordinary worker adapted only if its short forecast lost an already placed
ally. A quiet opening could therefore send both workers to the same floor.
287 assigns fixed upper and lower tactical seeds to the two ordinary opening
workers. Both retain the complete legal action search; shared node/time limits
are unchanged. This applies only to two-worker, whole-battle, shorter-horizon,
default preferred-floor2 openings with no living placed allies. Single-worker,
later-turn, explicit tactical/floor and strict-horizon options keep their prior
strategy selection.

The focused unchanged-root test now obtains the59-input/Pyre46 model win under
the same10000ms/256-node/two-worker options and independently replays full
state/RNG without mutating the root. The old upper-floor Pyre37 control still
passes. Existing single/two-worker terminal wins, separate threads, shared
one-node/one-step caps, expiry and cancellation controls also pass.

Original model counterexample33115bytes/3849nodes/SHA256
d54f846e2249312331388555931501082e284ede6d7c09eccc660960213366bd;
lower route92438bytes/12430nodes/SHA256
adc6cfb128517f26d6553c0cf6667cb122ef30e11ce815c18c979272723d87bb.
Actual default-budget native execution and independent replay remain required.
