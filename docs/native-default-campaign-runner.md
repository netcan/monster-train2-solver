# Continuous native default campaign acceptance

`Run-NativeDefaultCampaign.ps1 -Rank 3 -InputProfile <native-BattleIntro-profile>
-StopAfterRing 8 -SkipBuild` chains the existing focused acceptance workflows.
It starts at a progressed ordinary battle (Ring2–8), preserves the original
rank/seed and every preceding victory, and requires a real default advice win
plus an independent frozen native replay before advancing original rewards,
drafts, shops and map choices to the next battle. Each accepted input is zipped
and restored with hash verification. Accepted pairs are journaled before
continuation, so later failures retain the completed evidence.

The generated `.probe-runs/default-campaign-*/accepted-rings.jsonl` records
each pair and its separate native continuation. A loss, missing advice,
nonzero failure counter or changed input stops the batch at that exact ring;
later rings are not counted. `LastAcceptedRing` is the bounded battle progress;
only the separate `NativeCampaignWon`/`NativeVictoryType` fields establish
native campaign settlement. The last ordinary battle additionally replays
through native EndGame in the same process so the existing completed-Standard
archive verifier can preserve the full final trace and metagame settlement;
that archive is restored and verified too. Timeouts apply to each native process.
New default source runs also exercise seven actual floor switches, including
one during search. The native source gate requires zero new searches and an
unchanged generation; the independent pair gate checks all seven selection
reuses and their zero search time, and the frozen process repeats the switches.
This runner adds no encounter/deck authoring, full regression or Mac build.

Input preflight rejects wrong difficulty and an already completed target range
before creating a journal or launching the game. Actual campaign acceptance
is recorded separately in [native campaign evidence](../tests/fixtures/notes/campaign/native-covenant-campaign.md).

The first positive integration is natural Covenant3 Ring5: both actual-default
native processes win with43 inputs and Pyre72; independent full-state/RNG
pair acceptance passes,all15 checkpoint payloads restore,and original
reward/map progression reaches Ring6 with five native victories. This is
recorded in the Ring5 fixture section; it does not establish later ring wins.
