# Deferred floor browsing · Windows 0.2.17

Changing the viewed floor used to capture the entire native battle, hash it,
replay the remaining forecast and publish another advice object. Even a reused
route dimmed the panel while checking, and native preview loading could schedule
yet another capture.

The overlay now records selection callbacks in order and keeps the current
publication, text, status, color and scroll position. A view event does no native
capture, detached snapshot hashing, model replay or route search. Selection-only
preview/loading frames preserve that publication too. Views during snapshot
preparation or an in-flight search do not cancel that work.

After an actual card/turn/mutation boundary, the normal complete native capture
runs. The session reproduces the recorded selection callbacks from the prior
published root, matches a full decision/RNG prefix, then verifies the remaining
forecast from the actual new root. Replaying from that root retains the actual
preview allocator in the forecast. Conduit/room spell modifiers, private tracker
histories, costs, physical identities and RNG remain checked. Manual reset clears
the view history and starts a new search.

Cheap resource/pile/turn changes and real input/mutation hooks still wake the
ordinary capture path. The selected-room stamp alone does not. The existing
native callback calibration is documented in [live-scroll-debug.md](live-scroll-debug.md).

Focused checks extend the captured Conduit wheel fixture with seven deferred
view notifications, exact publication identity, unchanged generation/search
count, a following advised input and complete suffix replay. Energy/RNG changes,
manual reset and views during an in-flight search are also checked. Existing
ready/in-flight floor rebasing and resource/cost/RNG rejection checks remain.

The isolated player acceptance asserts zero overlay captures on all seven native
wheel inputs, including one during search. For an already published route it
also checks the advice object, publication count, text, status, color and scroll
position before taking its own explicit diagnostic capture. That independent
capture compares the complete native selection transition and RNG with the
recorded callbacks; it is not part of the player's browsing path.
