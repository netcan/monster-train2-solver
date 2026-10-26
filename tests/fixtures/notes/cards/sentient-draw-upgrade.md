# Original Sentient draw champion upgrade

Natural sample 16: Awoken/LunaCoven, seed 1168994391, Covenant 0. The
original upgrade f8032f84-d804-4b2e-ad51-679f949f5eb2 / SentientDrawOnRevenge
contains +30 HP, a captured revenge trigger and one
RoomStateHandSizeModifier(+1), allowed during deployment. This was read from
the unchanged original checkpoint, not inferred from the translated name.

The source preserves the old explicit room-upgrade rejection. The independent
0.2.169 process plays the same champion instance 3 on the top floor and agrees
on every field, forecast and RNG. The native unit has 40 HP, one draw aura and
the exact captured OnHit trigger. Four owned branches preserve the parent.
The focused pair rejects altered HP signatures; native capture also guards
changed room modifier count, type, amount or deployment flag. Live-unit upgrade
and removal APIs retain their unsupported guard.

The separate full-battle pair now passes 26 inputs, 21 plays, five EndTurns
and nine train stages in two unchanged native processes, both wins with zero
capture/mismatch/unsupported/pending counters. Every successor, draw, forecast
and RNG agrees; 52 normal reward/map events advance to ring 2. The curated
sample-16 ring-1 archives retain this broader evidence separately from birth.

Run `scripts/Check-Models.ps1 -Fixture player-sentient-draw-upgrade-calibration.mt2f`.

0.2.174 adds the independently observed second tier from the same campaign's
natural Ring4 checkpoint. Upgrade09621571-ddd8-4ee9-bf99-55c846d565bd /
SentientDrawOnRevenge2 has +70HP, the same one draw aura, and a revenge trigger
that gives the weakest ally +3attack/+3health. The source0.2.172 rejection and
fresh0.2.174 replay both deploy champion6 on the top floor. All successors,
forecasts and RNG match: native80HP, exactly one draw1 aura and exact revenge
target/stats. Four independent branches preserve their parent. Altered HP and
crossed tier identities remain rejected; capture applies the existing room
definition count/type/value/deployment checks to both tier IDs. The original
first-tier independent pair remains unchanged and passes. Third-tier signatures
and live upgrade/removal remain guarded. Ring4 full-battle acceptance is separate.

Source: full-battle-units-spells-and-junk-20261027-014508-2214f843.
Replay: full-battle-units-spells-and-junk-20261027-014654-76d93549.
Run `scripts/Check-Models.ps1 -Fixture player-sentient-draw-upgrade2-calibration.mt2f`.
