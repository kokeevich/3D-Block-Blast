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

94 cases cover occupancy, non-origin placement, boundaries, overlap rejection
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
2. Before selection, swipe to turn the cube by 90 degrees around an axis. On a phone, pinch
   to zoom; in the Editor, use the mouse wheel over the game gesture regions.
   Releasing one finger after pinch must not start rotation or placement.
3. Choose an offer. Its card illustration and target ghost should have the same shape
   and orientation. Drag horizontally/vertically from the current viewpoint:
   the ghost should move in the two axes of the closest face; the cube should
   remain still. Test after viewing the cube from every side.
4. Use the depth buttons 1–8 (near to far anchor layers). A thick shape
   can extend outside the board at an edge layer: it must turn red and disable
   placement. Move across occupied blocks: red ghost cells must remain visible.
5. Use both rotation buttons to change the offer's orientation. The card illustration
   should update too. Find a valid position: the ghost turns green and
   `Поставить` enables. Releasing a drag or tapping the gesture region must
   never place a figure; only the confirmation button places it.
6. Cancel a selection. No blocks or points should be added, and the same offer
   must remain available. Select another offer and verify normal placement.
7. While a spawn/line-clear animation runs, try selecting another offer,
   dragging, rotating and confirming again: the input must remain locked.
8. Use all three offers: a fresh set must contain a playable concrete offer
   whenever any catalog shape eligible at the current difficulty can fit in any of its 24 rotations. For remaining
   offers, Game Over must only occur when all of their allowed rotations fail;
   catalog shapes that were not offered do not postpone Game Over.

Core gameplay component GUIDs are retained. The legacy `SwipeZoneFigure`,
`GameInputBinder`, GridManager and old UI instances were removed from Game.
Gesture interpretation is centralized in FieldRotator and uses the runtime
HUD's gesture area.

### Offer click regression

`OfferInteractionTests.VisibleOfferReceivesClickAndCreatesAnActiveFigure` enters
Play Mode, loads the real Game scene, raycasts the visible first card, dispatches
its click, checks cancellation, switches through all three offers, then confirms
placement and verifies score/consumption of only the selected offer. Run this
in Unity's EditMode Test Runner; it is excluded from standalone .NET because it
needs Unity's player lifecycle and EventSystem. It has not run in the cloud
without an activated Unity license.

Regression cause: the previous layout moved the offer slots underneath the
transparent `SwipeZoneFigure` Image, which was still a UI raycast target.
The raw gesture handler does not need those Images to receive EventSystem hits.
The gesture Images were first made non-raycastable and have since been removed
along with the retired Canvas; input now uses only the runtime HUD.


### Updated game screen

The old wallpaper Canvas, 3D offer containers and imported field model have
been removed from Game. ShapePlacer is an independent gameplay root, and the
generator always produces three offers without depending on old preview slots. A new safe-area HUD shows score,
record, three large cards and contextual placement actions. Decorative graphics
have raycastTarget=false; only actual buttons receive hits. New cards call the
central GameManager.TrySelectOffer path, with selection changes permitted before
confirmation. Unplaceable offers are muted and labelled «Нет места».

Check the new screen in portrait and landscape: the offer/control panel moves
to the right in landscape. Card art and labels must not block the whole-card hit
area. The active card receives an accent outline. Verify color continuity from
card to placed blocks, score gain feedback, and the new Restart overlay at Game
Over. Browsing hides placement-only actions instead of showing a disabled row.
The board now displays 12 edges, a quiet floor guide and the active layer grid.
Try all eight layers and both rotation buttons after turning the cube around both axes.

Standalone model tests: 94 passed. Runtime and Unity integration-test sources
compile against the actual Unity 2022.3.62f3 assemblies (including its matching
UGUI and test-runner references). Scene/UI interaction tests remain unrun in the
cloud because the editor has no activated license. Compilation does not verify
raycast results, visual appearance, shader rendering or touch behavior.


