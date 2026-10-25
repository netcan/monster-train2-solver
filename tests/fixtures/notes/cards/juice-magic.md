# Merchant Juice on MagicJuice

The eleventh random Stygian/Awoken campaign (seed73751504) reached Ring5
with native MagicJuice card10 carrying merchant TraitJuice. The former
global card validation rejected every action, including a plain Steward
deployment, because this owner had not been calibrated. The original
one-input native refusal remains in `player-juice-magic-calibration.mt2f`.

Version0.2.104 accepts only owner`9ea70171-8afd-4fab-ad54-7ff5c8b478b8`,
ordered ordinary Exhaust0/Juice2 traits and the single permanent merchant
upgrade`0fc9f6e7-df71-4045-9867-67453769adf8`. Ordered status-scaling callbacks
and other signatures remain guarded. Existing status-doubling arithmetic
and exhaust behavior are reused.

The observation/replay fixtures execute the same13-input legal prefix from
deployment in two independent native processes. The14th input casts card10
at Turn2 into room0 with enemies12,13,14: native spell weakness2 becomes4
on each, health and energy remain unchanged, and the card is consumed.
Source-attributed status statistics increase by12. Every root, prediction,
successor and RNG matches; eight parallel branches leave their parent
unchanged. Ten altered owner/scaling/trait/upgrade controls remain rejected.
The original refusal has the same full deployment root. All protected game
files remain unchanged. This component pair is distinct from a full Ring5
default-advice victory.

Focused verification:

```powershell
scripts/Check-Models.ps1 -Fixture player-juice-magic-calibration.mt2f -SkipBuild
```
