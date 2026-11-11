// PaddleSquashFeedback
// Trigger: GameEvents.BallHitPaddle.
// Purpose: the paddle's VISUAL child does a quick squash (wider and shorter) and then springs back to exactly
// its original scale, plus an expanding, fading water ripple ring at the contact point.
// The original scale is stored once at startup; every hit restarts from the current scale and always ends by
// assigning the stored scale, so back-to-back hits can never cause permanent stretching or drift.
// The paddle root, its collider and Rigidbody are never touched.
using System.Collections;
using UnityEngine;

public class PaddleSquashFeedback : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The paddle's VISUAL child (never the root that holds the collider).")]
    [SerializeField] private Transform visual;

    [Header("Squash")]
    [Tooltip("Scale multiplier at full squash (x = wider, y = shorter).")]
    [SerializeField] private Vector2 squashScale = new Vector2(1.2f, 0.7f);
    [Tooltip("Seconds to reach full squash.")]
    [SerializeField, Min(0.001f)] private float squashTime = 0.05f;
    [Tooltip("Seconds to spring back to the original scale.")]
    [SerializeField, Min(0.01f)] private float recoverTime = 0.3f;
    [Tooltip("How many wobbles the spring makes while recovering.")]
    [SerializeField, Range(0f, 4f)] private float springOscillations = 1.5f;
    [Tooltip("How quickly the spring wobble dies out. Higher = less overshoot.")]
    [SerializeField, Range(1f, 12f)] private float springDamping = 6f;

    [Header("Ripple")]
    [Tooltip("Shared sprite particle pool.")]
    [SerializeField] private FxSpritePool pool;
    [Tooltip("Ring sprite for the ripple.")]
    [SerializeField] private Sprite rippleSprite;
    [Tooltip("Ripple lifetime in seconds.")]
    [SerializeField] private float rippleLifetime = 0.5f;
    [Tooltip("Ripple diameter at start and end, in world units.")]
    [SerializeField] private Vector2 rippleSize = new Vector2(0.3f, 1.6f);
    [Tooltip("Ripple height multiplier (below 1 = flat water ring).")]
    [SerializeField, Range(0.2f, 1f)] private float rippleAspect = 0.4f;
    [Tooltip("Ripple colour; alpha is the starting opacity.")]
    [SerializeField] private Color rippleColor = new Color(0.75f, 0.95f, 1f, 0.6f);
    [Tooltip("Also spawn a smaller, slower inner ring.")]
    [SerializeField] private bool innerRing = true;
    [Tooltip("Ripple sorting order (above the paddle, below the ball).")]
    [SerializeField] private int rippleSortingOrder = 6;

    private Vector3 originalScale;
    private bool hasOriginal;
    private Coroutine routine;

    private void Awake()
    {
        if (visual != null)
        {
            originalScale = visual.localScale;
            hasOriginal = true;
        }
    }

    private void OnEnable() => GameEvents.BallHitPaddle += OnBallHitPaddle;

    private void OnDisable()
    {
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (hasOriginal) visual.localScale = originalScale;
    }

    private void OnBallHitPaddle(ImpactInfo info)
    {
        if (hasOriginal)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Squash());
        }

        if (pool != null)
        {
            pool.Emit(new FxParticle
            {
                Sprite = rippleSprite,
                Position = info.Point,
                Lifetime = rippleLifetime,
                StartSize = rippleSize.x,
                EndSize = rippleSize.y,
                Aspect = rippleAspect,
                Color = rippleColor,
                SortingOrder = rippleSortingOrder
            });

            if (innerRing)
            {
                Color inner = rippleColor;
                inner.a *= 0.6f;
                pool.Emit(new FxParticle
                {
                    Sprite = rippleSprite,
                    Position = info.Point,
                    Lifetime = rippleLifetime * 1.3f,
                    StartSize = rippleSize.x * 0.6f,
                    EndSize = rippleSize.y * 0.65f,
                    Aspect = rippleAspect,
                    Color = inner,
                    SortingOrder = rippleSortingOrder
                });
            }
        }
    }

    private IEnumerator Squash()
    {
        Vector3 from = visual.localScale;   // restart cleanly from wherever we are
        Vector3 squashed = Vector3.Scale(originalScale, new Vector3(squashScale.x, squashScale.y, 1f));

        float t = 0f;
        while (t < squashTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / squashTime);
            k = 1f - (1f - k) * (1f - k);   // ease-out
            visual.localScale = Vector3.LerpUnclamped(from, squashed, k);
            yield return null;
        }

        t = 0f;
        Vector3 delta = squashed - originalScale;
        while (t < recoverTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / recoverTime);
            float spring = Mathf.Exp(-springDamping * u) * Mathf.Cos(springOscillations * Mathf.PI * 2f * u);
            visual.localScale = originalScale + delta * spring;
            yield return null;
        }

        visual.localScale = originalScale;  // always land exactly on the stored scale
        routine = null;
    }
}
