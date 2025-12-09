using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class ScreenFade : MonoBehaviour
{
    public static ScreenFade instance;

    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Awake()
    {
        instance = this;
    }

    public void FadeOut(System.Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, onComplete));
    }

    public void FadeIn(System.Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(0f, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete)
    {
        float startAlpha = fadeImage.color.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
        onComplete?.Invoke();
    }
}
