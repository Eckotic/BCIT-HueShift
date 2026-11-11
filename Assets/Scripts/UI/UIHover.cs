using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

// Buttons grow a little and tick when hovered, and click when pressed.
public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public float hoverScale = 1.07f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(hoverScale, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true);
        Sfx.Play(t => t.buttonHover, 0.5f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        transform.localScale = Vector3.one * hoverScale;
        transform.DOPunchScale(Vector3.one * -0.08f, 0.18f, 6, 0.5f).SetUpdate(true);
        Sfx.Play(t => t.buttonPress, 0.8f);
    }

    void OnDestroy()
    {
        transform.DOKill();
    }
}
