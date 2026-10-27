# Inactive Fragile boss leaving Inferno

Sample25 Ring8 release291 followed the actual 10000ms/two-worker advice for
15 card plays and four end turns before the fourth turn exposed a native
moon-mask mismatch. With tracking inactive, the original Fragile boss leaves
the original Inferno room and loses its room upgrade: native understanding
changes from6 to5, while the historical model retained6. The archived battle
is incomplete and is not a victory or a full-battle acceptance.

Release292 handles this exact original primary movement/upgrade signature.
The checker replays all15 card actions and four turns against complete native
state, forecasts and RNG; the historical difference is solely the boss mask.
Four parallel branches preserve the parent, and three altered room/upgrade or
moon-callback signatures remain rejected. Frozen revision f5784d08 independently
replayed all19 original inputs and matched every native state/forecast/RNG.
An actual-budget Ring8 continuation passed the original boundary but found a
separate subsequent room-exit mask mismatch; complete battle acceptance remains
outstanding.
