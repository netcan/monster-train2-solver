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

This pair proves birth. Draw, revenge combat and full-battle wins still need
the resumed natural campaign; this single input is not counted as a win.

Run `scripts/Check-Models.ps1 -Fixture player-sentient-draw-upgrade-calibration.mt2f`.
