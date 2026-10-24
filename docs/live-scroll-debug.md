# Live floor scrolling · Windows0.2.11

The0.2.10 isolated starter acceptance missed selection-sensitive contributors:
it scrolled before any Conduit unit was deployed. In the user's running game,
scrolling after play still changed generation and launched a3.1-second search.
This was reproduced directly in PID59976, started2026-10-24 17:22:19, with
the real0.2.10 player, whole-battle mode, two workers and3000ms budget.

Full captures around the native wheel handler show the actual differences:

| Captured field | Floor1 to2 (zero-based) |
| --- | --- |
| SelectedRoom and every QueryFrame.SelectedRoomIndex | 1 to2 |
| CardInstances/CardRegistry card3 temporary upgrades | Add hidden ConduitUpgrade, damage/heal+1, identity conduit:1 |
| Conduit contributor1 tracked Cards | Empty to[3] |

Those are normal selection callbacks.0.2.10 normalized the room number but
still compared the other fields literally, classifying this complete callback
transition as a gameplay input. The diagnosis does not justify ignoring card
upgrades, tracker histories, costs or RNG.

`SelectionAdviceReuse.SameBattle` now keeps its exact existing fast comparison,
then, only for different known selected rooms, runs
`BattleTurnModel.SelectRoom(previous, current.SelectedRoom)`. It accepts the
selection only when that entire modeled callback/preview result matches the
complete current native root, including RNG and all costs/modifiers/trackers.
Only the existing next-unused preview-copy allocator normalization remains.
Unsupported callbacks and unrelated resource/cost/RNG changes still reject reuse.
Rebasing also replays the complete remaining forecast on a background worker.

A temporary Harmony prefix applying the same selection comparison was attached
to the running game without restarting it. Seven native wheel inputs after play
cover2->1->0->1->2->1->0->1. Search count stays4 and generation stays6 throughout;
all seven resulting publications have SearchMs=0 and ReusedSelection=true.
Native captures have zero purity failures and zero overlay failures. Every
captured complete transition matches the model's native selection callbacks/RNG.
The game remains at turn2, energy2, with the originally selected floor1 restored.
No cards or EndTurns were executed by this live diagnostic.

Temporary observation/command hooks were removed at17:30:25. The selection fix
remains active until this game process exits. The loader still reports0.2.10
for this process; installed0.2.11 loads on the next launch. The runtime fix is
not a claim that this process has loaded the new player binary.

The complete captured fixture is `player-live-conduit-floor-scroll.mt2f`:
eight-step existing forecast, before/after roots, all seven observations and
the accepted live overlay/counter/root summary. Its SHA256 is
`3c953161d3622a70bed03ba486e8d6a0f2d96f789c45edccd632e5806d444b4a`.
Local diagnostic evidence is under `.probe-runs/live-scroll-debug`: original
failure `roots-1.json`, fixed observations `roots-101.json` through107,
`trace.log`, and `live-accepted-summary.json`. These are live observations,
not an autoplay/frozen full-battle victory pair.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-floor-scroll-only tests/fixtures/player-live-conduit-floor-scroll.mt2f
```

The checker independently reproduces every full native selection transition,
replays the complete eight-step forecast after three reuse switches, verifies
one search and stable generation, and rejects unrelated energy/RNG changes.
Existing ready/in-flight floor/cost/RNG controls and11 ordinary inputs across
three turns pass. The fixture catalog passes1355 hashes/registrations without
a full regression. The exact installed production Model.dll also passes the
live fixture in an isolated copy of ModelChecks; its SHA256 is
`e184a5d19d088e496382c073d36a675b7fb57e2d150f231b0066ecef236a49a8`.

These checks establish removal of the repeated search in the reproduced live
Conduit case. Selection still performs native capture and background replay;
debug snapshot/JIT/GC overhead is not a user latency or FPS benchmark.
