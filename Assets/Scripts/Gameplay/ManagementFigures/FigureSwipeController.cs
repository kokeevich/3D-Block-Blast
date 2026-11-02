using UnityEngine;
using UnityEngine.EventSystems;

public class FigureSwipeController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
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

        // 1. Вертикаль → Y
        if (absY > absX * 1.5f)
        {
            axis = MoveAxis.Y;
            direction = delta.y > 0 ? 1 : -1; // вверх = +1
        }
        else
        {
            // 2. Диагонали
            if (delta.x * delta.y < 0)
            {
                axis = MoveAxis.X;
                // 🔥 Инвертируем направление ПО ГОРИЗОНТАЛИ
                direction = delta.x > 0 ? -1 : 1; // ← именно так!
            }
            else
            {
                axis = MoveAxis.Z;
                direction = delta.x > 0 ? 1 : -1; // или по delta.y — как у тебя
            }
        }

        OnSwipe?.Invoke(axis, direction);
    }
}