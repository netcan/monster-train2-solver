# Untouchable removes an impacted room upgrade

Sample16 Ring6, turn6, input34 replaces Sentient(unit2)'s StoicPlatemail
with StasisCrystal(card9). Native RoomState.UpdateTrainRoomAttachments
removes EnchantedChamber's upgrade when the character gains Untouchable.
The old forecast incorrectly kept that upgrade:12 records rather than11.

The original34-input counterexample and an independent33-input prefix plus
replacement pair match complete state, forecasts and RNG. Four concurrent
branches leave their parent unchanged. Modified attachment asset, upgrade
identity and Health payloads are rejected. Only the captured zero-stat
EnchantedChamber upgrade is calibrated; other payloads still need native
evidence. The ordinary attachment reinstall pair also passes.

This component does not establish a complete Ring6 victory.
