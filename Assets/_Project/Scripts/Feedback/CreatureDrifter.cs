// CreatureDrifter
// Trigger: called by BrickBurstFeedback on GameEvents.BrickDestroyed (by chance).
// Purpose: releases the tiny creature (fish, coral, seahorse or jellyfish) from a broken capsule; it drifts slowly away BEHIND the playfield and fades out.
// Pooled with a hard cap on how many are alive at once. Pure decoration: no colliders, no gameplay effect.
using UnityEngine;

public class CreatureDrifter : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Creature sprites to pick from at random. Fish sprites should face left.")]
    [SerializeField] private Sprite[] creatureSprites;
    [Tooltip("Material for creature sprites. Leave empty for the default sprite material.")]
    [SerializeField] private Material spriteMaterial;

    [Header("Limits")]
    [Tooltip("Hard cap on creatures alive at once. Extra spawn requests are ignored.")]
    [SerializeField, Min(1)] private int maxAlive = 6;

    [Header("Motion")]
    [Tooltip("Random lifetime in seconds (min, max).")]
    [SerializeField] private Vector2 lifetime = new Vector2(3.5f, 5f);
    [Tooltip("Random drift speed in units/second (min, max). Keep it slow and calm.")]
    [SerializeField] private Vector2 driftSpeed = new Vector2(0.35f, 0.7f);
    [Tooltip("Random creature height in world units (min, max).")]
    [SerializeField] private Vector2 size = new Vector2(0.42f, 0.55f);
    [Tooltip("Sideways sway amplitude in world units.")]
    [SerializeField] private float swayAmplitude = 0.15f;
    [Tooltip("Sideways sway frequency in Hz.")]
    [SerializeField] private float swayFrequency = 0.6f;
    [Tooltip("Gentle 'breathing' scale pulse (0 = none).")]
    [SerializeField, Range(0f, 0.3f)] private float pulseAmount = 0.08f;
    [Tooltip("Breathing pulse frequency in Hz.")]
    [SerializeField] private float pulseFrequency = 0.9f;

    [Header("Look")]
    [Tooltip("Creature tint. Its alpha is the maximum opacity; keep it low so creatures stay in the background.")]
    [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.6f);
    [Tooltip("Seconds to fade in after spawning.")]
    [SerializeField] private float fadeInTime = 0.5f;
    [Tooltip("Fraction of the lifetime (at the end) spent fading out.")]
    [SerializeField, Range(0.1f, 0.9f)] private float fadeOutPortion = 0.45f;
    [Tooltip("Sorting order. Must be below the bricks (0) so creatures stay behind the playfield.")]
    [SerializeField] private int sortingOrder = -50;

    private sealed class Creature
    {
        public Transform T;
        public SpriteRenderer R;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Age;
        public float Life;
        public float Size;
        public float Phase;
        public float SpriteHeight = 1f;
        public Color Color = Color.white;
        public bool Alive;
    }

    private Creature[] creatures;

    /// <summary>Number of creatures currently visible.</summary>
    public int AliveCount { get; private set; }

    private void Awake()
    {
        creatures = new Creature[maxAlive];
        for (int i = 0; i < maxAlive; i++)
        {
            var go = new GameObject("Creature");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            if (spriteMaterial != null) r.sharedMaterial = spriteMaterial;
            r.sortingOrder = sortingOrder;
            go.SetActive(false);
            creatures[i] = new Creature { T = go.transform, R = r };
        }
    }

    private void OnDisable()
    {
        if (creatures == null) return;
        foreach (Creature c in creatures) Hide(c);
    }

    /// <summary>Releases a random creature at a world position. Returns false if the cap is reached.</summary>
    public bool TrySpawn(Vector2 position)
    {
        if (creatureSprites == null || creatureSprites.Length == 0) return false;
        return TrySpawn(position, Random.Range(0, creatureSprites.Length), Color.white, Random.value < 0.5f);
    }

    /// <summary>
    /// Releases a specific creature (index into the sprite list) with a colour and facing, so it matches
    /// the creature that was inside the broken capsule. Returns false if the cap is reached.
    /// </summary>
    public bool TrySpawn(Vector2 position, int spriteIndex, Color color, bool facesRight)
    {
        if (creatures == null || creatureSprites == null || creatureSprites.Length == 0) return false;

        Creature c = null;
        foreach (Creature candidate in creatures)
        {
            if (!candidate.Alive) { c = candidate; break; }
        }
        if (c == null) return false;

        // Swim the way the creature faces (sprites face left; flipped = facing right).
        float sideways = Random.Range(0.2f, 1f) * (facesRight ? 1f : -1f);
        Vector2 dir = new Vector2(sideways, Random.Range(0.3f, 1f)).normalized;
        c.Pos = position;
        c.Vel = dir * Random.Range(driftSpeed.x, driftSpeed.y);
        c.Age = 0f;
        c.Life = Random.Range(lifetime.x, lifetime.y);
        c.Size = Random.Range(size.x, size.y);
        c.Phase = Random.value * Mathf.PI * 2f;
        c.Color = color;
        c.R.sprite = creatureSprites[Mathf.Clamp(spriteIndex, 0, creatureSprites.Length - 1)];
        c.SpriteHeight = Mathf.Max(0.01f, c.R.sprite.bounds.size.y);
        c.R.flipX = facesRight;
        c.R.sortingOrder = sortingOrder;
        c.Alive = true;
        AliveCount++;
        Apply(c);
        c.T.gameObject.SetActive(true);
        return true;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        foreach (Creature c in creatures)
        {
            if (!c.Alive) continue;
            c.Age += dt;
            if (c.Age >= c.Life) { Hide(c); continue; }
            c.Pos += c.Vel * dt;
            Apply(c);
        }
    }

    private void Apply(Creature c)
    {
        float sway = swayAmplitude * Mathf.Sin(c.Phase + c.Age * swayFrequency * Mathf.PI * 2f);
        c.T.localPosition = new Vector3(c.Pos.x + sway, c.Pos.y, 0f);

        float pulse = 1f + pulseAmount * Mathf.Sin(c.Phase + c.Age * pulseFrequency * Mathf.PI * 2f);
        float k = c.Size / c.SpriteHeight;   // Size is the creature's height in world units
        c.T.localScale = new Vector3(k, k * pulse, 1f);

        float t = c.Age / c.Life;
        float fadeIn = fadeInTime > 0f ? Mathf.Clamp01(c.Age / fadeInTime) : 1f;
        float fadeOut = Mathf.Clamp01((1f - t) / fadeOutPortion);
        Color col = tint * c.Color;
        col.a = tint.a * fadeIn * fadeOut;
        c.R.color = col;
    }

    private void Hide(Creature c)
    {
        if (c.Alive) AliveCount--;
        c.Alive = false;
        if (c.T != null && c.T.gameObject.activeSelf) c.T.gameObject.SetActive(false);
    }
}
