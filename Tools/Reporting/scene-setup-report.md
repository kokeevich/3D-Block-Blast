# Отчёт по настройке 3D Block Blast

Дата: 2026-08-03 23:38

## Итог

Геймплей 3D Block Blast (размещение фигур, очистка заполненных плоскостей, регенерация, очки, Game Over) полностью реализован на уровне скриптов. Осталось довесить два новых компонента в сцену \Game.unity\ и при необходимости связать UI Game Over.

Unity 2022.3.62f3, скрипты проверены вручную на согласованность API. Batch-компиляция через Unity.exe невозможна без лицензии ULF на этой машине, поэтому финальная сборка проверяется в редакторе.

## Что уже висит на сцене Game.unity (проверено по GUID)

| Скрипт | На сцене? |
|---|---|
| Field3D | Да |
| PlaneClearer | Нет |
| ActiveFigureController | Да |
| FigureGridShape | Нет |
| ShapePlacer | Нет |
| GameInputBinder | Да |
| GridManager | Да |
| BalancedShapeSelector | Да |
| GameManager | Да |
| ShapeSpawnButton | Да |

### Уже привязано
- **GameFieldRoot** содержит **Field3D**  ← состояние поля 8×8×8.
- **GameManager** содержит **GameManager**, **BalancedShapeSelector**.
- **ActiveFigureController**, **GameInputBinder**, **SwipeZoneFigure**, **ShapeSpawnButton** — на своих местах.
- **GridManager** — заглушка для совместимости, в геймплее не участвует, можно удалить из объекта позже.

### Нужно добавить вручную (2 новых компонента)

#### 1. На объект GameFieldRoot добавить PlaneClearer
1. В Hierarchy выдели **GameFieldRoot**.
2. В Inspector нажми **Add Component** → введи **PlaneClearer** → добавь.
3. Параметры: **Field** подставится автоматически (тот же GameObject). Остальные по умолчанию подходят (popDuration 0.25, popScale 1.4).

#### 2. На объект SwipeZoneFigure добавить ShapePlacer
1. В Hierarchy найди **Canvas → SwipeZoneFigure** (Layer = UI).
2. **Add Component** → **ShapePlacer**.
3. Поля:
   - **Figure Controller** → перетащи объект **ActiveFigureController** (или оставь пустым — найдётся сам).
   - **Field** → объект **GameFieldRoot** (там Field3D).
   - **Plane Clearer** → объект **GameFieldRoot** (там PlaneClearer).
   - **Blocks Parent** → можно оставить пустым (блоки станут детьми Field3D). Если хочешь порядок в иерархии, создай пустой дочерний объект у GameFieldRoot (например \PlacedBlocks\) и перетащи его.
   - **Block Prefab** → опционально. Если оставить пустым, блоки создадутся как серые кубы через \CreatePrimitive\. Для красоты укажи любой кубовый префаб (например один из \Resources/UI/Prefabs/Small/Figure1\ — у него есть MeshRenderer).
   - **Place Tap Time** = 0.25 с, **Place Tap Max Distance** = 25 px — при таких порогах свайпы игнорируются, а короткий тап по зоне свайпа размещает фигуру.

## Что делает GameManager

- При старте: подхватывает **Field3D**, **ActiveFigureController**, **BalancedShapeSelector**, **ShapePlacer** (FindObjectOfType) — ручная привязка не обязательна.
- Автоматически находит все **ShapeSpawnButton** на сцене и сортирует их по \uttonIndex\, присваивает ссылки.
- Счёт: **+1 за блок**, **+10 за плоскость**, **комбо +5\*(cleared-1)**, рекорд в \PlayerPrefs\ (\lockblast.best\).
- **Game Over** срабатывает, когда ни одна из оставшихся (или свежесгенерированных) фигур не помещается на поле.

## UI Game Over (опционально)

Если хочешь экран окончания игры:
1. В Canvas создай **Panel** (назови, например, \GameOverPanel\), растяни на весь экран, поставь **SetActive = false**.
2. Внутрь добавь два **Text** (счёт и рекорд) и **Button** "Restart".
3. На объекте **GameManager** укажи поля:
   - **Score Text** → существующий UI Text для текущего счёта (если есть — создай его тоже),
   - **Best Score Text** → UI Text для рекорда,
   - **Game Over Panel** → твой \GameOverPanel\,
   - **Game Over Score Text** → Text внутри панели,
   - **Restart Button** → кнопка Restart.

## Проверка после настройки

1. Открой проект в Unity Editor → дождись компиляции (без лицензии batchmode не работает — это нормально, в редакторе всё соберётся).
2. Ошибок в Console быть не должно (предупреждение от \GridManager\ допустимо).
3. **Play**: нажми любую кнопку фигуры → фигура заспавнится; свайпай по \SwipeZoneFigure\ для перемещения; короткий **тап** по той же зоне = размещение. Заполненные плоскости (8×8) анимируются и исчезают, очки растут.

## Замечание про Context7

По твоему запросу Context7 MCP пропущен. В MiniMax Code нет штатного UI для MCP, а конфиг \config.yaml\ в текущей версии секцию MCP не подхватывает — если захочешь, разберём отдельной задачей.
