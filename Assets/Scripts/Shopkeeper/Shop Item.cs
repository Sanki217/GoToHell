using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public abstract class ShopItem : MonoBehaviour
{
    [Header("Shop Item")]
    public string itemName = "Item";
    public int cost = 50;

    [Header("Label")]
    public float labelHeight = 1.2f;
    public float labelFontSize = 5f;

    private bool playerInRange = false;
    private bool sold = false;

    private GameObject canvasGO;
    private TMP_Text label;

    protected virtual void Start()
    {
        BuildLabel();
    }

    private void BuildLabel()
    {
        canvasGO = new GameObject("ShopLabel");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        float scale = 0.01f;
        canvasGO.transform.localScale = Vector3.one * scale;

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 80f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        label = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = labelFontSize;
        label.color = Color.white;
        label.text = $"{itemName}\n{cost} Souls";

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        canvasGO.SetActive(false);
    }

    private void LateUpdate()
    {
        if (canvasGO == null) return;
        Vector3 pos = transform.position;
        pos.y += labelHeight;
        canvasGO.transform.position = pos;

        Camera cam = Camera.main;
        if (cam != null) canvasGO.transform.rotation = cam.transform.rotation;
    }

    private void Update()
    {
        if (!playerInRange || sold) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            PlayerInventory inv = PlayerRefs.I?.Inventory;
            if (inv == null) return;

            if (!inv.SpendSouls(cost))
            {
                StopAllCoroutines();
                StartCoroutine(CantAffordFlash());
                return;
            }

            sold = true;
            canvasGO?.SetActive(false);
            OnPurchased();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        canvasGO?.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        canvasGO?.SetActive(false);
    }

    private IEnumerator CantAffordFlash()
    {
        if (label == null) yield break;

        Color original = label.color;
        Vector3 originalPos = canvasGO.transform.localPosition;
        float elapsed = 0f;
        float duration = 0.5f;
        float shakeAmount = 0.05f;

        label.color = Color.red;
        label.text = $"{itemName}\nNot enough souls!";

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Sin(elapsed * 80f) * shakeAmount * (1f - elapsed / duration);
            canvasGO.transform.localPosition = originalPos + new Vector3(shake, 0f, 0f);
            label.color = Color.Lerp(Color.red, original, elapsed / duration);
            yield return null;
        }

        canvasGO.transform.localPosition = originalPos;
        label.color = original;
        label.text = $"{itemName}\n{cost} Souls";
    }

    protected abstract void OnPurchased();
}