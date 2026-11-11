// BrickBurstFeedback
// Trigger: GameEvents.BrickDestroyed.
// Purpose: a short underwater burst where the brick broke: bubbles that float up, a few splash droplets that
// arc and fall, and a soft splash ring, all tinted with the brick's colour. Lifetime is about 0.4-0.7 s and
// every sprite comes from the shared FxSpritePool, so rapid hits recycle instead of piling up.
// A few small glass shards spin out and fade quickly. The creature that lived inside the capsule
// (BrickVisualAfter) is released through CreatureDrifter and drifts away behind the playfield. Visual only.
using UnityEngine;

public class BrickBurstFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Shared sprite particle pool.")]
    [SerializeField] private FxSpritePool pool;
    [Tooltip("Sprite used for the bubbles.")]
    [SerializeField] private Sprite bubbleSprite;
    [Tooltip("Sprite used for the splash droplets.")]
    [SerializeField] private Sprite dropletSprite;
    [Tooltip("Sprite used for the splash ring.")]
    [SerializeField] private Sprite ringSprite;
    [Tooltip("Sprite used for the glass shards.")]
    [SerializeField] private Sprite shardSprite;
    [Tooltip("Optional creature spawner. Leave empty for no creatures.")]
    [SerializeField] private CreatureDrifter creatures;

    [Header("Spawn area")]
    [Tooltip("Half size of the area (around the brick centre) that bubbles start in, in world units.")]
    [SerializeField] private Vector2 spawnHalfExtents = new Vector2(0.85f, 0.25f);

    [Header("Bubbles")]
    [Tooltip("Random bubble count per brick (min, max inclusive).")]
    [SerializeField] private Vector2Int bubbleCount = new Vector2Int(10, 14);
    [Tooltip("Random bubble lifetime in seconds (min, max).")]
    [SerializeField] private Vector2 bubbleLifetime = new Vector2(0.45f, 0.7f);
    [Tooltip("Random bubble diameter in world units (min, max).")]
    [SerializeField] private Vector2 bubbleSize = new Vector2(0.12f, 0.26f);
    [Tooltip("How much a bubble grows over its life (end size = start size x this).")]
    [SerializeField, Range(1f, 2f)] private float bubbleGrowth = 1.15f;
    [Tooltip("Random initial bubble speed in units/second (min, max).")]
    [SerializeField] private Vector2 bubbleSpeed = new Vector2(0.8f, 2.2f);
    [Tooltip("Upward acceleration of bubbles in units/s².")]
    [SerializeField] private float bubbleBuoyancy = 3f;
    [Tooltip("Bubble velocity damping per second.")]
    [SerializeField] private float bubbleDrag = 2.5f;
    [Tooltip("0 = white bubbles, 1 = bubbles fully in the brick's colour.")]
    [SerializeField, Range(0f, 1f)] private float bubbleTint = 0.35f;
    [Tooltip("Starting bubble opacity.")]
    [SerializeField, Range(0f, 1f)] private float bubbleAlpha = 0.9f;

    [Header("Splash droplets")]
    [Tooltip("Random droplet count per brick (min, max inclusive).")]
    [SerializeField] private Vector2Int dropletCount = new Vector2Int(4, 6);
    [Tooltip("Random droplet lifetime in seconds (min, max).")]
    [SerializeField] private Vector2 dropletLifetime = new Vector2(0.35f, 0.5f);
    [Tooltip("Random droplet diameter in world units (min, max).")]
    [SerializeField] private Vector2 dropletSize = new Vector2(0.09f, 0.14f);
    [Tooltip("Random droplet launch speed in units/second (min, max).")]
    [SerializeField] private Vector2 dropletSpeed = new Vector2(2.5f, 4.5f);
    [Tooltip("Droplet gravity in units/s² (negative falls).")]
    [SerializeField] private float dropletGravity = -12f;
    [Tooltip("Starting droplet opacity.")]
    [SerializeField, Range(0f, 1f)] private float dropletAlpha = 0.85f;

    [Header("Splash ring")]
    [Tooltip("Show a soft expanding ring at the brick.")]
    [SerializeField] private bool showRing = true;
    [Tooltip("Ring lifetime in seconds.")]
    [SerializeField] private float ringLifetime = 0.45f;
    [Tooltip("Ring diameter at start and end, in world units.")]
    [SerializeField] private Vector2 ringSize = new Vector2(0.5f, 1.9f);
    [Tooltip("Ring height multiplier (below 1 = flattened).")]
    [SerializeField, Range(0.2f, 1f)] private float ringAspect = 0.55f;
    [Tooltip("Starting ring opacity.")]
    [SerializeField, Range(0f, 1f)] private float ringAlpha = 0.5f;

    [Header("Glass shards")]
    [Tooltip("Random shard count per brick (min, max inclusive). 0 = no shards.")]
    [SerializeField] private Vector2Int shardCount = new Vector2Int(5, 7);
    [Tooltip("Random shard lifetime in seconds (min, max). Keep short so shards vanish quickly.")]
    [SerializeField] private Vector2 shardLifetime = new Vector2(0.2f, 0.35f);
    [Tooltip("Random shard size in world units (min, max).")]
    [SerializeField] private Vector2 shardSize = new Vector2(0.1f, 0.18f);
    [Tooltip("Random shard launch speed in units/second (min, max).")]
    [SerializeField] private Vector2 shardSpeed = new Vector2(2.5f, 4.5f);
    [Tooltip("Random shard spin in degrees/second (applied in either direction).")]
    [SerializeField] private Vector2 shardSpin = new Vector2(180f, 540f);
    [Tooltip("Shard gravity in units/s² (negative falls).")]
    [SerializeField] private float shardGravity = -6f;
    [Tooltip("How much of the brick colour the shards take (0 = white glass).")]
    [SerializeField, Range(0f, 1f)] private float shardTint = 0.4f;
    [Tooltip("Starting shard opacity.")]
    [SerializeField, Range(0f, 1f)] private float shardAlpha = 0.85f;

    [Header("Sorting & creatures")]
    [Tooltip("Sorting order of the burst. Keep below the ball (10) so the ball stays readable.")]
    [SerializeField] private int sortingOrder = 8;
    [Tooltip("Chance (0-1) that a destroyed brick releases its creature (CreatureDrifter still caps how many are alive).")]
    [SerializeField, Range(0f, 1f)] private float creatureChance = 1f;

    private void OnEnable() => GameEvents.BrickDestroyed += OnBrickDestroyed;
    private void OnDisable() => GameEvents.BrickDestroyed -= OnBrickDestroyed;

    private void OnBrickDestroyed(ImpactInfo info)
    {
        if (pool == null) return;

        Vector2 center = info.Center;
        Color brick = info.Color;
        brick.a = 1f;

        // Bubbles: float up and wobble.
        Color bubbleColor = Color.Lerp(Color.white, brick, bubbleTint);
        bubbleColor.a = bubbleAlpha;
        int bubbles = Random.Range(bubbleCount.x, bubbleCount.y + 1);
        for (int i = 0; i < bubbles; i++)
        {
            Vector2 offset = new Vector2(
                Random.Range(-spawnHalfExtents.x, spawnHalfExtents.x),
                Random.Range(-spawnHalfExtents.y, spawnHalfExtents.y));
            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 1f)).normalized;
            float size = Random.Range(bubbleSize.x, bubbleSize.y);

            pool.Emit(new FxParticle
            {
                Sprite = bubbleSprite,
                Position = center + offset,
                Velocity = dir * Random.Range(bubbleSpeed.x, bubbleSpeed.y),
                Drag = bubbleDrag,
                Buoyancy = bubbleBuoyancy,
                Lifetime = Random.Range(bubbleLifetime.x, bubbleLifetime.y),
                StartSize = size,
                EndSize = size * bubbleGrowth,
                Aspect = 1f,
                Color = bubbleColor,
                WobbleAmplitude = 0.05f,
                WobbleFrequency = 3f,
                SortingOrder = sortingOrder
            });
        }

        // Droplets: a small upward fan that falls back.
        Color dropColor = Color.Lerp(brick, Color.white, 0.3f);
        dropColor.a = dropletAlpha;
        int drops = Random.Range(dropletCount.x, dropletCount.y + 1);
        for (int i = 0; i < drops; i++)
        {
            float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float size = Random.Range(dropletSize.x, dropletSize.y);

            pool.Emit(new FxParticle
            {
                Sprite = dropletSprite,
                Position = center + new Vector2(Random.Range(-spawnHalfExtents.x, spawnHalfExtents.x) * 0.5f, 0f),
                Velocity = dir * Random.Range(dropletSpeed.x, dropletSpeed.y),
                Drag = 1f,
                Buoyancy = dropletGravity,
                Lifetime = Random.Range(dropletLifetime.x, dropletLifetime.y),
                StartSize = size,
                EndSize = size * 0.5f,
                Aspect = 1f,
                Color = dropColor,
                SortingOrder = sortingOrder
            });
        }

        // Splash ring.
        if (showRing)
        {
            Color ringColor = Color.Lerp(Color.white, brick, 0.25f);
            ringColor.a = ringAlpha;
            pool.Emit(new FxParticle
            {
                Sprite = ringSprite,
                Position = center,
                Lifetime = ringLifetime,
                StartSize = ringSize.x,
                EndSize = ringSize.y,
                Aspect = ringAspect,
                Color = ringColor,
                SortingOrder = sortingOrder - 1
            });
        }

        // Glass shards: quick, spinning, gone in about a quarter second.
        if (shardSprite != null)
        {
            Color shardColor = Color.Lerp(Color.white, brick, shardTint);
            shardColor.a = shardAlpha;
            int shards = Random.Range(shardCount.x, shardCount.y + 1);
            for (int i = 0; i < shards; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f + 0.3f);
                float size = Random.Range(shardSize.x, shardSize.y);
                float spin = Random.Range(shardSpin.x, shardSpin.y) * (Random.value < 0.5f ? -1f : 1f);

                pool.Emit(new FxParticle
                {
                    Sprite = shardSprite,
                    Position = center + new Vector2(
                        Random.Range(-spawnHalfExtents.x, spawnHalfExtents.x),
                        Random.Range(-spawnHalfExtents.y, spawnHalfExtents.y)),
                    Velocity = dir.normalized * Random.Range(shardSpeed.x, shardSpeed.y),
                    Drag = 2f,
                    Buoyancy = shardGravity,
                    Lifetime = Random.Range(shardLifetime.x, shardLifetime.y),
                    StartSize = size,
                    EndSize = size * 0.6f,
                    Aspect = 1f,
                    Color = shardColor,
                    Rotation = Random.Range(0f, 360f),
                    AngularVelocity = spin,
                    SortingOrder = sortingOrder
                });
            }
        }

        // Release the creature that was inside this capsule (same type, same tint).
        if (creatures != null && Random.value < creatureChance)
        {
            if (BrickVisualAfter.TryGetAt(center, out BrickVisualAfter v))
                creatures.TrySpawn(center, v.CreatureIndex, v.CreatureColor, v.CreatureFlipped);
            else
                creatures.TrySpawn(center);
        }
    }
}