### Friendly theme and thumbnail silhouette regression

The HUD now uses a light cream/mint background, white surfaces, dark readable
text and teal/lilac/apricot offers. Cube edges, active-layer grid and ghost
colors are tuned for the light background as well.

`ShapeThumbnailTests` checks a complete centered single-cube silhouette with
isometric width/height ratio, and verifies padding/centering for a tall/wide
shape through all 24 rotations. The renderer now draws the visible +X/+Z side
faces (the previous rear faces omitted the lower front corner), sorts by all
three coordinates, and caps enlargement of single-cell figures. These tests
compile against Unity's actual assemblies but remain unrun without an Editor
license. Check tall and wide thumbnails on the phone for clipping, face order,
consistent scale, label separation and selected-card contrast.


### Axial rotation and progressive balance

`BalanceAndTurnTests` covers swipe thresholds/directions, quarter-turn inverses,
four-turn identity, stage boundaries, distinct offers, geometry connectivity,
all cube rotations, deterministic seeded generation, and Game Over with isolated
free cells. It also checks that the late-game fallback cannot reintroduce retired
tiny pieces. All 94 standalone cases pass.

Additional Unity tests in `OfferInteractionTests` check a 90-degree turn and
selection lock during its animation, plus the real Game Over event/overlay path
on a fragmented board. They compile with the editor/test-runner APIs but have
not run in the cloud without a license. Verify horizontal/vertical swipe signs,
short-tap rejection, repeated turns returning to the same pose, pinch/mouse-wheel
zoom, and placement after each snapped orientation in Unity. Rotations occur on
release; dragging an active figure still moves its ghost instead of the board.

The exact gameplay catalog and balance simulations are documented in
`../Balance/README.md`. Test random and careful human play before treating the
sampled agent move counts as a target session length.


### Manual placement and run statistics

Selection now preserves the displayed orientation. `PlacementFrame.StartAnchor`
centers the shape on the nearest geometrically fitting layer using only its
bounds; it never reads occupied cells. A blocked start stays red, confirmation
is disabled, and a player must drag, change depth or rotate. Other legal positions
still count when deciding availability and Game Over.

`ManualPlacementTests` covers all six faces and all catalog rotations, blocked
starts with legal moves elsewhere, and statistics including unique cleared cells
in the occupancy peak. Statistics count successful placements only. Final results
show moves, cleared lines and the maximum occupied percentage *before* clearing.

The added Unity selection test occupies one initial target cell, cancels and
reselects the offer, and checks that position/orientation are unchanged, placement
is disabled, Game Over is false, and no move was counted. Unity-specific tests
compile against the actual editor assemblies but remain unrun without a license.
Try the same sequence in Game, manually move to a free position, then confirm.
Check the new result statistics after both a placement-only and a clearing move.

`Tools/Balance/paired-results.txt` records a controlled comparison on identical
offer tapes; the limitations and distinction between manual input required and
Game Over are documented in `Tools/Balance/README.md`.


### Step 7: board visibility

`BlockVisibilityTests` checks every face/sign, all eight selected layers and all
eight occupied-cell depths. Only layers on the camera side of the selection
fade in normal mode; XRay highlights the selected layer and shows other layers
with faint faces/edges. With no selection, XRay applies uniformly; cancel plus
XRay off restores solid rendering. All 94 standalone model tests pass.

`OfferInteractionTests.XRayAndLayerFadeRestoreMaterialsColorsAndOccupancy` loads
Game, creates blocks in front/on/behind the selected layer, clicks the actual
XRay button, verifies rendering states, disables/re-enables the component,
cancels selection, and checks original materials, tint, shadows and occupancy.
It checks cleanup through ClearCell and ClearPlane too. The test compiles against
Unity's actual UGUI/editor APIs but has not executed without an Editor license.
C# compilation does not validate shader compilation or rendered appearance.

