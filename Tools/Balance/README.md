# Balance smoke simulation

From the repository root, with .NET 8:

```sh
dotnet run --project Tools/Balance/BalanceSimulation.csproj
```

This uses the exact `BalanceRules` catalog, weighted stages, offer fallback,
24 rotations and BoardModel line-clearing logic used by the game. Each seed
plays until no remaining offer fits, or a simulation-only cap of 240 placements.
The game itself has no placement cap, time limit or forced-loss counter.

The sampled-random agent chooses between sampled valid poses. The clear-seeking
agent samples poses and favors immediate clears and contact with existing blocks.
Neither is a complete search or a model of a human player. Both also have an
exhaustive fitting-pose fallback, so a failed random search is not called Game Over.

Results from the current .NET 8 run (seeds 0–11 per agent):

| Agent | Game Over | Median placements | Range |
| --- | --- | --- | --- |
| Sampled random | 12/12 | 38 | 31–44 |
| Clear-seeking | 12/12 | 50 | 44–70 |

Raw output is in `sample-results.txt`. These observations show that losses are
reachable and this simple clearing policy lasts longer. They do not prove a
particular human difficulty or establish comparison with the former generator.
Human playtesting is still needed to tune the thresholds and weights.

## Runtime rules

The 8×8×8 board, eight-cell line length, points and zoom stay the same.
Difficulty changes only when a fresh set of three offers is generated:

| Placements already completed | Stage | Cells per piece |
| --- | --- | --- |
| 0–8 | Разминка | 3–8 |
| 9–26 | Темп | 5–12 |
| 27+ | Вызов | 7–16 |

A set contains three distinct catalog entries. Larger pieces become more likely
as the stage advances. The exhaustive rescue searches only the eligible catalog
of the current stage. Single-cell and two-cell pieces cannot keep reviving a
board full of isolated holes. A playable fresh set is guaranteed whenever an
eligible shape fits; remaining offers still determine Game Over after a move.

`Assets/Scripts/Gameplay/Model/BalanceRules.cs` owns the 18 connected shapes and
weights. Legacy Resources prefabs are retained as assets, but their old category
weights no longer drive gameplay. The selected catalog geometry is assigned to
an inactive FigureGridShape before Awake; its mesh, ghost, thumbnail and rules
therefore use the same cells. Geometry is no longer inferred from unrelated art.

## Paired input comparison

```sh
dotnet run --project Tools/Balance/BalanceSimulation.csproj -- --paired
```

This freezes 80 sets per seed before either agent sees a board. Each pair gets
identical catalog indices and displayed orientations (the sequence checksum is
printed in both rows). The normal runtime's board-dependent rescue is excluded
in this benchmark: otherwise different agent boards could produce different
replacement pieces even with the same random seed. Catalog weights/stages and
clearing rules stay the same. This is a controlled-input test, not an exact
replay of the runtime's adaptive offer fallback.

`confirm-default` only places displayed shapes at the neutral start on the -Z
face, with a fixed viewpoint, without moving or rotating. `clear-seeking` uses
its existing legal-pose search. Results for seeds 0–11 are in `paired-results.txt`:

| Agent | Placements | Stop reason |
| --- | --- | --- |
| Confirm default | 1–2 (median 1) | Manual input required, 12/12 |
| Clear-seeking | 44–70 (median 50) | Game Over, 12/12 |

A blocked default preview with another valid placement is **not Game Over**.
The first agent is simply unable to continue under its no-movement restriction.
These observations check removal of spawn assistance; they do not establish
human difficulty or justify changing field size or shape weights again.
