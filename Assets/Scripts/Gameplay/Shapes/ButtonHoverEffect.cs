using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Transform target;
    public float hoverScale = 1.1f;

    private Vector3 normalScale;
    private bool hovering = false;

    void Start()
    {
        normalScale = target.localScale;
    }

    void Update()
    {
        if (hovering)
            target.localScale = Vector3.Lerp(target.localScale, normalScale * hoverScale, Time.deltaTime * 10f);
        else
            target.localScale = Vector3.Lerp(target.localScale, normalScale, Time.deltaTime * 10f);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;
    public void OnPointerExit(PointerEventData eventData) => hovering = false;
}