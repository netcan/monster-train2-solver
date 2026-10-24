# Identify upgraded cards in advice · Windows 0.2.22

Advice already selects a card by its captured instance identity, but the panel
previously showed only its localized title. Different upgrades could therefore
leave two copies indistinguishable to the player.

Each ordinary card step now shows its position in the current hand, counted
from the left, its current displayed energy cost, and its permanent upgrades.
Upgrade names include their numeric changes; temporary upgrades appear
separately. For example:

```text
卡名 · 当前手牌左起第 3 张 · 当前 1 费
当前升级：升级名称（法强+10）
第 3 层 · 目标：单位名称
```

The position and cost describe the current hand and selected floor, rather than
inventing a hand position or price for a future step. A known card outside the
hand says `当前不在手牌` and omits the current cost. Generated cards whose native
instance does not exist yet keep their existing predicted-name fallback. Each
verified publication updates these descriptions after cards leave or enter the
hand. Both collapsed current-turn steps and the expanded plan use this format.

Hovering a card step outlines its exact native hand card in gold. It follows
wrapped lines and viewport scrolling, checks the card behind a pooled CardUI,
and clears when the pointer leaves, advice becomes pending, or the panel hides.
The outline is a separate overlay with raycasts disabled; it does not select or
focus a native card, alter its native edge effects, or request capture/search.

## Focused native acceptance

Advisor and Probe Release builds pass. The actual production 0.2.22 DLL passes
the isolated native run
`.probe-runs/full-battle-units-spells-and-junk-20261024-233349-b0cfc357`:

- Three description cases cover same-title detached native cards with different
  costs/upgrades, a temporary upgrade, and a card outside the current hand.
- Five hover checks exercise two actual same-title native hand instances in
  expanded/collapsed views, viewport clipping/scrolling, pointer exit, and the
  disabled-advice gate. Native state/RNG, advice identity and capture/search
  counts remain unchanged.
- The battle finishes with a win after 21 plays and seven EndTurns. All four
  native failure counters are zero, original player files are unchanged, and
  native continuation/idle/floor/panel checks pass. Six collapsed turn changes
  still return to the first step of the new turn.

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Seed 424242 -BattleDistance 0 `
  -Policy units-spells-and-junk -PlayerAdvisor -RollingAdvice -AdvisorFloorBrowse `
  -AdvisorPresentation -PlannerWholeBattle -PlannerSearchWorkers 2 `
  -PhysicalSpawnPoints -BranchCardMasks -SkipBuild -TimeoutSeconds 300
```

This is a focused UI/native continuation acceptance, not a full model regression
or a claim about untested upgrade combinations. No Mac build is required.

## Windows delivery

Installed at 2026-10-24 23:39:00 from `e4137b42` to
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
All three installed DLL hashes match the accepted build and checked ZIP;
28 other loader/plugin/configuration files, including F10 and the user's
10-second search budget, remain unchanged.

The previous and accepted payloads, retained in-use DLLs and installation
receipt are under `BepInEx/PojuBackups/CardIdentity-20261024-233751`.
The Windows package is
`results/mod-dist/PojuAdvisor-0.2.22-Windows-20261024-233751.zip`.
The running player PID46732 retains its loaded 0.2.21 assembly and needs a user
restart to load 0.2.22. No real player input/restart or Mac build occurred.
