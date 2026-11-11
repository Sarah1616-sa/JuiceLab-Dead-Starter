// BrickVisualAfter
// Trigger: Start (once per brick, after LevelBuilder has named and coloured it); gentle idle bob every frame.
// Purpose: lives on the Visual child of Brick_After and turns the brick into a tiny aquarium: a pastel glass
// capsule tinted with the row colour, a small light creature silhouette inside, and a white highlight/bubble
// overlay. The creature is picked from a fixed pattern (row/column), so every Play looks the same.
// BrickBurstFeedback asks this registry which creature was inside, so the released creature matches.
// Purely visual: the brick root, its collider and the Brick script are never touched.
using System.Collections.Generic;
using UnityEngine;

public class BrickVisualAfter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The brick root's SpriteRenderer that LevelBuilder colours (kept disabled in Brick_After).")]
    [SerializeField] private SpriteRenderer sourceRenderer;
    [Tooltip("Tinted glass body of the capsule.")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Creature silhouette inside the capsule.")]
    [SerializeField] private SpriteRenderer creature;
    [Tooltip("White highlight + tiny bubbles drawn over everything.")]
    [SerializeField] private SpriteRenderer highlight;
    [Tooltip("Creature silhouettes. Order must match CreatureDrifter's sprites (Fish, Coral, Seahorse, Jellyfish).")]
    [SerializeField] private Sprite[] creatureSprites;

    [Header("Glass")]
    [Tooltip("How much of the row colour the glass takes (0 = clear white, 1 = full colour). Pastel ≈ 0.55.")]
    [SerializeField, Range(0f, 1f)] private float glassTint = 0.55f;
    [Tooltip("Opacity multiplier of the glass body.")]
    [SerializeField, Range(0f, 1f)] private float glassAlpha = 1f;

    [Header("Creature")]
    [Tooltip("Creature height in world units (the capsule is 0.7 tall).")]
    [SerializeField, Range(0.1f, 0.65f)] private float creatureHeight = 0.42f;
    [Tooltip("How much of the row colour the creature takes (0 = white).")]
    [SerializeField, Range(0f, 1f)] private float creatureTint = 0.15f;
    [Tooltip("Creature opacity.")]
    [SerializeField, Range(0f, 1f)] private float creatureAlpha = 0.95f;
    [Tooltip("Idle bob distance in world units (0.017 ≈ 1.5 px at 1080p). 0 = no bob.")]
    [SerializeField, Range(0f, 0.05f)] private float bobAmplitude = 0.017f;
    [Tooltip("Idle bob speed in cycles per second.")]
    [SerializeField, Range(0f, 2f)] private float bobSpeed = 0.35f;

    private static readonly List<BrickVisualAfter> Active = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();

    /// <summary>Index into the creature sprite list (Fish, Coral, Seahorse, Jellyfish).</summary>
    public int CreatureIndex { get; private set; }
    /// <summary>Colour the creature was drawn with.</summary>
    public Color CreatureColor { get; private set; } = Color.white;
    /// <summary>Whether the creature sprite is mirrored.</summary>
    public bool CreatureFlipped { get; private set; }

    private float bobPhase;
    private float bobLocalScale = 1f;
    private bool ready;

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Start()
    {
        Color row = sourceRenderer != null ? sourceRenderer.color : Color.white;
        row.a = 1f;

        if (body != null)
        {
            Color g = Color.Lerp(Color.white, row, glassTint);
            g.a = glassAlpha;
            body.color = g;
        }

        int r = 0, c = 0;
        bool parsed = TryParseGrid(transform.parent != null ? transform.parent.name : name, out r, out c);
        int count = creatureSprites != null ? creatureSprites.Length : 0;
        if (count > 0)
        {
            int key = parsed
                ? r * 3 + c
                : Mathf.Abs(Mathf.RoundToInt(transform.position.x * 7f) + Mathf.RoundToInt(transform.position.y * 13f));
            CreatureIndex = key % count;
        }
        CreatureFlipped = parsed ? (c % 2 == 1) : false;
        bobPhase = (r * 7 + c) * 0.9f;

        if (creature != null && count > 0)
        {
            creature.sprite = creatureSprites[CreatureIndex];
            creature.flipX = CreatureFlipped;
            Color cc = Color.Lerp(Color.white, row, creatureTint);
            cc.a = creatureAlpha;
            creature.color = cc;
            CreatureColor = cc;
            FitCreature();
        }
        ready = true;
    }

    private void FitCreature()
    {
        // Undo the brick's non-uniform scale so the creature keeps its true proportions.
        Vector3 parentScale = creature.transform.parent.lossyScale;
        Vector2 spriteSize = creature.sprite.bounds.size;
        if (spriteSize.y <= 0f || Mathf.Approximately(parentScale.x, 0f) || Mathf.Approximately(parentScale.y, 0f))
            return;
        float k = creatureHeight / spriteSize.y;
        creature.transform.localScale = new Vector3(k / parentScale.x, k / parentScale.y, 1f);
        creature.transform.localPosition = Vector3.zero;
        bobLocalScale = 1f / parentScale.y;
    }

    private void Update()
    {
        if (!ready || creature == null || bobAmplitude <= 0f) return;
        float y = bobAmplitude * bobLocalScale *
                  Mathf.Sin(Time.unscaledTime * bobSpeed * Mathf.PI * 2f + bobPhase);
        creature.transform.localPosition = new Vector3(0f, y, 0f);
    }

    /// <summary>Finds the brick visual whose brick sits at <paramref name="center"/>.</summary>
    public static bool TryGetAt(Vector2 center, out BrickVisualAfter found)
    {
        found = null;
        float best = 0.05f * 0.05f;
        foreach (BrickVisualAfter v in Active)
        {
            if (v == null || !v.ready) continue;
            Vector2 p = v.transform.parent != null ? v.transform.parent.position : v.transform.position;
            float d = (p - center).sqrMagnitude;
            if (d <= best) { best = d; found = v; }
        }
        return found != null;
    }

    private static bool TryParseGrid(string n, out int row, out int col)
    {
        row = col = 0;
        // LevelBuilder names bricks "Brick_r{row}_c{col}".
        int ri = n.IndexOf("_r", System.StringComparison.Ordinal);
        int ci = n.IndexOf("_c", System.StringComparison.Ordinal);
        if (ri < 0 || ci < 0 || ci <= ri) return false;
        return int.TryParse(n.Substring(ri + 2, ci - ri - 2), out row) &&
               int.TryParse(n.Substring(ci + 2), out col);
    }
}
