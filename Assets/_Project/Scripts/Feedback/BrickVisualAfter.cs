// BrickVisualAfter
// Trigger: Start (once per brick, after LevelBuilder has coloured the brick).
// Purpose: lives on the Visual child of Brick_After. Reads the colour LevelBuilder gave the brick's root
// SpriteRenderer (hidden, but still the source of the brick's colour) and tints the glass-capsule water with it,
// so each brick keeps its original colour identity. Purely visual; the root, collider and Brick script are untouched.
using UnityEngine;

public class BrickVisualAfter : MonoBehaviour
{
    [Tooltip("The brick root's SpriteRenderer that LevelBuilder colours (kept disabled in Brick_After).")]
    [SerializeField] private SpriteRenderer sourceRenderer;
    [Tooltip("Coloured water inside the capsule.")]
    [SerializeField] private SpriteRenderer water;
    [Tooltip("Transparent glass shell drawn over the water.")]
    [SerializeField] private SpriteRenderer glass;

    [Tooltip("Opacity of the coloured water.")]
    [SerializeField, Range(0f, 1f)] private float waterAlpha = 0.9f;
    [Tooltip("How much the glass shell picks up the brick colour (0 = clear white glass).")]
    [SerializeField, Range(0f, 1f)] private float glassTint = 0.3f;
    [Tooltip("Opacity of the glass shell.")]
    [SerializeField, Range(0f, 1f)] private float glassAlpha = 0.95f;

    private void Start()
    {
        Color c = sourceRenderer != null ? sourceRenderer.color : Color.white;
        c.a = 1f;

        if (water != null) water.color = new Color(c.r, c.g, c.b, waterAlpha);

        if (glass != null)
        {
            Color g = Color.Lerp(Color.white, c, glassTint);
            g.a = glassAlpha;
            glass.color = g;
        }
    }
}