Implementation: `BoardVisibility` is registered by GameManager on Field3D.
Cell-change notifications maintain a renderer cache. Three shared translucent
materials and property blocks avoid per-frame material instances. Normal mode
uses the original materials; no cells or renderers are removed to simulate
visibility. Scene unload/disable restores original state and disposes shared
materials. Block colors are stored in property blocks instead of cloned materials.
Transparent and guide shaders live in Resources so Android builds retain them.

#### Visual acceptance on a phone

1. Partially fill the center of the cube, then select a shape and switch depth
   through all eight levels. Blocks between camera and selection should become
   faint; the selected layer and red/green target cells should remain legible.
2. Cancel: ordinary solids/colors/shadows must return. Quarter-turn the field
   and repeat for the opposite sides and top/bottom.
3. Tap `Рентген: выкл`: the label switches to `Рентген: вкл`. Interior occupied
   cells must show through the front cells. Select a shape: its depth layer gets
   stronger contours than the other layers. Change depth, move and rotate it.
4. Cancel with XRay on: uniform XRay should remain. Switch it off: original
   solids return. None of these actions may change score, offers or occupancy.
5. Place and clear lines in both modes. Pop/spawn scaling should still work;
   removed cells must not leave translucent duplicates or retain display state.
6. Check portrait/landscape and safe areas: the header toggle, record, score and
   placement controls must remain separate and clickable. Restart a party:
   XRay starts off. Evaluate dense central blocks at the phone's actual size.

Shaders use the built-in render pipeline, transparent blending and shader model
3.0 derivatives for anti-aliased cube outlines. GPU rendering/performance and
screen-size readability remain device checks, not validated cloud outcomes.


### Authored Game scene cleanup

Before Play, Game now contains only camera, light, EventSystem, field and game
controllers; its background already matches the muted game palette. Board meshes
and the current HUD are generated at runtime, so the pre-Play Game view is a
clean background rather than the retired wallpaper and FBX grid.

The field was detached from the removed imported model while preserving world
position (2, 2.5, -1.75) and uniform world scale 0.5. Gameplay references are wired
explicitly in the scene. ShapePlacer no longer lives in a UI region; the selector
uses three logical offers, and controls no longer hide arbitrary scene Canvases.
MainMenu and shared resource assets are not deleted by this scene cleanup.

Local serialized fileID references in the saved scene were checked for dangling
links. All 94 standalone model tests pass and both runtime/test sources compile.
`GameSceneCleanupTests` checks the saved scene hierarchy, field scale, component
links and lack of legacy UI/model in Unity. Open MainMenu (or another scene)
before running this asset test: it explicitly skips if Game is already open so
it will not close or replace the user's potentially unsaved work. The test loads
Game additively and closes only the scene it opened. It remains unrun in the cloud
without a Unity license; the existing Play Mode tests still cover bootstrapping,
card interaction, placement, XRay and Game Over on the actual Game scene.


### Step 8: menu, session controls, preferences and onboarding

MainMenu is rebuilt using the same GameTheme colors, rounded surfaces, font and
ShapeThumbnailGraphic as the game. The authored scene is minimal, matching Game's
runtime-HUD approach. Both MainMenu and Game are enabled in Build Settings.
ScreenWidgets/SettingsView share the menu/modal controls and safe-area layout.

SessionFlow guards pause, Game Over and duplicate scene transitions. GameManager
owns Time.timeScale and listener pause, restores them before scene changes, and
pauses on background/focus loss in builds. Actor manipulation, selection,
confirmation and field rotation respect this input lock; a held gesture cannot
become a move on resume. Spawn/clear animations freeze, while menus remain usable.
A pending committed placement contributes to the saved best score if the player
leaves before its animation finishes, without awarding its points twice on resume.

GamePreferences retains the existing best-score key and saves sound, volume and
tutorial completion. Sound/volume apply in both scenes and survive restart. UIAudio
provides a quiet procedural interface click, including paused menu interaction;
full gameplay sound effects remain in step 9. Volume drags update live and flush
on settings close/background/exit, avoiding a disk save on every slider tick.

