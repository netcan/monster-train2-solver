# Late automatic view during snapshot preparation

Natural sample 16 ring 2, seed 1168994391. Three deployment plays and the
first EndTurn match the original game. The overlay then publishes a prepared
middle-floor root while the game has returned to the bottom floor. The
native diagnostic records both full roots and their original keys: only the
outer selection and all five shared query-frame selections differ, with no
pending view history. Reduced battle comparisons previously hid these fields.

When a floor changes while preparation runs, preserve that view event after
the newly observed worker's committed prefix. It must survive even if an older
worker already saw that room. No additional capture, cancellation or search
is introduced by recording the event. Native mismatch diagnostics now retain
the complete snapshots, not just a reduced battle comparison.

The focused native-root check reproduces the lost-history failure, gates the
worker to force the race, then proves the repaired publication matches the
late view with one search and unchanged generation. An energy change still
fails matching. Modeled selection agrees with native using the existing
unused-preview-allocator normalization; existing identities and RNG remain keyed.

The first repaired native execution completes 26 inputs and wins, with all
four error counters zero. A clean committed build and independent native
replay remain necessary for release. The separate automatic-view continuation
fallback still needs work to remove a post-EndTurn search.
