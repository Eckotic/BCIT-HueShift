using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Lays the hand out in a fan (arc), centred on the screen, and lifts the card under the mouse.
// New cards fly in from the draw pile. Replaces the HandArea's HorizontalLayoutGroup at runtime.
public class HandLayout : MonoBehaviour
{
    [Tooltip("Gap between card centres when the hand is small.")]
    public float cardSpacing = 190f;
    [Tooltip("The fan never gets wider than this (cards overlap more instead).")]
    public float maxHandWidth = 1100f;
    [Tooltip("Tilt in degrees per card away from the centre.")]
    public float anglePerCard = 4f;
    [Tooltip("How far outer cards drop, per card away from the centre (squared).")]
    public float arcDrop = 14f;
    public float hoverLift = 120f;
    public float hoverScale = 1.18f;
    [Tooltip("How far the neighbours slide away from the hovered card.")]
    public float neighbourPush = 55f;
    public float followSpeed = 14f;
    [Tooltip("Height offset for the whole hand.")]
    public float baseY = 10f;

    [HideInInspector] public RectTransform spawnFrom;

    private RectTransform area;
    private readonly List<RectTransform> cards = new List<RectTransform>();

    void Awake()
    {
        area = (RectTransform)transform;
        HorizontalLayoutGroup group = GetComponent<HorizontalLayoutGroup>();
        if (group != null)
        {
            group.enabled = false;
        }
    }

    void LateUpdate()
    {
        SyncCards();
        int n = cards.Count;
        if (n == 0)
        {
            return;
        }

        float spacing = n > 1 ? Mathf.Min(cardSpacing, maxHandWidth / (n - 1)) : 0f;
        int hovered = FindHovered(n, spacing);
        float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        for (int i = 0; i < n; i++)
        {
            RectTransform card = cards[i];
            float offset = i - (n - 1) / 2f;

            Vector2 targetPos = new Vector2(offset * spacing, baseY - offset * offset * arcDrop);
            float targetAngle = -offset * anglePerCard;
            float targetScale = 1f;

            if (hovered >= 0)
            {
                if (i == hovered)
                {
                    targetPos = new Vector2(offset * spacing, baseY + hoverLift);
                    targetAngle = 0f;
                    targetScale = hoverScale;
                }
                else
                {
                    int distance = i - hovered;
                    targetPos.x += Mathf.Sign(distance) * neighbourPush / Mathf.Abs(distance);
                }
            }

            card.anchoredPosition = Vector2.Lerp(card.anchoredPosition, targetPos, k);
            card.localRotation = Quaternion.Slerp(card.localRotation, Quaternion.Euler(0f, 0f, targetAngle), k);
            card.localScale = Vector3.Lerp(card.localScale, Vector3.one * targetScale, k);
            card.SetSiblingIndex(i);
        }

        if (hovered >= 0)
        {
            cards[hovered].SetAsLastSibling(); // drawn on top, and gets the click
        }
    }

    // Keeps the list in step with the HandArea's children (cards get added and destroyed by DeckManager).
    private void SyncCards()
    {
        cards.RemoveAll(card => card == null || card.parent != transform);
        foreach (Transform child in transform)
        {
            RectTransform rect = child as RectTransform;
            if (rect == null || cards.Contains(rect))
            {
                continue;
            }
            cards.Add(rect);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            if (spawnFrom != null)
            {
                rect.position = spawnFrom.position; // fly in from the draw pile
            }
            rect.localScale = Vector3.one * 0.35f;
            rect.localRotation = Quaternion.Euler(0f, 0f, 25f);
        }
    }

    // The hovered card is picked from the mouse position against the cards' resting slots,
    // so lifting a card never makes it flicker off and on.
    private int FindHovered(int n, float spacing)
    {
        Vector2 mouse;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            return -1;
        }
        mouse = Mouse.current.position.ReadValue();
#else
        mouse = Input.mousePosition;
#endif
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, mouse, null, out Vector2 local))
        {
            return -1;
        }

        float cardWidth = cards[0].rect.width;
        float cardHeight = cards[0].rect.height;
        float lowest = baseY + hoverLift - cardHeight * hoverScale / 2f + 10f;
        float highest = baseY + hoverLift + cardHeight * hoverScale / 2f;
        if (local.y < lowest || local.y > highest)
        {
            return -1;
        }

        float slot = spacing > 0f ? spacing : cardWidth;
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) / 2f) * spacing;
            float distance = Mathf.Abs(local.x - x);
            float reach = (i == 0 || i == n - 1) ? Mathf.Max(slot / 2f, cardWidth / 2f) : slot / 2f;
            if (distance <= reach && distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }
        return best;
    }
}
