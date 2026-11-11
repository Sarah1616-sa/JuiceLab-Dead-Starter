// LivesShellsFeedback
// Trigger: GameEvents.LivesChanged (and GameManager.Instance.Lives once in Start for the starting value).
// Purpose: shows one seashell icon per remaining life in the HUD (After scene only; the original lives text is
// hidden there). Every event first snaps the row to the exact lives value, so the shells always match the real
// count, including the reset to full lives after the last life is lost. Then it animates: a lost shell pops and
// fades out, regained shells pop back in one after another, and the row flashes a soft red-to-blue tint and wobbles.
// UI only: no gameplay effect, no raycast targets.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LivesShellsFeedback : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("Seashell sprite.")]
    [SerializeField] private Sprite shellSprite;
    [Tooltip("Shell size in canvas pixels.")]
    [SerializeField] private Vector2 shellSize = new Vector2(54f, 50f);
    [Tooltip("Gap between shells in canvas pixels.")]
    [SerializeField] private float spacing = 12f;
    [Tooltip("Normal shell colour.")]
    [SerializeField] private Color shellColor = Color.white;

    [Header("Life lost / reset")]
    [Tooltip("First flash colour.")]
    [SerializeField] private Color flashRed = new Color(1f, 0.55f, 0.6f, 1f);
    [Tooltip("Second flash colour before returning to normal.")]
    [SerializeField] private Color flashBlue = new Color(0.55f, 0.8f, 1f, 1f);
    [Tooltip("Seconds for the row's tint and wobble.")]
    [SerializeField, Min(0.05f)] private float flashDuration = 0.7f;
    [Tooltip("Maximum wobble rotation of the row in degrees.")]
    [SerializeField, Range(0f, 30f)] private float wobbleAngle = 10f;
    [Tooltip("Number of wobbles during the flash.")]
    [SerializeField, Range(0.5f, 6f)] private float wobbleOscillations = 3f;
    [Tooltip("Seconds for a lost shell to pop and fade out.")]
    [SerializeField, Min(0.05f)] private float loseTime = 0.45f;
    [Tooltip("Scale a lost shell grows to while fading.")]
    [SerializeField, Range(1f, 2f)] private float losePopScale = 1.5f;
    [Tooltip("Seconds for a regained shell to pop in.")]
    [SerializeField, Min(0.05f)] private float gainTime = 0.3f;
    [Tooltip("Delay between regained shells popping in, in seconds.")]
    [SerializeField, Min(0f)] private float gainStagger = 0.08f;

    private readonly List<Image> shells = new();
    private RectTransform row;
    private Quaternion rowRotation;
    private int shown = -1;
    private Coroutine routine;

    private void Awake()
    {
        row = (RectTransform)transform;
        rowRotation = row.localRotation;
    }

    private void OnEnable() => GameEvents.LivesChanged += OnLivesChanged;

    private void OnDisable()
    {
        GameEvents.LivesChanged -= OnLivesChanged;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (shown >= 0) Snap(shown);
        row.localRotation = rowRotation;
    }

    private void Start()
    {
        int lives = GameManager.Instance != null ? GameManager.Instance.Lives : 0;
        Snap(lives);
    }

    private void OnLivesChanged(int lives)
    {
        int previous = shown < 0 ? lives : shown;
        Snap(lives);   // the shell count is correct immediately, whatever the animation does
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Animate(previous, lives));
    }

    /// <summary>Shows exactly <paramref name="lives"/> shells at rest (full size, normal colour).</summary>
    private void Snap(int lives)
    {
        lives = Mathf.Max(0, lives);
        while (shells.Count < lives) shells.Add(CreateShell(shells.Count));
        for (int i = 0; i < shells.Count; i++)
        {
            Image s = shells[i];
            bool on = i < lives;
            if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
            s.rectTransform.localScale = Vector3.one;
            s.color = shellColor;
        }
        shown = lives;
        row.localRotation = rowRotation;
    }

    private Image CreateShell(int index)
    {
        var go = new GameObject($"Shell_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(row, false);
        // Right-aligned row: shell 0 is the leftmost of the full row.
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = shellSize;
        var img = go.GetComponent<Image>();
        img.sprite = shellSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = shellColor;
        return img;
    }

    private void LateUpdate()
    {
        // Keep the row right-aligned for the pool size (a handful of icons, so this is cheap).
        int n = shells.Count;
        for (int i = 0; i < n; i++) PlaceShell(shells[i].rectTransform, i, n);

        // Size the row to its shells and keep its right edge on the anchor, so the wobble pivots at the row's centre.
        float width = n > 0 ? n * shellSize.x + (n - 1) * spacing : 0f;
        row.sizeDelta = new Vector2(width, shellSize.y);
        row.anchoredPosition = new Vector2(-width * 0.5f, 0f);
    }

    private void PlaceShell(RectTransform rt, int index, int total)
    {
        float step = shellSize.x + spacing;
        float x = -(total - 1 - index) * step - shellSize.x * 0.5f;
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    private IEnumerator Animate(int from, int to)
    {
        // A lost shell is re-shown temporarily just to play its pop-out.
        Image lost = null;
        if (to < from && to >= 0 && to < shells.Count)
        {
            lost = shells[to];
            lost.gameObject.SetActive(true);
        }

        // Regained shells start hidden (scale 0) and pop in one by one.
        int gainedFrom = Mathf.Max(0, from);
        for (int i = gainedFrom; i < to; i++) shells[i].rectTransform.localScale = Vector3.zero;

        float total = Mathf.Max(flashDuration, loseTime, gainTime + gainStagger * Mathf.Max(0, to - gainedFrom));
        float t = 0f;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            // Row flash + wobble.
            float u = Mathf.Clamp01(t / flashDuration);
            Color tint;
            if (u < 0.3f) tint = Color.Lerp(shellColor, flashRed, u / 0.3f);
            else if (u < 0.6f) tint = Color.Lerp(flashRed, flashBlue, (u - 0.3f) / 0.3f);
            else tint = Color.Lerp(flashBlue, shellColor, (u - 0.6f) / 0.4f);
            float angle = wobbleAngle * (1f - u) * (1f - u) * Mathf.Sin(wobbleOscillations * Mathf.PI * 2f * u);
            row.localRotation = rowRotation * Quaternion.Euler(0f, 0f, angle);
            for (int i = 0; i < to && i < shells.Count; i++) shells[i].color = tint;

            // Lost shell pops and fades.
            if (lost != null)
            {
                float l = Mathf.Clamp01(t / loseTime);
                lost.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, losePopScale, 1f - (1f - l) * (1f - l));
                Color c = flashRed;
                c.a = 1f - l;
                lost.color = c;
            }

            // Regained shells pop in with a small overshoot.
            for (int i = gainedFrom; i < to && i < shells.Count; i++)
            {
                float g = Mathf.Clamp01((t - (i - gainedFrom) * gainStagger) / gainTime);
                float s = g <= 0f ? 0f : EaseOutBack(g);
                shells[i].rectTransform.localScale = Vector3.one * s;
            }
            yield return null;
        }

        Snap(to);
        routine = null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
