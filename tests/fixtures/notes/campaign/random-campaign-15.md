# Natural sample 15: Stygian / Wurmkin

Seed 494122740, Covenant 0, default 10000 ms, two search workers. Rings 1–6
each have a source win and a second independent native win, identical inputs,
complete states, forecasts and RNG, and zero capture/mismatch/unsupported/
pending failures. No offline route refinement was used.

| Ring | Runtime | Inputs / plays / EndTurn | Train stages | Normal reward/map events |
| --- | --- | --- | --- | --- |
| 1 | 0.2.165 | 24 / 19 / 5 | 10 | 52 |
| 2 | 0.2.166 | 27 / 22 / 5 | 9 | 42 |
| 3 | 0.2.167 | 42 / 35 / 7 | 13 | 58 |
| 4 | 0.2.167 | 39 / 32 / 7 | 14 | 57 |
| 5 | 0.2.167 | 51 / 44 / 7 | 14 | 41 |
| 6 | 0.2.167 | 75 / 64 / 11 | 21 | 44 |

The ring 3 replay advances normally to ring 4, save SHA256
0B850480E268BE1B59529C36FBD4E386C9A868A5DEC6C02FF1CD4D18326FC884.
The ring 4 replay advances normally to ring 5, save SHA256
D9265FE2BCA41D3ADE44E11F12E19C51DDA394ADB5384032DE835A81CE784C99.
The ring 5 replay advances normally to ring 6, save SHA256
8D8C6144A452F9EB6AEB251386F24B8132E1750824077573E4D8C5BD2F4B69B7.
The ring 6 replay advances normally to ring 7, save SHA256
622770A51E5667FC9B1F7B243CB5EACF03D72E2C3144E6BC825FF3394911FDEF.

`Check-RandomCampaignChain.ps1 -ThroughRing 6` independently checked the
unchanged three journals, archive hashes, paired DLL identity and uninterrupted
save chain without rerunning simulations. CompleteStandardCampaign remains
false. Ring 7's default source is a complete native defeat: 68 inputs,
58 plays, ten EndTurns and 20 train stages; Rax survives with829 HP.
All full states, forecasts and RNG match, with four zero failure counters.
It is retained as a strategy counterexample, not a win or proof that no winning
route exists. No independent losing replay was run.

Journals: random-campaign-20261026-235135-b4f2d6ca,
random-campaign-20261027-001229-d9f469b9,
random-campaign-20261027-003133-5b2ab897.
