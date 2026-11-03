# Board model tests

The same NUnit tests can run without a scene in Unity 2022.3.62f3
(Test Runner → EditMode → BlockBlast.Model.Tests), or with .NET 8:

```sh
cd /workspace/3D-Block-Blast
dotnet test Tools/Tests/BoardModel.Tests.csproj
```

In the current cloud instance the SDK is installed outside the checkout:

```sh
/workspace/tools/dotnet/dotnet test Tools/Tests/BoardModel.Tests.csproj
```

24 cases cover occupancy, non-origin placement, boundaries, overlap rejection
without partial writes, invalid shapes, full/incomplete planes on all axes,
intersecting plane clearing, and placement availability on a full board.
Additional cases cover full lines on each axis, incomplete lines, parallel and
intersecting lines, unique cell removal, plane bonuses, combo scoring, and a
completely occupied cube.

These tests do not validate Unity object creation, scene wiring, rendering,
or the offer/orientation policy in GameManager. Those still need Unity checks.
Gameplay now clears lines of 8 cells along X/Y/Z. Plane detection is retained
for an additional 100-point bonus per full plane, captured before clearing.
Scoring: placed cells × points per block + lines × 10 + max(0, lines − 1) × 5
+ full planes × 100.

## Unity smoke check

Open Game in Unity 2022.3.62f3 and enter Play Mode. Complete a line of eight
cells along each axis: only that line should pop and disappear, unrelated
blocks should stay in place. A one-block placement completing one line gives
11 points with the default settings. Complete two intersecting lines in one
move: the shared block should animate once and the line/combo reward should
be 25 points plus the placed-block points. Verify the next shape cannot be
selected during the spawn/clear animation and becomes selectable afterwards.
A full plane captured at placement grants 100 extra points in addition to its
16 lines and combo. These scene checks have not been run in the cloud because
the Unity editor is not installed.
