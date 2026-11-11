// BallTrailFeedback
// Trigger: continuous while the ball moves (reads the ball's Visual child position each frame; never moves it).
// Purpose: a short trail of real-looking bubbles behind the pearl. Bubbles are dropped at irregular distances,
// with varied sizes and a little sideways scatter (so it never reads as a dotted line), then shrink, rise slightly
// and fade, so they get smaller and more transparent the further they are from the ball. Pooled via FxSpritePool,
// sorted below the ball so it stays readable. The relaunch teleport after a lost ball is skipped.
using UnityEngine;

public class BallTrailFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ball's Visual child. Only its position is read.")]
    [SerializeField] private Transform target;
    [Tooltip("Shared sprite particle pool.")]
    [SerializeField] private FxSpritePool pool;
    [Tooltip("Bubble sprite for the trail.")]
    [SerializeField] private Sprite bubbleSprite;

    [Header("Trail")]
    [Tooltip("Turn the trail on or off.")]
    [SerializeField] private bool trailEnabled = true;
    [Tooltip("Random distance (world units) the ball travels between two bubbles (min, max). Irregular = natural.")]
    [SerializeField] private Vector2 spacing = new Vector2(0.18f, 0.32f);
    [Tooltip("Random bubble lifetime in seconds (min, max). Short keeps the trail close to the ball.")]
    [SerializeField] private Vector2 lifetime = new Vector2(0.25f, 0.4f);
    [Tooltip("Random starting bubble diameter in world units (min, max). The ball is 0.5.")]
    [SerializeField] private Vector2 size = new Vector2(0.07f, 0.13f);
    [Tooltip("Size at the end of a bubble's life as a fraction of its start size.")]
    [SerializeField, Range(0.1f, 1f)] private float endSizeFactor = 0.45f;
    [Tooltip("Trail colour; alpha is the starting opacity.")]
    [SerializeField] private Color color = new Color(0.85f, 0.97f, 1f, 0.5f);
    [Tooltip("Random sideways scatter from the ball path in world units.")]
    [SerializeField] private float jitter = 0.09f;
    [Tooltip("Upward drift of trail bubbles in units/s².")]
    [SerializeField] private float buoyancy = 1.2f;
    [Tooltip("A jump larger than this in one frame counts as a relaunch and is not trailed.")]
    [SerializeField] private float teleportDistance = 1.5f;
    [Tooltip("Sorting order (below the ball's 10).")]
    [SerializeField] private int sortingOrder = 9;

    private Vector2 lastPos;
    private float distanceSinceEmit;
    private float nextSpacing;

    private void OnEnable()
    {
        if (target != null) lastPos = target.position;
        distanceSinceEmit = 0f;
        nextSpacing = Random.Range(spacing.x, spacing.y);
    }

    private void LateUpdate()
    {
        if (!trailEnabled || target == null || pool == null) return;

        Vector2 pos = target.position;
        Vector2 delta = pos - lastPos;
        float dist = delta.magnitude;

        if (dist > teleportDistance)
        {
            lastPos = pos;
            distanceSinceEmit = 0f;
            return;
        }

        distanceSinceEmit += dist;
        int safety = 4;
        while (distanceSinceEmit >= nextSpacing && safety-- > 0)
        {
            distanceSinceEmit -= nextSpacing;
            // Place the bubble back along this frame's path where the spacing boundary was crossed.
            Vector2 at = dist > 0.0001f ? pos - delta / dist * distanceSinceEmit : pos;
            Emit(at);
            nextSpacing = Random.Range(spacing.x, spacing.y);
        }
        if (distanceSinceEmit > spacing.y) distanceSinceEmit = 0f;

        lastPos = pos;
    }

    private void Emit(Vector2 at)
    {
        float s = Random.Range(size.x, size.y);
        Color c = color;
        c.a *= Random.Range(0.7f, 1f);
        pool.Emit(new FxParticle
        {
            Sprite = bubbleSprite,
            Position = at + Random.insideUnitCircle * jitter,
            Velocity = Random.insideUnitCircle * 0.2f,
            Drag = 2f,
            Buoyancy = buoyancy,
            Lifetime = Random.Range(lifetime.x, lifetime.y),
            StartSize = s,
            EndSize = s * endSizeFactor,
            Aspect = 1f,
            Color = c,
            WobbleAmplitude = 0.025f,
            WobbleFrequency = 3.5f,
            SortingOrder = sortingOrder
        });
    }
}
