// BallTrailFeedback
// Trigger: continuous while the ball moves (reads the ball's Visual child position each frame; never moves it).
// Purpose: a short, faint trail of tiny bubbles behind the pearl. Bubbles are emitted by distance travelled,
// come from the shared FxSpritePool and sort below the ball so it stays readable. The relaunch teleport after
// a lost ball is detected and skipped, so no bubble line is drawn across the screen.
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
    [Tooltip("World distance the ball travels between two trail bubbles. Bigger = sparser trail.")]
    [SerializeField, Min(0.05f)] private float spacing = 0.3f;
    [Tooltip("Random bubble lifetime in seconds (min, max). Short keeps the trail subtle.")]
    [SerializeField] private Vector2 lifetime = new Vector2(0.3f, 0.45f);
    [Tooltip("Random bubble diameter in world units (min, max). The ball is 0.5.")]
    [SerializeField] private Vector2 size = new Vector2(0.06f, 0.11f);
    [Tooltip("Trail colour; alpha is the starting opacity.")]
    [SerializeField] private Color color = new Color(0.8f, 0.95f, 1f, 0.35f);
    [Tooltip("Random sideways offset from the ball path in world units.")]
    [SerializeField] private float jitter = 0.06f;
    [Tooltip("Upward drift of trail bubbles in units/s².")]
    [SerializeField] private float buoyancy = 0.8f;
    [Tooltip("A jump larger than this in one frame counts as a relaunch and is not trailed.")]
    [SerializeField] private float teleportDistance = 1.5f;
    [Tooltip("Sorting order (below the ball's 10).")]
    [SerializeField] private int sortingOrder = 9;

    private Vector2 lastPos;
    private float distanceSinceEmit;

    private void OnEnable()
    {
        if (target != null) lastPos = target.position;
        distanceSinceEmit = 0f;
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
        while (distanceSinceEmit >= spacing && safety-- > 0)
        {
            distanceSinceEmit -= spacing;
            // Place the bubble back along this frame's path where the spacing boundary was crossed.
            Vector2 at = dist > 0.0001f ? pos - delta / dist * distanceSinceEmit : pos;
            Emit(at);
        }
        if (distanceSinceEmit > spacing) distanceSinceEmit = 0f;

        lastPos = pos;
    }

    private void Emit(Vector2 at)
    {
        float s = Random.Range(size.x, size.y);
        pool.Emit(new FxParticle
        {
            Sprite = bubbleSprite,
            Position = at + Random.insideUnitCircle * jitter,
            Velocity = Vector2.zero,
            Drag = 1f,
            Buoyancy = buoyancy,
            Lifetime = Random.Range(lifetime.x, lifetime.y),
            StartSize = s,
            EndSize = s * 0.6f,
            Aspect = 1f,
            Color = color,
            WobbleAmplitude = 0.02f,
            WobbleFrequency = 4f,
            SortingOrder = sortingOrder
        });
    }
}
