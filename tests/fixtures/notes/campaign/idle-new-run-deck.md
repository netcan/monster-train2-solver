# Post-EndGame setup has no active deck

Sample23 is the next unchanged Random(20261025) entry:Underlegion/LunaCoven,
seed47305607, Covenant0. Starting from sample12's native idle completed profile
fails in release238 before clan selection:CampaignProgressScenario's screen
audit calls Select on SaveManager.GetDeckState while the setup deck is null.
The native audit records new-run followed by ArgumentNullException; its source
profile and the player's real files remain unchanged.

Release239 permits a null deck in that diagnostic screen record. The native
setup, original UI, reward selection, metagame progression and battle acceptance
remain responsible for creating and advancing the run. A fresh same-profile
new-run retry must pass before claiming this continuation works for sample23.

The release239 native retry now reaches the first-battle checkpoint through the
unchanged new-run UI:campaign-ring1-20261027-170125-8b5ef618. Battle advice is
blocked separately by Upgrade room modifiers. This verifies that the setup
exception is removed, but does not yet establish a battle win or campaign clear.

Check-NativeMetagameContinuation verifies the saved sample12 completed archive
and sample23 opening archive (SHA25630771bed40a8fd1605cb528811876ebad2d413eb1f480f0fa13cbb8ff1ed2ae8).
All21 prior tracked values persist, a native dialogue completes (0->1), the
source and real files remain unchanged, and no battle simulation is required.
This is retained native progression, not a true-final victory acceptance.
