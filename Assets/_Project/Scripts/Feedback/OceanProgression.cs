// OceanProgression
// Trigger: GameEvents.BrickDestroyed (counts cleared bricks); animates every frame with unscaled time.
// Purpose: subtle visual progression. As bricks are cleared the ocean background slowly brightens and a few
// seaweed strands and light rays fade in and sway gently. Visual only: never touches gameplay, and all values
// stay low-contrast so nothing competes with the ball.
using UnityEngine;

public class OceanProgression : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ocean background sprite.")]
    [SerializeField] private SpriteRenderer background;
    [Tooltip("Parent of the bricks (the LevelBuilder). Used once to count the starting bricks.")]
    [SerializeField] private Transform brickParent;
    [Tooltip("Seaweed sprites (pivot at the bottom).")]
    [SerializeField] private SpriteRenderer[] seaweed;
    [Tooltip("Light-ray sprites (pivot at the top).")]
    [SerializeField] private SpriteRenderer[] lightRays;

    [Header("Background")]
    [Tooltip("Background tint at the start (darker).")]
    [SerializeField] private Color startTint = new Color(0.78f, 0.78f, 0.82f, 1f);
    [Tooltip("Background tint when every brick is cleared (brighter).")]
    [SerializeField] private Color clearedTint = Color.white;
    [Tooltip("How fast the visuals catch up with progress (per second).")]
    [SerializeField, Min(0.1f)] private float smoothing = 1.5f;

    [Header("Seaweed")]
    [Tooltip("Seaweed opacity at the start.")]
    [SerializeField, Range(0f, 1f)] private float seaweedStartAlpha = 0.08f;
    [Tooltip("Seaweed opacity when every brick is cleared.")]
    [SerializeField, Range(0f, 1f)] private float seaweedMaxAlpha = 0.35f;
    [Tooltip("Seaweed sway in degrees.")]
    [SerializeField, Range(0f, 20f)] private float swayAngle = 6f;
    [Tooltip("Seaweed sway speed in Hz.")]
    [SerializeField] private float swaySpeed = 0.35f;

    [Header("Light rays")]
    [Tooltip("Light-ray opacity when every brick is cleared (they start invisible).")]
    [SerializeField, Range(0f, 1f)] private float rayMaxAlpha = 0.16f;
    [Tooltip("Light-ray shimmer speed in Hz.")]
    [SerializeField] private float rayShimmerSpeed = 0.25f;

    private int totalBricks;
    private int destroyedBricks;
    private float shownProgress;
    private Quaternion[] seaweedBaseRot;
    private Color[] seaweedColor;
    private Color[] rayColor;

    private void Awake()
    {
        seaweed ??= new SpriteRenderer[0];
        lightRays ??= new SpriteRenderer[0];

        seaweedBaseRot = new Quaternion[seaweed.Length];
        seaweedColor = new Color[seaweed.Length];
        for (int i = 0; i < seaweed.Length; i++)
        {
            if (seaweed[i] == null) continue;
            seaweedBaseRot[i] = seaweed[i].transform.localRotation;
            seaweedColor[i] = seaweed[i].color;
        }

        rayColor = new Color[lightRays.Length];
        for (int i = 0; i < lightRays.Length; i++)
            if (lightRays[i] != null) rayColor[i] = lightRays[i].color;

        Apply(0f, 0f);
    }

    private void OnEnable() => GameEvents.BrickDestroyed += OnBrickDestroyed;
    private void OnDisable() => GameEvents.BrickDestroyed -= OnBrickDestroyed;

    private void OnBrickDestroyed(ImpactInfo _) => destroyedBricks++;

    private void Update()
    {
        // LevelBuilder spawns bricks in its Start, so count lazily on the first frames.
        if (totalBricks == 0 && brickParent != null)
            totalBricks = brickParent.GetComponentsInChildren<Brick>().Length;

        float progress = totalBricks > 0 ? Mathf.Clamp01((float)destroyedBricks / totalBricks) : 0f;
        float dt = Time.unscaledDeltaTime;
        shownProgress = Mathf.Lerp(shownProgress, progress, 1f - Mathf.Exp(-smoothing * dt));

        Apply(shownProgress, Time.unscaledTime);
    }

    private void Apply(float p, float time)
    {
        if (background != null) background.color = Color.Lerp(startTint, clearedTint, p);

        float weedAlpha = Mathf.Lerp(seaweedStartAlpha, seaweedMaxAlpha, p);
        for (int i = 0; i < seaweed.Length; i++)
        {
            SpriteRenderer sr = seaweed[i];
            if (sr == null) continue;
            float angle = swayAngle * Mathf.Sin(time * swaySpeed * Mathf.PI * 2f + i * 1.7f);
            sr.transform.localRotation = seaweedBaseRot[i] * Quaternion.Euler(0f, 0f, angle);
            Color c = seaweedColor[i];
            c.a = weedAlpha;
            sr.color = c;
        }

        for (int i = 0; i < lightRays.Length; i++)
        {
            SpriteRenderer sr = lightRays[i];
            if (sr == null) continue;
            float shimmer = 0.75f + 0.25f * Mathf.Sin(time * rayShimmerSpeed * Mathf.PI * 2f + i * 2.3f);
            Color c = rayColor[i];
            c.a = rayMaxAlpha * p * shimmer;
            sr.color = c;
        }
    }
}
