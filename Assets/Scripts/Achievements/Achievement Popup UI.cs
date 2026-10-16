using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Shows Steam-style achievement popups bottom-right. Queues multiple.
/// Put in gameplay scenes (or a persistent canvas). Subscribes to Achievements.OnUnlocked.
///
/// Setup: a panel anchored bottom-right, with an icon Image, name TMP_Text, desc TMP_Text.
/// The panel starts inactive; this script slides it in, holds, slides out.
/// </summary>
public class AchievementPopupUI : MonoBehaviour
{
    [Header("Popup UI")]
    public RectTransform panel;
    public Image    iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;

    [Header("Timing")]
    public float slideTime  = 0.35f;
    public float holdTime   = 3f;
    public float hiddenX    = 400f;   // off-screen X offset
    public float shownX     = 0f;     // resting X offset

    private readonly Queue<AchievementDefinition> queue = new Queue<AchievementDefinition>();
    private bool showing = false;

    private void OnEnable()  => Achievements.OnUnlocked += Enqueue;

    private void OnDisable()
    {
        Achievements.OnUnlocked -= Enqueue;
        // Coroutines die with the component — don't stay stuck in "showing"
        StopAllCoroutines();
        showing = false;
        if (panel != null) panel.gameObject.SetActive(false);
    }

    [Header("Placement")]
    [Tooltip("Gap from the bottom-right screen corner (reference pixels).")]
    public Vector2 margin = new Vector2(40f, 40f);

    private void Start()
    {
        if (panel == null) return;

        // Always on top of every other UI: own canvas with the highest sort order
        Canvas c = panel.GetComponent<Canvas>();
        if (c == null) c = panel.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = short.MaxValue;

        // Always bottom-right, sliding in from off-screen right
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 0f);
        shownX = -margin.x;
        hiddenX = panel.rect.width + margin.x;
        panel.anchoredPosition = new Vector2(hiddenX, margin.y);

        panel.gameObject.SetActive(false);
    }

    private void Enqueue(AchievementDefinition a)
    {
        if (a == null) return;
        queue.Enqueue(a);
        if (!showing && isActiveAndEnabled) StartCoroutine(ShowLoop());
    }

    private IEnumerator ShowLoop()
    {
        showing = true;

        while (queue.Count > 0)
        {
            AchievementDefinition a = queue.Dequeue();

            if (nameText != null) nameText.text = a.displayName;
            if (descText != null) descText.text = a.description;
            if (iconImage != null)
            {
                iconImage.sprite  = a.icon;
                iconImage.enabled = a.icon != null;
            }

            if (panel != null)
            {
                panel.gameObject.SetActive(true);

                yield return Slide(hiddenX, shownX, slideTime);
                yield return new WaitForSecondsRealtime(holdTime);
                yield return Slide(shownX, hiddenX, slideTime);

                panel.gameObject.SetActive(false);
            }
        }

        showing = false;
    }

    private IEnumerator Slide(float fromX, float toX, float time)
    {
        float elapsed = 0f;
        Vector2 pos = panel.anchoredPosition;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / time);
            pos.x = Mathf.Lerp(fromX, toX, t);
            panel.anchoredPosition = pos;
            yield return null;
        }

        pos.x = toX;
        panel.anchoredPosition = pos;
    }
}
