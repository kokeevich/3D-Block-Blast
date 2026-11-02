using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeZoneFigure : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Inversion")]
    [SerializeField] private bool invertX = true;


    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f;

    private Vector2 startPos;
    private Vector2 endPos;

    public System.Action<MoveAxis, int> OnSwipe;

    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        endPos = eventData.position;

        Vector2 delta = endPos - startPos;

        if (delta.magnitude < minSwipeDistance)
            return;

        InterpretSwipe(delta);
    }

    private void InterpretSwipe(Vector2 delta)
    {
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        MoveAxis axis;
        int direction;

        // Вертикаль → Y
        if (absY > absX * 1.5f)
        {
            axis = MoveAxis.Y;
            direction = delta.y > 0 ? 1 : -1;
        }
        else
        {
            // Диагонали
            if (delta.x * delta.y < 0)
            {
                // ↖ ↘ → X
                axis = MoveAxis.X;

                direction = delta.x > 0 ? -1 : 1;

                if (invertX)
                    direction *= -1;
            }
            else
            {
                // ↗ ↙ → Z
                axis = MoveAxis.Z;
                direction = delta.y > 0 ? 1 : -1;
            }
        }

        OnSwipe?.Invoke(axis, direction);
    }
}
