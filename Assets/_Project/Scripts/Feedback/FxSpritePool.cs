// FxSpritePool
// Trigger: none on its own. BrickBurstFeedback, PaddleSquashFeedback and BallTrailFeedback call Emit().
// Purpose: a single shared, fixed-size pool of SpriteRenderer "particles" (bubbles, droplets, ripple rings)
// animated with unscaled, frame-rate independent time. When the pool is full the oldest particle is recycled,
// so rapid hits can never pile effects up. Decorative only: pooled objects never have colliders.
using UnityEngine;

/// <summary>Description of one pooled sprite particle.</summary>
public struct FxParticle
{
    public Sprite Sprite;
    public Vector2 Position;
    public Vector2 Velocity;
    /// <summary>Velocity damping per second (0 = none).</summary>
    public float Drag;
    /// <summary>Vertical acceleration in units/s². Positive rises like a bubble, negative falls.</summary>
    public float Buoyancy;
    public float Lifetime;
    public float StartSize;
    public float EndSize;
    /// <summary>Height multiplier (1 = round, below 1 = flattened ellipse).</summary>
    public float Aspect;
    /// <summary>Tint. Its alpha is the starting alpha.</summary>
    public Color Color;
    /// <summary>Sideways sine drift amplitude in units.</summary>
    public float WobbleAmplitude;
    /// <summary>Sideways sine drift frequency in Hz.</summary>
    public float WobbleFrequency;
    public int SortingOrder;
}

public class FxSpritePool : MonoBehaviour
{
    [Tooltip("Maximum particles alive at once. When full, the oldest particle is recycled instead of adding more.")]
    [SerializeField, Min(8)] private int capacity = 160;

    [Tooltip("Material for pooled sprites. Leave empty to use the default sprite material.")]
    [SerializeField] private Material spriteMaterial;

    [Tooltip("Fraction of each particle's lifetime spent fading in (0 = appear at full alpha).")]
    [SerializeField, Range(0f, 0.5f)] private float fadeInPortion = 0.08f;

    [Tooltip("Largest time step used per frame, so a hitch never makes particles jump.")]
    [SerializeField, Range(0.02f, 0.2f)] private float maxStep = 0.05f;

    private sealed class Slot
    {
        public Transform T;
        public SpriteRenderer R;
        public FxParticle P;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Age;
        public float Phase;
        public bool Alive;
    }

    private Slot[] slots;
    private int cursor;

    /// <summary>Number of particles currently visible.</summary>
    public int AliveCount { get; private set; }

    private void Awake()
    {
        slots = new Slot[capacity];
        for (int i = 0; i < capacity; i++)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            if (spriteMaterial != null) r.sharedMaterial = spriteMaterial;
            go.SetActive(false);
            slots[i] = new Slot { T = go.transform, R = r };
        }
    }

    private void OnDisable()
    {
        if (slots == null) return;
        foreach (Slot s in slots) Kill(s);
    }

    /// <summary>Shows one particle. Never allocates; recycles the oldest particle when the pool is full.</summary>
    public void Emit(in FxParticle p)
    {
        if (slots == null || p.Sprite == null || p.Lifetime <= 0f) return;

        Slot s = TakeSlot();
        s.P = p;
        s.Pos = p.Position;
        s.Vel = p.Velocity;
        s.Age = 0f;
        s.Phase = Random.value * Mathf.PI * 2f;
        s.R.sprite = p.Sprite;
        s.R.sortingOrder = p.SortingOrder;
        if (!s.Alive) AliveCount++;
        s.Alive = true;
        Apply(s);
        if (!s.T.gameObject.activeSelf) s.T.gameObject.SetActive(true);
    }

    private Slot TakeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int idx = (cursor + i) % slots.Length;
            if (!slots[idx].Alive)
            {
                cursor = (idx + 1) % slots.Length;
                return slots[idx];
            }
        }

        // Pool is full: recycle the particle closest to the end of its life.
        Slot oldest = slots[0];
        float best = -1f;
        foreach (Slot s in slots)
        {
            float t = s.Age / s.P.Lifetime;
            if (t > best) { best = t; oldest = s; }
        }
        return oldest;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, maxStep);
        if (dt <= 0f) return;

        foreach (Slot s in slots)
        {
            if (!s.Alive) continue;

            s.Age += dt;
            if (s.Age >= s.P.Lifetime)
            {
                Kill(s);
                continue;
            }

            s.Vel.y += s.P.Buoyancy * dt;
            s.Vel /= 1f + s.P.Drag * dt;
            s.Pos += s.Vel * dt;
            Apply(s);
        }
    }

    private void Apply(Slot s)
    {
        float t = Mathf.Clamp01(s.Age / s.P.Lifetime);

        // Grow fast then slow down (ease-out), like a bubble expanding.
        float grow = 1f - (1f - t) * (1f - t);
        float size = Mathf.Lerp(s.P.StartSize, s.P.EndSize, grow);
        float aspect = s.P.Aspect > 0f ? s.P.Aspect : 1f;

        float wobble = s.P.WobbleAmplitude *
                       Mathf.Sin(s.Phase + s.Age * s.P.WobbleFrequency * Mathf.PI * 2f);

        s.T.localPosition = new Vector3(s.Pos.x + wobble, s.Pos.y, 0f);
        s.T.localScale = new Vector3(size, size * aspect, 1f);

        float fadeIn = fadeInPortion > 0f ? Mathf.Clamp01(t / fadeInPortion) : 1f;
        float fadeOut = 1f - t * t;
        Color c = s.P.Color;
        c.a *= fadeIn * fadeOut;
        s.R.color = c;
    }

    private void Kill(Slot s)
    {
        if (s.Alive) AliveCount--;
        s.Alive = false;
        if (s.T != null && s.T.gameObject.activeSelf) s.T.gameObject.SetActive(false);
    }
}
