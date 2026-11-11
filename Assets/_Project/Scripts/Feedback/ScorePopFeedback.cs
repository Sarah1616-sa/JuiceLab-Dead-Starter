// ScorePopFeedback
// Trigger: GameEvents.ScoreChanged.
// Purpose: the score text does a quick bubbly scale pop (up to ~1.2x, then settles to 1.0x with a small
// overshoot). Only the RectTransform scale is animated: the HUD keeps ownership of the text value, so the
// score shown is always correct. A new score mid-animation restarts from the current scale toward the same
// fixed peak, so rapid scoring can never make the text grow larger and larger.
using System.Collections;
using UnityEngine;

public class ScorePopFeedback : MonoBehaviour
{
    [Tooltip("RectTransform of the HUD score text. Only its scale is changed.")]
    [SerializeField] private RectTransform target;

    [Tooltip("Peak scale of the pop (1.2 = 20 % bigger). The pop never exceeds this.")]
    [SerializeField, Range(1f, 1.5f)] private float peakScale = 1.2f;
    [Tooltip("Seconds to grow to the peak.")]
    [SerializeField, Min(0.001f)] private float riseTime = 0.07f;
    [Tooltip("Seconds to settle back to normal size.")]
    [SerializeField, Min(0.01f)] private float settleTime = 0.32f;
    [Tooltip("How quickly the settle wobble dies out. Higher = smaller overshoot.")]
    [SerializeField, Range(1f, 10f)] private float settleDamping = 4f;
    [Tooltip("How many wobbles happen while settling (1.25 gives one small dip below 1.0).")]
    [SerializeField, Range(0.5f, 3f)] private float settleOscillations = 1.25f;

    private Vector3 baseScale = Vector3.one;
    private bool hasBase;
    private Coroutine routine;

    private void Awake()
    {
        if (target != null)
        {
            baseScale = target.localScale;
            hasBase = true;
        }
    }

    private void OnEnable() => GameEvents.ScoreChanged += OnScoreChanged;

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (hasBase) target.localScale = baseScale;
    }

    private void OnScoreChanged(int _)
    {
        if (!hasBase || !isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Pop());
    }

    private IEnumerator Pop()
    {
        Vector3 from = target.localScale;
        Vector3 peak = baseScale * peakScale;

        float t = 0f;
        while (t < riseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / riseTime);
            k = 1f - (1f - k) * (1f - k);
            target.localScale = Vector3.Lerp(from, peak, k);
            yield return null;
        }

        t = 0f;
        while (t < settleTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / settleTime);
            float wobble = Mathf.Exp(-settleDamping * u) * Mathf.Cos(settleOscillations * Mathf.PI * 2f * u);
            target.localScale = baseScale * (1f + (peakScale - 1f) * wobble);
            yield return null;
        }

        target.localScale = baseScale;
        routine = null;
    }
}
