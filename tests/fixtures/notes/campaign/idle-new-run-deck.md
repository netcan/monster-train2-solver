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