TutorialProgress follows actual board-turn completion, selection, planar movement,
depth change and successful placement. It introduces no occupancy-aware spawn or
automatic solution. First play shows the guide; intro or pause/help can skip it.
MainMenu's Learn button explicitly replays it even after completion. Cancellation
keeps the current learned step and asks for a new selection if necessary.

SessionAndTutorialTests brings the standalone suite to 94 passed cases. New
SessionInterfaceTests compile against real Unity APIs and cover:

- main menu → persisted settings → game → pause/resume → exit confirmation → menu;
- pausing a committed placement, frozen animation, record persistence and no double
  scoring, then Game Over → restart → Game Over → menu;
- actual five-step onboarding, saved completion and forced replay/skip from menu.

Unity interface tests remain unexecuted in the cloud without an Editor license.
PreferenceTestScope uses editor SessionState across Play Mode domain reloads and
restores the player's settings/record *after* scene destruction, so tests do not
leave their preference values behind. Existing gameplay tests explicitly bypass
the guide only for that run, without recording tutorial completion.

#### Acceptance in Unity and on a phone

1. Start MainMenu. Check the new art, score record and buttons in portrait and
   landscape, including the phone's safe areas. Open settings: mute, adjust volume,
   return, start Game, and verify the choices still apply. Reopen the application
   to verify disk persistence. The old best score should remain intact.
2. On first play, begin the guide. Cards should unlock after a completed field
   turn; confirmation should unlock after movement and a depth change. Complete
   placement, then play normally. Restart: the guide should not appear again.
   Use Learn in MainMenu to replay it. Check intro skip and pause/help skip.
3. During rotation, placement and clearing, open Pause. Everything on the board
   should freeze. Try dragging/selecting/confirming: no gameplay action should
   occur. Settings and sound controls should still work. Resume and verify the
   same selection and animation continue without an extra move.
4. Pause → Restart/Menu must ask for confirmation. Cancel must keep the same
   party paused. Confirm must restore normal time and keep the best result. Verify
   the best result also persists when leaving during a placement animation.
5. Background the phone and return: the pause screen should be waiting. Test the
   Android back/Escape paths through settings/help/confirmation/pause/menu.
6. Finish a party, then test both Restart and Menu on Game Over. Check score,
   combo message, lines/moves/peak statistics and fresh-party state.

The Play Mode scenarios, audible click/volume behavior, mobile lifecycle and
visual layout require device/editor acceptance; compilation alone is not a pass.

## Сохранение партии

- Поставить несколько фигур, повернуть поле, изменить масштаб, выбрать/повернуть следующую фигуру и сдвинуть её. Свернуть и закрыть приложение. После повторного запуска нажать «Продолжить»: поле, счёт, цвета, оставшиеся карточки, выбранная фигура и её позиция должны восстановиться.
- Выйти через паузу в главное меню и продолжить. Партия сохраняется; подтверждение выхода больше не обещает её завершение.
- Закрыть приложение сразу после подтверждения хода с очисткой линии, пока проигрывается анимация. Продолжить: линия уже очищена, очки начислены один раз, использованная фигура недоступна.
- Повторить выход на последней фигуре тройки: при продолжении появляется одна новая тройка, стадия сложности и число ходов не сбрасываются.
- В меню выбрать «Новая игра» → «Отмена»: сохранение остаётся. Подтвердить новую игру либо перезапустить через паузу: пустое поле, нулевой счёт, рекорд сохранён.
- Проиграть и перезапустить приложение: продолжение проигранной партии недоступно.
- При наличии сохранённой партии пройти/пропустить повторное обучение из настроек и вернуться в меню: основная партия доступна через «Продолжить».

Автоматические Play Mode-сценарии находятся в `SessionInterfaceTests`, валидация формата и статистики — в `RunSnapshotTests`. Проверки не заменяют пользовательское сохранение: `PreferenceTestScope` восстанавливает его после выхода из Play Mode.
