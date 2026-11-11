// LifeLossFeedback
// Trigger: GameEvents.BallLost and GameEvents.LivesChanged (both are raised together when the ball is lost,
//          so they are merged into one cue per frame).
// Purpose: makes losing a life visible even with audio muted. A dark vignette pulses at the screen edges and
// fades in under 0.5 s. Optionally the lives text flashes a soft red-to-blue tint and wobbles (in After.unity the
// text is hidden and LivesShellsFeedback does the shell flash instead, so that slot is left empty).
// Only colour, rotation and scale of UI elements change; the HUD still owns the lives text value.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LifeLossFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional: the HUD lives text (any UI Graphic). Leave empty when shells replace the text.")]
    [SerializeField] private Graphic livesText;
    [Tooltip("Full-screen vignette Image. Its Raycast Target must be off.")]
    [SerializeField] private Graphic vignette;

    [Header("Lives text")]
    [Tooltip("First flash colour.")]
    [SerializeField] private Color flashRed = new Color(1f, 0.45f, 0.5f, 1f);
    [Tooltip("Second flash colour before returning to normal.")]
    [SerializeField] private Color flashBlue = new Color(0.5f, 0.75f, 1f, 1f);
    [Tooltip("Total seconds for the tint and wobble.")]
    [SerializeField, Min(0.05f)] private float textDuration = 0.7f;
    [Tooltip("Maximum wobble rotation in degrees.")]
    [SerializeField, Range(0f, 30f)] private float wobbleAngle = 12f;
    [Tooltip("Number of wobbles during the animation.")]
    [SerializeField, Range(0.5f, 6f)] private float wobbleOscillations = 3f;
    [Tooltip("Scale bump of the lives text at the middle of the animation.")]
    [SerializeField, Range(1f, 1.4f)] private float textPopScale = 1.15f;

    [Header("Vignette")]
    [Tooltip("Vignette colour (alpha is ignored; see Peak Alpha).")]
    [SerializeField] private Color vignetteColor = new Color(0.01f, 0.03f, 0.10f, 1f);
    [Tooltip("Vignette opacity at its strongest.")]
    [SerializeField, Range(0f, 1f)] private float vignettePeakAlpha = 0.6f;
    [Tooltip("Seconds to fade the vignette in.")]
    [SerializeField, Min(0.01f)] private float vignetteFadeIn = 0.07f;
    [Tooltip("Seconds to fade the vignette out. Fade in + out stays under 0.5 s.")]
    [SerializeField, Min(0.01f)] private float vignetteFadeOut = 0.35f;

    private RectTransform textRect;
    private Color textColor;
    private Quaternion textRotation;
    private Vector3 textScale;
    private bool hasText;

    private Coroutine textRoutine;
    private Coroutine vignetteRoutine;
    private int lastTriggerFrame = -1;

    private void Awake()
    {
        if (livesText != null)
        {
            textRect = livesText.rectTransform;
            textColor = livesText.color;
            textRotation = textRect.localRotation;
            textScale = textRect.localScale;
            hasText = true;
        }
        SetVignetteAlpha(0f);
    }

    private void OnEnable()
    {
        GameEvents.BallLost += OnBallLost;
        GameEvents.LivesChanged += OnLivesChanged;
    }

    private void OnDisable()
    {
        GameEvents.BallLost -= OnBallLost;
        GameEvents.LivesChanged -= OnLivesChanged;
        if (textRoutine != null) StopCoroutine(textRoutine);
        if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
        textRoutine = vignetteRoutine = null;
        RestoreText();
        SetVignetteAlpha(0f);
    }

    private void OnBallLost() => Trigger();
    private void OnLivesChanged(int _) => Trigger();

    private void Trigger()
    {
        if (!isActiveAndEnabled) return;
        if (Time.frameCount == lastTriggerFrame) return;   // BallLost + LivesChanged arrive together
        lastTriggerFrame = Time.frameCount;

        if (hasText)
        {
            if (textRoutine != null) StopCoroutine(textRoutine);
            textRoutine = StartCoroutine(AnimateText());
        }
        if (vignette != null)
        {
            if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
            vignetteRoutine = StartCoroutine(AnimateVignette());
        }
    }

    private IEnumerator AnimateText()
    {
        float t = 0f;
        while (t < textDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / textDuration);

            Color c;
            if (u < 0.3f) c = Color.Lerp(textColor, flashRed, u / 0.3f);
            else if (u < 0.6f) c = Color.Lerp(flashRed, flashBlue, (u - 0.3f) / 0.3f);
            else c = Color.Lerp(flashBlue, textColor, (u - 0.6f) / 0.4f);
            livesText.color = c;

            float angle = wobbleAngle * (1f - u) * (1f - u) * Mathf.Sin(wobbleOscillations * Mathf.PI * 2f * u);
            textRect.localRotation = textRotation * Quaternion.Euler(0f, 0f, angle);
            textRect.localScale = textScale * (1f + (textPopScale - 1f) * Mathf.Sin(Mathf.PI * u));
            yield return null;
        }

        RestoreText();
        textRoutine = null;
    }

    private IEnumerator AnimateVignette()
    {
        float start = vignette.color.a;
        float t = 0f;
        while (t < vignetteFadeIn)
        {
            t += Time.unscaledDeltaTime;
            SetVignetteAlpha(Mathf.Lerp(start, vignettePeakAlpha, Mathf.Clamp01(t / vignetteFadeIn)));
            yield return null;
        }

        t = 0f;
        while (t < vignetteFadeOut)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / vignetteFadeOut);
            SetVignetteAlpha(vignettePeakAlpha * (1f - u) * (1f - u));
            yield return null;
        }

        SetVignetteAlpha(0f);
        vignetteRoutine = null;
    }

    private void RestoreText()
    {
        // The lives text is on the Canvas (another root) and may already be destroyed when Play mode stops.
        if (!hasText || livesText == null) return;
        livesText.color = textColor;
        textRect.localRotation = textRotation;
        textRect.localScale = textScale;
    }

    private void SetVignetteAlpha(float a)
    {
        if (vignette == null) return;
        Color c = vignetteColor;
        c.a = a;
        vignette.color = c;
    }
}
