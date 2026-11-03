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

46 cases cover occupancy, non-origin placement, boundaries, overlap rejection
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
16 lines and combo. These scene checks have not been run in the cloud: Unity 2022.3.62f3 is
now available, but it cannot open the project without an activated license.


## Offers and mobile controls (steps 5–6)

The suite now also checks the 24 proper cube rotations, preserved geometry,
four-turn identity, concrete offers and defensive copies, rotated placements,
exhaustive fallback to a playable catalog entry, full-board/no-move cases,
all six placement faces, all eight depth layers, bounds, and reachability of
all orientations using the two plane rotation buttons.

Runtime scripts additionally compile against the official Unity 2022.3.62f3
assemblies. This is a C# API/compilation check, not a Unity import or Play Mode
result. Unity's own EditMode attempt stopped at licensing before test execution.

### Combined Unity check

1. Open `Assets/Scenes/Game.unity`. The bottom control panel should appear
   automatically, with all three offers visible above it; no Inspector wiring
   is required. Check portrait mode and the target phone's safe area.
2. Before selection, drag the cube freely in any direction. On a phone, pinch
   to zoom; in the Editor, use the mouse wheel over the game gesture regions.
   Releasing one finger after pinch must not start rotation or placement.
3. Choose an offer. Its preview and target ghost should have the same shape
   and orientation. Drag horizontally/vertically from the current viewpoint:
   the ghost should move in the two axes of the closest face; the cube should
   remain still. Test after viewing the cube from every side.
4. Move the depth slider through 1–8 (near to far anchor layers). A thick shape
   can extend outside the board at an edge layer: it must turn red and disable
   placement. Move across occupied blocks: red ghost cells must remain visible.
5. Use both rotation buttons to change the offer's orientation. The thumbnail
   should update too. Find a valid position: the ghost turns green and
   `Поставить` enables. Releasing a drag or tapping the gesture region must
   never place a figure; only the confirmation button places it.
6. Cancel a selection. No blocks or points should be added, and the same offer
   must remain available. Select another offer and verify normal placement.
7. While a spawn/line-clear animation runs, try selecting another offer,
   dragging, rotating and confirming again: the input must remain locked.
8. Use all three offers: a fresh set must contain a playable concrete offer
   whenever any catalog shape can fit in any of its 24 rotations. For remaining
   offers, Game Over must only occur when all of their allowed rotations fail;
   catalog shapes that were not offered do not postpone Game Over.

Existing scene component names/GUIDs are retained. `SwipeZoneFigure` and
`GameInputBinder` remain as compatibility components; gesture interpretation
is centralized in `FieldRotator` to prevent duplicate movement on release.

### Offer click regression

`OfferInteractionTests.VisibleOfferReceivesClickAndCreatesAnActiveFigure` enters
Play Mode, loads the real Game scene, raycasts the visible first card, dispatches
its click, checks the active figure/placement, and checks cancellation. Run this
in Unity's EditMode Test Runner; it is excluded from standalone .NET because it
needs Unity's player lifecycle and EventSystem. It has not run in the cloud
without an activated Unity license.

Regression cause: the previous layout moved the offer slots underneath the
transparent `SwipeZoneFigure` Image, which was still a UI raycast target.
The raw gesture handler does not need those Images to receive EventSystem hits.
Both gesture Images now have raycastTarget disabled in the scene and at startup.
