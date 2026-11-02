using UnityEngine;

/// <summary>
/// Связывает UI-события (свайпы) с ActiveFigureController.
/// ShapePlacer работает независимо — он сам реализует IPointerDown/Up.
/// </summary>
public class GameInputBinder : MonoBehaviour
{
    [SerializeField] private SwipeZoneFigure swipeZoneFigure;
    [SerializeField] private ActiveFigureController figureController;

    private void Awake()
    {
        if (swipeZoneFigure == null)
            swipeZoneFigure = FindObjectOfType<SwipeZoneFigure>();
        if (figureController == null)
            figureController = FindObjectOfType<ActiveFigureController>();
    }

    private void OnEnable()
    {
        if (swipeZoneFigure != null)
            swipeZoneFigure.OnSwipe += HandleSwipe;
    }

    private void OnDisable()
    {
        if (swipeZoneFigure != null)
            swipeZoneFigure.OnSwipe -= HandleSwipe;
    }

    private void HandleSwipe(MoveAxis axis, int direction)
    {
        if (figureController == null) return;
        figureController.Move(axis, direction);
    }
}
