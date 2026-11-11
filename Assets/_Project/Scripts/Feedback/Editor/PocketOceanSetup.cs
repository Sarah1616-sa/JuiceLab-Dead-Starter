// PocketOceanSetup (Editor only)
// Trigger: menu "Tools/Pocket Ocean/Setup After Scene" (keeps existing art files), menu
//          "Tools/Pocket Ocean/Regenerate Procedural Art + Setup" (overwrites art/audio with the procedural versions),
//          or batch mode: Unity -batchmode -quit -projectPath . -executeMethod PocketOceanSetup.RunAll
//          (optional "-pocketPreview <file.png>" renders a 1920x1080 preview of the playfield).
// Purpose: one-click, repeatable setup for the Pocket Ocean theme in Assets/HW3/After.unity.
//   1. Procedurally generates any missing sprites (PNG) / sounds (WAV) and applies import settings. Replacement art
//      with the same file name is kept and fitted automatically (sprites may have any pixel aspect).
//   2. Creates Assets/_Project/Prefabs/Brick_After.prefab (copy of Brick with a glass-capsule visual child).
//   3. Opens After.unity, centres the camera, swaps visual sprites (fitted exactly to the colliders) and adds the
//      feedback objects, HUD extras and background music, then saves.
//   4. Validates: one of each created object (safe to re-run), no colliders on decoration, Brick_After physics and
//      values identical to Brick, every visual exactly the size of its collider, background centred and covering 16:9.
// It never opens Before.unity and never edits Brick.prefab or any gameplay script.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PocketOceanSetup
{
    private const string ArtDir = "Assets/_Project/Art/PocketOcean";
    private const string AudioDir = "Assets/_Project/Audio/PocketOcean";
    private const string BrickPrefabPath = "Assets/_Project/Prefabs/Brick.prefab";
    private const string BrickAfterPath = "Assets/_Project/Prefabs/Brick_After.prefab";
    private const string AfterScenePath = "Assets/HW3/After.unity";
    private const string ScoreMaterialPath = ArtDir + "/PocketOcean_ScoreText.mat";

    private const string RootName = "PocketOcean";
    private const string UiPrefix = "PocketOcean_";
    private const string VignetteName = "PocketOcean_Vignette";
    private const string ShellsName = "PocketOcean_LivesShells";
    private const string WaveLineName = "PocketOcean_WaveLine";
    private const string WallVisualName = "PocketOcean_WallVisual";
    private const string MusicName = "BackgroundMusic";

    // Sorting orders (walls' and bricks' roots are 0).
    private const int OrderBackground = -100;
    private const int OrderLightRays = -90;
    private const int OrderDecor = -60;
    private const int OrderCreatures = -50;
    private const int OrderWallVisual = 1;
    private const int OrderBrickBody = 1;
    private const int OrderBrickCreature = 2;
    private const int OrderBrickHighlight = 3;
    private const int OrderPaddle = 5;
    private const int OrderBall = 10;

    // Creature order shared by Brick_After and CreatureDrifter.
    private static readonly string[] CreatureNames = { "Fish", "Coral", "Seahorse", "Jellyfish" };

    private static bool overwriteAssets;

    [MenuItem("Tools/Pocket Ocean/Setup After Scene")]
    public static void RunAll() => Run(false);

    [MenuItem("Tools/Pocket Ocean/Regenerate Procedural Art + Setup")]
    public static void RegenerateAndRunAll() => Run(true);

    private static void Run(bool overwrite)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        overwriteAssets = overwrite;
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(AudioDir);
        AssetDatabase.Refresh();

        // Assets from the first pass that are no longer used.
        foreach (string old in new[] { ArtDir + "/CoralPaddle.png", ArtDir + "/CapsuleWater.png" })
            if (File.Exists(old)) AssetDatabase.DeleteAsset(old);

        GenerateArt();
        GenerateAudio();
        AssetDatabase.Refresh();

        GameObject brickAfter = BuildBrickAfterPrefab();
        BuildScene(brickAfter);
        Validate(brickAfter);
        AssetDatabase.SaveAssets();

        string preview = GetArg("-pocketPreview");
        if (!string.IsNullOrEmpty(preview))
        {
            try { RenderPreview(preview); }
            catch (Exception e) { Debug.Log($"[PocketOcean] Preview render skipped: {e.Message}"); }
        }

        Debug.Log("[PocketOcean] Setup complete. After.unity updated; Before.unity untouched.");
    }

    // ------------------------------------------------------------------ ART

    private delegate Color Painter(float x, float y);

    private static readonly List<(string path, SpriteAlignment align)> PendingSprites = new();

    private static void GenerateArt()
    {
        PendingSprites.Clear();

        // Ocean background: lighter teal-blue, low contrast, faint caustic light from the surface.
        Png("OceanGradient", 256, 256, (x, y) =>
        {
            Color bottom = new Color(0.07f, 0.22f, 0.33f);
            Color top = new Color(0.24f, 0.52f, 0.62f);
            Color c = Color.Lerp(bottom, top, SmoothStep(0f, 1f, y));
            float caustic = Mathf.Abs(Mathf.Sin(x * 18f + Mathf.Sin(y * 11f) * 1.5f) *
                                      Mathf.Sin(y * 14f + Mathf.Sin(x * 9f) * 1.5f));
            float k = Mathf.Pow(caustic, 3f) * 0.03f * y;
            float glow = Mathf.Exp(-((x - 0.5f) * (x - 0.5f) / 0.1f + (y - 1f) * (y - 1f) / 0.12f)) * 0.04f;
            return new Color(c.r + k + glow, c.g + k * 1.1f + glow, c.b + k * 1.1f + glow, 1f);
        });

        // Pearl: circle = ball collider. White, faint iridescent rim, soft darker edge for contrast, highlight.
        Png("Pearl", 256, 256, (x, y) =>
        {
            Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
            float r = p.magnitude;
            float a = Fill(r - 0.497f, 0.006f);
            float n = Mathf.Clamp01(r / 0.5f);
            Color c = Color.Lerp(Color.white, new Color(0.88f, 0.9f, 0.96f), n * n);
            float ang = Mathf.Atan2(p.y, p.x);
            float hue = Mathf.Repeat(0.55f + 0.35f * Mathf.Sin(ang * 1.5f + n * 4f), 1f);
            Color iris = Color.HSVToRGB(hue, 0.22f, 1f);
            c = Color.Lerp(c, iris, SmoothStep(0.5f, 0.95f, n) * 0.55f);
            c = Color.Lerp(c, new Color(0.55f, 0.6f, 0.75f), SmoothStep(0.88f, 1f, n) * 0.45f);
            float hl = 1f - SmoothStep(0f, 0.16f, (p - new Vector2(-0.15f, 0.17f)).magnitude);
            c = Color.Lerp(c, Color.white, hl);
            float sheen = 1f - SmoothStep(0f, 0.12f, (p - new Vector2(0.16f, -0.17f)).magnitude);
            c = Color.Lerp(c, new Color(0.9f, 1f, 1f), sheen * 0.45f);
            c.a = a;
            return c;
        });

        // Aqua glass paddle: 768x128 = 3 x 0.5 world. Inner glow only, so it never extends past the collider.
        Png("AquaPaddle", 768, 128, (x, y) =>
        {
            Vector2 w = new Vector2((x - 0.5f) * 3f, (y - 0.5f) * 0.5f);
            float d = SdRoundBox(w, new Vector2(1.5f, 0.25f), 0.25f);
            float a = Fill(d + 0.002f, 0.006f);
            float inner = SmoothStep(0f, 1f, Mathf.Clamp01(-d / 0.22f));
            Color c = Color.Lerp(new Color(0.18f, 0.66f, 0.78f), new Color(0.62f, 1f, 0.97f), inner);
            float rim = Mathf.Exp(-Mathf.Pow((d + 0.025f) / 0.015f, 2f));
            c = Color.Lerp(c, new Color(0.85f, 1f, 1f), rim * 0.8f);
            float top = Mathf.Exp(-Mathf.Pow((w.y - 0.13f) / 0.03f, 2f)) * (1f - SmoothStep(1.0f, 1.32f, Mathf.Abs(w.x)));
            c = Color.Lerp(c, Color.white, top * 0.85f);
            float low = Mathf.Exp(-Mathf.Pow((w.y + 0.14f) / 0.035f, 2f)) * (1f - SmoothStep(0.6f, 1.2f, Mathf.Abs(w.x)));
            c = Color.Lerp(c, new Color(0.7f, 1f, 1f), low * 0.35f);
            c.a = a * Mathf.Lerp(0.88f, 1f, rim);
            return c;
        });

        // Glass capsule body: 512x180 ≈ 2 x 0.7 world. White (tinted per row in code); clear-ish body, bright rim.
        Png("GlassCapsule", 512, 180, (x, y) =>
        {
            Vector2 w = new Vector2((x - 0.5f) * 2f, (y - 0.5f) * 0.7f);
            float d = SdRoundBox(w, new Vector2(1f, 0.35f), 0.3f);
            float inside = Fill(d, 0.006f);
            float rim = SmoothStep(-0.06f, -0.012f, d);
            float body = Mathf.Lerp(0.78f, 0.55f, (w.y + 0.35f) / 0.7f);   // denser at the bottom, like tinted water
            float alpha = inside * Mathf.Clamp01(body + rim * 0.5f);
            float v = Mathf.Lerp(0.95f, 1f, rim);
            return new Color(v, v, v, alpha);
        });

        // Glass highlight overlay: bright streak along the top edge, faint bottom reflection and a few tiny bubbles.
        Vector3[] tinyBubbles =
        {
            new Vector3(0.70f, -0.08f, 0.035f), new Vector3(0.80f, 0.06f, 0.022f),
            new Vector3(-0.76f, -0.14f, 0.026f), new Vector3(0.60f, -0.20f, 0.018f), new Vector3(-0.66f, 0.02f, 0.016f)
        };
        Png("GlassHighlight", 512, 180, (x, y) =>
        {
            Vector2 w = new Vector2((x - 0.5f) * 2f, (y - 0.5f) * 0.7f);
            float d = SdRoundBox(w, new Vector2(1f, 0.35f), 0.3f);
            float inside = Fill(d, 0.006f);
            float streak = Mathf.Exp(-Mathf.Pow((w.y - 0.235f) / 0.028f, 2f)) *
                           (1f - SmoothStep(0.6f, 0.85f, Mathf.Abs(w.x + 0.05f)));
            float dot = 1f - SmoothStep(0.015f, 0.04f, (w - new Vector2(-0.82f, 0.18f)).magnitude);
            float low = Mathf.Exp(-Mathf.Pow((w.y + 0.25f) / 0.025f, 2f)) *
                        (1f - SmoothStep(0.35f, 0.7f, Mathf.Abs(w.x - 0.25f))) * 0.3f;
            float bub = 0f;
            foreach (Vector3 b in tinyBubbles)
            {
                float r = (w - new Vector2(b.x, b.y)).magnitude;
                float ring = Mathf.Exp(-Mathf.Pow((r - b.z) / 0.006f, 2f));
                float spec = 1f - SmoothStep(0.002f, 0.008f, (w - new Vector2(b.x - b.z * 0.4f, b.y + b.z * 0.4f)).magnitude);
                bub = Mathf.Max(bub, Mathf.Max(ring * 0.75f, spec));
            }
            float alpha = inside * Mathf.Clamp01(Mathf.Max(Mathf.Max(streak * 0.9f, dot * 0.9f), Mathf.Max(low, bub)));
            return new Color(1f, 1f, 1f, alpha);
        });

        // Creature silhouettes (white/greyscale; tinted in code). Fish faces left.
        Png("Fish", 256, 160, (x, y) =>
        {
            Vector2 q = new Vector2(x, y * 0.625f);
            float ex = (q.x - 0.44f) / 0.27f, ey = (q.y - 0.3125f) / 0.17f;
            float body = Fill((Mathf.Sqrt(ex * ex + ey * ey) - 1f) * 0.17f, 0.006f);
            float tailD = Mathf.Max(Mathf.Max(Mathf.Abs(q.y - 0.3125f) - (q.x - 0.66f) * 0.95f, 0.64f - q.x), q.x - 0.92f);
            float tail = Fill(tailD, 0.006f);
            float fin = Fill(SdTriangle(q, new Vector2(0.34f, 0.44f), new Vector2(0.58f, 0.44f), new Vector2(0.42f, 0.57f)), 0.006f);
            float a = Mathf.Max(body, Mathf.Max(tail, fin));
            float v = Mathf.Lerp(0.84f, 1f, Mathf.Clamp01((q.y - 0.14f) / 0.34f));
            float gill = Mathf.Exp(-Mathf.Pow(((q - new Vector2(0.37f, 0.3125f)).magnitude - 0.11f) / 0.007f, 2f)) *
                         (q.x < 0.37f ? 1f : 0f) * body;
            v = Mathf.Lerp(v, 0.72f, gill * 0.8f);
            float eye = Fill((q - new Vector2(0.27f, 0.34f)).magnitude - 0.026f, 0.005f);
            v = Mathf.Lerp(v, 0.25f, eye);
            return new Color(v, v, v, a);
        });

        Png("Coral", 192, 192, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float d = float.MaxValue;
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.42f)) - 0.07f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.5f, 0.38f), new Vector2(0.3f, 0.62f)) - 0.055f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.3f, 0.62f), new Vector2(0.24f, 0.86f)) - 0.045f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.3f, 0.62f), new Vector2(0.4f, 0.8f)) - 0.04f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.5f, 0.4f), new Vector2(0.7f, 0.6f)) - 0.055f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.7f, 0.6f), new Vector2(0.78f, 0.84f)) - 0.045f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.7f, 0.6f), new Vector2(0.6f, 0.78f)) - 0.04f);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.5f, 0.42f), new Vector2(0.52f, 0.74f)) - 0.045f);
            float a = Fill(d, 0.006f);
            float v = Mathf.Lerp(0.82f, 1f, y) - 0.08f * SmoothStep(-0.03f, 0f, d);
            return new Color(v, v, v, a);
        });

        const int seg = 48;
        var centers = new Vector2[seg];
        var radii = new float[seg];
        for (int i = 0; i < seg; i++)
        {
            float t = i / (seg - 1f);
            centers[i] = new Vector2(0.47f + 0.12f * Mathf.Sin(t * Mathf.PI * 1.15f), 0.78f - 0.6f * t);
            radii[i] = 0.09f * (1f - t) + 0.02f + 0.035f * Mathf.Sin(t * Mathf.PI);
        }
        Png("Seahorse", 192, 192, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float d = float.MaxValue;
            for (int i = 0; i < seg; i++) d = Mathf.Min(d, (p - centers[i]).magnitude - radii[i]);
            d = Mathf.Min(d, (p - new Vector2(0.46f, 0.84f)).magnitude - 0.085f);                      // head
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.46f, 0.84f), new Vector2(0.27f, 0.8f)) - 0.028f); // snout (left)
            d = Mathf.Min(d, Mathf.Abs((p - new Vector2(0.5f, 0.15f)).magnitude - 0.06f) - 0.02f);       // tail curl
            d = Mathf.Min(d, SdTriangle(p, new Vector2(0.6f, 0.5f), new Vector2(0.7f, 0.58f), new Vector2(0.6f, 0.64f))); // fin
            float a = Fill(d, 0.006f);
            float ridges = 0.06f * Mathf.Sin(y * 70f);
            float v = Mathf.Clamp01(0.9f + ridges + 0.1f * y);
            v = Mathf.Lerp(v, 0.25f, Fill((p - new Vector2(0.43f, 0.86f)).magnitude - 0.018f, 0.005f));
            return new Color(v, v, v, a);
        });

        Png("Jellyfish", 192, 192, (x, y) =>
        {
            float ex = (x - 0.5f) / 0.3f, ey = (y - 0.62f) / 0.24f;
            float bell = Fill((Mathf.Sqrt(ex * ex + ey * ey) - 1f) * 0.24f, 0.006f) *
                         Fill(0.52f + 0.015f * Mathf.Sin(x * 60f) - y, 0.006f);
            float tent = 0f;
            for (int k = 0; k < 5; k++)
            {
                float cx = 0.34f + k * 0.08f + 0.025f * Mathf.Sin(y * 22f + k);
                float width = 0.01f + 0.01f * Mathf.Clamp01((y - 0.12f) / 0.4f);
                float tk = Fill(Mathf.Abs(x - cx) - width, 0.006f) * Fill(0.12f - y, 0.008f) * Fill(y - 0.54f, 0.008f);
                tent = Mathf.Max(tent, tk * Mathf.Lerp(0.45f, 0.85f, (y - 0.12f) / 0.42f));
            }
            float v = Mathf.Lerp(0.85f, 1f, 1f - SmoothStep(0f, 0.25f, new Vector2(x - 0.5f, y - 0.68f).magnitude));
            return new Color(v, v, v, Mathf.Max(bell, tent));
        });

        // Bubble: clear centre, bright rim, small highlight.
        Png("Bubble", 64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
            float r = p.magnitude;
            float inside = Fill(r - 0.47f, 0.025f);
            float rim = SmoothStep(0.33f, 0.46f, r);
            float alpha = inside * (0.15f + 0.8f * rim);
            float hl = 1f - SmoothStep(0.04f, 0.1f, (p - new Vector2(-0.15f, 0.15f)).magnitude);
            return new Color(1f, 1f, 1f, Mathf.Max(alpha, hl * inside));
        });

        Png("Droplet", 64, 64, (x, y) =>
        {
            float r = new Vector2(x - 0.5f, y - 0.5f).magnitude;
            return new Color(1f, 1f, 1f, 1f - SmoothStep(0.2f, 0.48f, r));
        });

        // Ripple: thicker soft ring with a faint water disc inside.
        Png("RippleRing", 256, 256, (x, y) =>
        {
            float r = new Vector2(x - 0.5f, y - 0.5f).magnitude;
            float ring = Mathf.Exp(-Mathf.Pow((r - 0.44f) / 0.04f, 2f));
            float disc = (1f - SmoothStep(0.3f, 0.44f, r)) * 0.12f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(ring + disc));
        });

        // Glass shard: small triangle with a bright edge.
        Png("GlassShard", 64, 64, (x, y) =>
        {
            float d = SdTriangle(new Vector2(x, y), new Vector2(0.2f, 0.15f), new Vector2(0.85f, 0.45f), new Vector2(0.35f, 0.9f));
            float a = Fill(d, 0.02f);
            float edge = SmoothStep(-0.06f, -0.01f, d);
            return new Color(1f, 1f, 1f, a * Mathf.Lerp(0.55f, 1f, edge));
        });

        // Seaweed (pivot bottom): three tapering wavy strands. 128x256 = 1 x 2.
        Png("Seaweed", 128, 256, (x, y) =>
        {
            float a = 0f;
            float[] x0 = { 0.3f, 0.5f, 0.7f };
            float[] h = { 0.75f, 0.97f, 0.85f };
            for (int k = 0; k < 3; k++)
            {
                float cx = x0[k] + 0.08f * Mathf.Sin(y * 12f + k * 1.3f) * y;
                float width = Mathf.Lerp(0.07f, 0.015f, Mathf.Clamp01(y / h[k]));
                a = Mathf.Max(a, Fill(Mathf.Abs(x - cx) - width, 0.012f) * Fill(y - h[k], 0.01f));
            }
            Color c = Color.Lerp(new Color(0.16f, 0.5f, 0.4f), new Color(0.45f, 0.86f, 0.6f), y);
            c.a = a;
            return c;
        }, SpriteAlignment.BottomCenter);

        // Light ray (pivot top): soft beam, strongest at the top. 128x512 = 1 x 4.
        Png("LightRay", 128, 512, (x, y) =>
        {
            float across = 1f - Mathf.Abs(x - 0.5f) * 2f;
            return new Color(0.85f, 1f, 1f, across * across * SmoothStep(0f, 1f, y) * 0.9f);
        }, SpriteAlignment.TopCenter);

        // Seashell (lives icon): scalloped fan with ridges, peach to pink.
        Png("Seashell", 128, 128, (x, y) =>
        {
            Vector2 d = new Vector2(x - 0.5f, y - 0.14f);
            float r = d.magnitude;
            float ang = Mathf.Atan2(d.x, d.y);
            float R = 0.76f + 0.03f * Mathf.Cos(ang * 13f);
            float fan = Fill(r - R, 0.012f) * Fill((Mathf.Abs(ang) - 1.12f) * Mathf.Max(r, 0.05f), 0.012f);
            float ears = Fill(SdRoundBox(new Vector2(x - 0.5f, y - 0.14f), new Vector2(0.17f, 0.07f), 0.03f), 0.012f);
            float a = Mathf.Max(fan, ears);
            float ridge = 0.5f + 0.5f * Mathf.Cos(ang * 13f);
            Color c = Color.Lerp(new Color(1f, 0.9f, 0.82f), new Color(1f, 0.7f, 0.74f), Mathf.Clamp01(r / 0.8f));
            c *= 0.9f + 0.1f * ridge;
            c = Color.Lerp(c, new Color(0.85f, 0.5f, 0.55f), SmoothStep(R - 0.05f, R, r) * 0.5f * fan);
            c.a = a;
            return c;
        });

        // Wave line under the score: 512x32, fades at both ends.
        Png("WaveLine", 512, 32, (x, y) =>
        {
            float center = 0.5f + 0.28f * Mathf.Sin(x * Mathf.PI * 2f * 4f);
            float dist = Mathf.Abs(y - center);
            float ends = SmoothStep(0f, 0.08f, x) * (1f - SmoothStep(0.92f, 1f, x));
            return new Color(1f, 1f, 1f, Fill(dist - 0.12f, 0.08f) * ends);
        });

        // Frosted dark-teal wall surface. It is stretched unevenly over each wall, so it only has a smooth,
        // soft glow toward the middle (no noise or hard features that would turn into streaks).
        Png("WallFrost", 64, 64, (x, y) =>
        {
            float glow = Mathf.Sin(Mathf.PI * x) * Mathf.Sin(Mathf.PI * y);
            float k = 0.85f + 0.15f * glow;
            return new Color(0.15f * k, 0.36f * k, 0.42f * k, 1f);
        });

        // Vignette: transparent centre, opaque edges. Coloured by the UI Image.
        Png("Vignette", 256, 256, (x, y) =>
        {
            float r = new Vector2(x - 0.5f, y - 0.5f).magnitude / 0.7071f;
            return new Color(1f, 1f, 1f, SmoothStep(0.45f, 1f, r));
        });

        foreach (var (path, align) in PendingSprites) ConfigureSprite(path, align);
    }

    /// <summary>Writes a PNG (only if missing, unless regenerating) and queues its sprite import settings.</summary>
    private static void Png(string name, int width, int height, Painter painter, SpriteAlignment align = SpriteAlignment.Center)
    {
        string path = $"{ArtDir}/{name}.png";
        if (overwriteAssets || !File.Exists(path))
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var px = new Color[width * height];
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                {
                    Color c = painter((i + 0.5f) / width, (j + 0.5f) / height);
                    px[j * width + i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), Mathf.Clamp01(c.a));
                }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        PendingSprites.Add((path, align));
    }

    private static void ConfigureSprite(string path, SpriteAlignment align)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.GetSourceTextureWidthAndHeight(out int srcWidth, out int srcHeight);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spritePixelsPerUnit = Mathf.Max(1, srcWidth);   // every sprite is 1 world unit wide
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)align;
        s.spritePivot = align switch
        {
            SpriteAlignment.BottomCenter => new Vector2(0.5f, 0f),
            SpriteAlignment.TopCenter => new Vector2(0.5f, 1f),
            _ => new Vector2(0.5f, 0.5f)
        };
        s.mipmapEnabled = false;
        s.alphaIsTransparency = true;
        s.wrapMode = TextureWrapMode.Clamp;
        s.filterMode = FilterMode.Bilinear;
        ti.SetTextureSettings(s);
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");
        if (sprite == null) throw new Exception($"[PocketOcean] Sprite '{name}' failed to import.");
        return sprite;
    }

    private static float Fill(float d, float aa) => Mathf.Clamp01(0.5f - d / aa);

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static float SdRoundBox(Vector2 p, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - half.x + r, Mathf.Abs(p.y) - half.y + r);
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    /// <summary>Approximate signed distance to a counter-clockwise triangle (negative inside).</summary>
    private static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Edge(Vector2 p0, Vector2 p1)
        {
            Vector2 e = p1 - p0;
            Vector2 n = new Vector2(e.y, -e.x).normalized;
            return Vector2.Dot(p - p0, n);
        }
        return Mathf.Max(Edge(a, b), Mathf.Max(Edge(b, c), Edge(c, a)));
    }

    // ---------------------------------------------------------------- AUDIO

    private static void GenerateAudio()
    {
        // Soft, short "plip": quick upward chirp with a fast decay.
        Wav("Plip", 0.12f, 0.6f, 0f, false, t =>
        {
            float ph = ExpChirpPhase(t, 700f, 1150f, 0.05f);
            float env = (1f - Mathf.Exp(-t / 0.003f)) * Mathf.Exp(-t / 0.028f);
            return (Mathf.Sin(ph) + 0.12f * Mathf.Sin(2f * ph)) * env;
        });

        // Glassy bubble pop: rising bubble chirp + short inharmonic "glass" partials.
        Wav("BubblePop", 0.18f, 0.6f, 0f, false, t =>
        {
            float ph = ExpChirpPhase(t, 500f, 2000f, 0.045f);
            float bubble = Mathf.Sin(ph) * (1f - Mathf.Exp(-t / 0.002f)) * Mathf.Exp(-t / 0.03f);
            float twoPi = Mathf.PI * 2f;
            float glass = 0.35f * Mathf.Sin(twoPi * 2600f * t) + 0.2f * Mathf.Sin(twoPi * 3950f * t) +
                          0.1f * Mathf.Sin(twoPi * 5300f * t);
            glass *= Mathf.Exp(-t / 0.045f) * (1f - Mathf.Exp(-t / 0.004f));
            return 0.55f * bubble + 0.3f * glass;
        });

        // Low, muffled "bloop": descending tone with a slow underwater wobble, low-passed.
        Wav("Bloop", 0.5f, 0.7f, 900f, false, t =>
        {
            float ph = ExpChirpPhase(t, 300f, 95f, 0.4f) + 0.15f * Mathf.Sin(Mathf.PI * 2f * 7f * t);
            float env = (1f - Mathf.Exp(-t / 0.01f)) * Mathf.Exp(-t / 0.14f);
            return (Mathf.Sin(ph) + 0.25f * Mathf.Sin(2f * ph)) * env;
        });

        // Calm ambient pad, 16 s seamless loop. Every frequency and LFO rate is a multiple of 1/16 Hz,
        // so the waveform at the end joins the start exactly.
        const float loop = 16f;
        float[] freqs = { 110f, 110.0625f, 165f, 220f, 246.9375f, 277.1875f, 330f, 440f };
        float[] gains = { 0.30f, 0.22f, 0.22f, 0.18f, 0.10f, 0.12f, 0.10f, 0.05f };
        int[] lfoCycles = { 1, 1, 2, 1, 3, 2, 3, 4 };
        float[] lfoPhase = { 0f, 1.3f, 2.1f, 0.7f, 4.0f, 3.1f, 5.2f, 1.9f };
        Wav("OceanAmbience", loop, 0.55f, 0f, true, t =>
        {
            float twoPi = Mathf.PI * 2f;
            float sum = 0f;
            for (int i = 0; i < freqs.Length; i++)
            {
                float lfo = 0.55f + 0.45f * Mathf.Sin(twoPi * lfoCycles[i] * t / loop + lfoPhase[i]);
                sum += gains[i] * lfo * Mathf.Sin(twoPi * freqs[i] * t);
            }
            return sum;
        }, streaming: true);
    }

    /// <summary>Phase of an exponential sweep from f0 to f1 over sweepTime, then held at f1.</summary>
    private static float ExpChirpPhase(float t, float f0, float f1, float sweepTime)
    {
        float k = f1 / f0;
        float lnK = Mathf.Log(k);
        float twoPi = Mathf.PI * 2f;
        if (t <= sweepTime)
            return twoPi * f0 * sweepTime * (Mathf.Pow(k, t / sweepTime) - 1f) / lnK;
        float end = twoPi * f0 * sweepTime * (k - 1f) / lnK;
        return end + twoPi * f1 * (t - sweepTime);
    }

    private static void Wav(string name, float seconds, float peak, float lowpassHz, bool loopable,
        Func<float, float> synth, bool streaming = false)
    {
        string path = $"{AudioDir}/{name}.wav";
        if (overwriteAssets || !File.Exists(path))
        {
            const int rate = 44100;
            int n = Mathf.RoundToInt(seconds * rate);
            var samples = new float[n];
            float max = 0.0001f;
            float lp = 0f;
            float alpha = lowpassHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * lowpassHz / rate) : 1f;
            int fadeOut = loopable ? 0 : Mathf.Min(n, rate * 3 / 1000);

            for (int i = 0; i < n; i++)
            {
                // Double-precision time keeps long loops phase-accurate.
                float v = synth((float)(i / (double)rate));
                lp += alpha * (v - lp);
                v = lp;
                if (fadeOut > 0 && i >= n - fadeOut) v *= (n - 1 - i) / (float)fadeOut;  // click-free end
                samples[i] = v;
                max = Mathf.Max(max, Mathf.Abs(v));
            }

            using (var bw = new BinaryWriter(File.Create(path)))
            {
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + n * 2);
                bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(rate);
                bw.Write(rate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(n * 2);
                float gain = peak / max;
                for (int i = 0; i < n; i++)
                    bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * gain * 32767f), -32768, 32767));
            }
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (streaming && AssetImporter.GetAtPath(path) is AudioImporter ai)
        {
            AudioImporterSampleSettings s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }
    }

    private static AudioClip LoadClip(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");
        if (clip == null) throw new Exception($"[PocketOcean] Audio '{name}' failed to import.");
        return clip;
    }

    // --------------------------------------------------------------- PREFAB

    private static GameObject BuildBrickAfterPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BrickAfterPath) == null)
        {
            if (!AssetDatabase.CopyAsset(BrickPrefabPath, BrickAfterPath))
                throw new Exception("[PocketOcean] Could not duplicate Brick.prefab.");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(BrickAfterPath);
        try
        {
            // Root renderer stays (LevelBuilder colours it, Brick.cs reads it) but is hidden.
            var rootSr = root.GetComponent<SpriteRenderer>();
            rootSr.enabled = false;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            Material mat = rootSr.sharedMaterial;
            SpriteRenderer body = CreateSprite("Body", visual.transform, LoadSprite("GlassCapsule"), OrderBrickBody, mat);
            FitToUnit(body);
            SpriteRenderer creature = CreateSprite("Creature", visual.transform, LoadSprite(CreatureNames[0]), OrderBrickCreature, mat);
            SpriteRenderer highlight = CreateSprite("Highlight", visual.transform, LoadSprite("GlassHighlight"), OrderBrickHighlight, mat);
            FitToUnit(highlight);

            var bv = visual.AddComponent<BrickVisualAfter>();
            SetRef(bv, "sourceRenderer", rootSr);
            SetRef(bv, "body", body);
            SetRef(bv, "creature", creature);
            SetRef(bv, "highlight", highlight);
            SetRefs(bv, "creatureSprites", CreatureSprites());

            PrefabUtility.SaveAsPrefabAsset(root, BrickAfterPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(BrickAfterPath);
    }

    private static Object[] CreatureSprites()
    {
        var list = new Object[CreatureNames.Length];
        for (int i = 0; i < CreatureNames.Length; i++) list[i] = LoadSprite(CreatureNames[i]);
        return list;
    }

    // ---------------------------------------------------------------- SCENE

    private static void BuildScene(GameObject brickAfter)
    {
        Scene scene = EditorSceneManager.OpenScene(AfterScenePath, OpenSceneMode.Single);

        GameObject ball = FindRoot(scene, "Ball");
        GameObject paddle = FindRoot(scene, "Paddle");
        GameObject level = FindRoot(scene, "LevelBuilder");
        GameObject camGo = FindRoot(scene, "Main Camera");
        GameObject canvas = FindRoot(scene, "Canvas");
        GameObject walls = FindRoot(scene, "Walls");
        Transform scoreText = canvas.transform.Find("ScoreText");
        Transform livesText = canvas.transform.Find("LivesText");

        // ---- Clean everything a previous run created (safe to run any number of times).
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == RootName) Object.DestroyImmediate(go);
        DestroyChildrenNamed(canvas.transform, n => n.StartsWith(UiPrefix, StringComparison.Ordinal));
        DestroyChildrenNamed(scoreText, n => n == WaveLineName);
        DestroyChildrenNamed(livesText, n => n == ShellsName);
        foreach (Transform wall in walls.transform) DestroyChildrenNamed(wall, n => n == WallVisualName);
        GameObject music = null;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name != MusicName) continue;
            if (music == null) music = go; else Object.DestroyImmediate(go);
        }

        // ---- Camera: centre on the playfield (After only). Only x changes.
        var cam = camGo.GetComponent<Camera>();
        Vector3 cp = cam.transform.position;
        cam.transform.position = new Vector3(0f, cp.y, cp.z);
        cam.backgroundColor = new Color(0.07f, 0.22f, 0.33f, 1f);
        EditorUtility.SetDirty(cam);
        EditorUtility.SetDirty(cam.transform);

        // ---- Ball & paddle: only the sprite (and fit scale) on the existing Visual child changes.
        var ballSr = ball.transform.Find("Visual").GetComponent<SpriteRenderer>();
        ballSr.sprite = LoadSprite("Pearl");
        ballSr.color = Color.white;
        ballSr.sortingOrder = OrderBall;
        FitToUnit(ballSr);
        EditorUtility.SetDirty(ballSr);

        Transform paddleVisual = paddle.transform.Find("Visual");
        var paddleSr = paddleVisual.GetComponent<SpriteRenderer>();
        paddleSr.sprite = LoadSprite("AquaPaddle");
        paddleSr.color = Color.white;
        paddleSr.sortingOrder = OrderPaddle;
        FitToUnit(paddleSr);
        EditorUtility.SetDirty(paddleSr);

        Material mat = ballSr.sharedMaterial;

        // ---- Walls: frosted visual child exactly covering each wall (no collider).
        foreach (Transform wall in walls.transform)
        {
            if (wall.GetComponent<Collider2D>() == null) continue;
            SpriteRenderer wv = CreateSprite(WallVisualName, wall, LoadSprite("WallFrost"), OrderWallVisual, mat);
            FitToUnit(wv);
        }

        // ---- Bricks: same LevelBuilder (same 4x7 layout), pointing at the After copy of the prefab.
        SetRef(level.GetComponent<LevelBuilder>(), "brickPrefab", brickAfter.GetComponent<Brick>());

        float halfH = cam.orthographicSize;
        float halfW = halfH * 16f / 9f;
        float bottom = cam.transform.position.y - halfH;
        float topY = cam.transform.position.y + halfH;

        var root = new GameObject(RootName);

        // ---- Background, centred on the camera and covering 16:9 (and wider) with margin.
        SpriteRenderer bg = CreateSprite("Background", root.transform, LoadSprite("OceanGradient"), OrderBackground, mat);
        bg.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        SetWorldSize(bg, halfH * 2f * 2.2f, halfH * 2f + 2f);
        bg.color = new Color(0.88f, 0.88f, 0.9f, 1f);

        // ---- Decor, mirrored on both sides.
        var decor = new GameObject("Decor").transform;
        decor.SetParent(root.transform, false);
        var weeds = new List<SpriteRenderer>();
        var corals = new List<SpriteRenderer>();
        // (x, width, height) for the right side; the left side is the mirror image.
        Vector3[] weedSpots = { new Vector3(9.35f, 1.2f, 2.6f), new Vector3(10.3f, 1.0f, 2.0f), new Vector3(6.8f, 0.7f, 1.0f) };
        Vector3[] coralSpots = { new Vector3(9.15f, 1.1f, 1.1f), new Vector3(10.35f, 0.8f, 0.8f) };
        Color[] coralColors = { new Color(1f, 0.6f, 0.62f), new Color(1f, 0.76f, 0.56f) };
        foreach (float side in new[] { -1f, 1f })
        {
            foreach (Vector3 s in weedSpots)
            {
                SpriteRenderer sr = CreateSprite($"Seaweed_{(side < 0 ? "L" : "R")}", decor, LoadSprite("Seaweed"), OrderDecor, mat);
                sr.transform.position = new Vector3(side * s.x, bottom, 0f);
                SetWorldSize(sr, s.y, s.z);
                sr.flipX = side > 0f;
                sr.color = new Color(1f, 1f, 1f, 0.35f);
                weeds.Add(sr);
            }
            for (int i = 0; i < coralSpots.Length; i++)
            {
                Vector3 s = coralSpots[i];
                SpriteRenderer sr = CreateSprite($"Coral_{(side < 0 ? "L" : "R")}", decor, LoadSprite("Coral"), OrderDecor + 1, mat);
                sr.transform.position = new Vector3(side * s.x, bottom + s.z * 0.45f, 0f);
                SetWorldSize(sr, s.y, s.z);
                sr.flipX = side > 0f;
                Color c = coralColors[i];
                c.a = 0.35f;
                sr.color = c;
                corals.Add(sr);
            }
        }
        float[] rayX = { -6f, -2f, 2f, 6f };
        float[] rayRot = { -10f, -4f, 4f, 10f };
        var rays = new List<SpriteRenderer>();
        for (int i = 0; i < rayX.Length; i++)
        {
            SpriteRenderer sr = CreateSprite($"LightRay_{i}", decor, LoadSprite("LightRay"), OrderLightRays, mat);
            sr.transform.position = new Vector3(rayX[i], topY, 0f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rayRot[i]);
            SetWorldSize(sr, 2.4f, 12f);
            sr.color = new Color(1f, 1f, 1f, 0.07f);
            rays.Add(sr);
        }
        var progression = decor.gameObject.AddComponent<OceanProgression>();
        SetRef(progression, "background", bg);
        SetRef(progression, "brickParent", level.transform);
        SetRefs(progression, "seaweed", weeds.ToArray());
        SetRefs(progression, "coral", corals.ToArray());
        SetRefs(progression, "lightRays", rays.ToArray());

        // ---- Shared particle pool and creatures.
        var pool = AddChild<FxSpritePool>(root.transform, "FxPool");
        SetRef(pool, "spriteMaterial", mat);

        var creatures = AddChild<CreatureDrifter>(root.transform, "Creatures");
        SetRefs(creatures, "creatureSprites", CreatureSprites());
        SetRef(creatures, "spriteMaterial", mat);
        SetInt(creatures, "sortingOrder", OrderCreatures);

        // ---- 1. Brick burst.
        var burst = AddChild<BrickBurstFeedback>(root.transform, "BrickBurst");
        SetRef(burst, "pool", pool);
        SetRef(burst, "bubbleSprite", LoadSprite("Bubble"));
        SetRef(burst, "dropletSprite", LoadSprite("Droplet"));
        SetRef(burst, "ringSprite", LoadSprite("RippleRing"));
        SetRef(burst, "shardSprite", LoadSprite("GlassShard"));
        SetRef(burst, "creatures", creatures);

        // ---- 2. Paddle squash + ripple.
        var squash = AddChild<PaddleSquashFeedback>(root.transform, "PaddleSquash");
        SetRef(squash, "visual", paddleVisual);
        SetRef(squash, "pool", pool);
        SetRef(squash, "rippleSprite", LoadSprite("RippleRing"));

        // ---- 3. Event audio.
        var audio = AddChild<OceanAudioFeedback>(root.transform, "Audio");
        SetRef(audio, "paddlePlip.clip", LoadClip("Plip"));
        SetRef(audio, "brickPop.clip", LoadClip("BubblePop"));
        SetRef(audio, "ballLostBloop.clip", LoadClip("Bloop"));

        // ---- 5a. Ball trail.
        var trail = AddChild<BallTrailFeedback>(root.transform, "BallTrail");
        SetRef(trail, "target", ballSr.transform);
        SetRef(trail, "pool", pool);
        SetRef(trail, "bubbleSprite", LoadSprite("Bubble"));

        // ---- Background music (reuse the object if it exists; add what is missing).
        if (music == null) music = new GameObject(MusicName);
        var src = music.GetComponent<AudioSource>();
        if (src == null) src = music.AddComponent<AudioSource>();
        src.clip = LoadClip("OceanAmbience");
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        if (music.GetComponent<BackgroundMusicFeedback>() == null) music.AddComponent<BackgroundMusicFeedback>();
        EditorUtility.SetDirty(src);

        // ---- UI: vignette (first Canvas child so HUD draws on top; never blocks raycasts).
        var vig = new GameObject(VignetteName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vig.layer = canvas.layer;
        vig.transform.SetParent(canvas.transform, false);
        vig.transform.SetSiblingIndex(0);
        var vrt = (RectTransform)vig.transform;
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        var vigImg = vig.GetComponent<Image>();
        vigImg.sprite = LoadSprite("Vignette");
        vigImg.color = new Color(0.01f, 0.05f, 0.1f, 0f);
        vigImg.raycastTarget = false;

        // ---- Score: light aqua, softer/rounder SDF look, wave line under it. The pop still scales ScoreText.
        var scoreTmp = scoreText.GetComponent<TMP_Text>();
        scoreTmp.fontSharedMaterial = BuildScoreMaterial(scoreTmp.font);
        scoreTmp.color = new Color(0.78f, 0.96f, 1f, 1f);
        scoreTmp.characterSpacing = 4f;
        EditorUtility.SetDirty(scoreTmp);

        var wave = new GameObject(WaveLineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        wave.layer = canvas.layer;
        wave.transform.SetParent(scoreText, false);
        var wrt = (RectTransform)wave.transform;
        wrt.anchorMin = wrt.anchorMax = new Vector2(0f, 0f);
        wrt.pivot = new Vector2(0f, 0.5f);
        wrt.anchoredPosition = new Vector2(2f, 4f);
        wrt.sizeDelta = new Vector2(290f, 14f);
        var waveImg = wave.GetComponent<Image>();
        waveImg.sprite = LoadSprite("WaveLine");
        waveImg.color = new Color(0.6f, 0.92f, 1f, 0.8f);
        waveImg.raycastTarget = false;

        // ---- Lives: hide the text (After only; HUD keeps writing it harmlessly) and show seashells instead.
        var livesTmp = livesText.GetComponent<TMP_Text>();
        livesTmp.enabled = false;
        EditorUtility.SetDirty(livesTmp);

        var shells = new GameObject(ShellsName, typeof(RectTransform));
        shells.layer = canvas.layer;
        shells.transform.SetParent(livesText, false);
        var srt = (RectTransform)shells.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(1f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(10f, 50f);
        var shellsFx = shells.AddComponent<LivesShellsFeedback>();
        SetRef(shellsFx, "shellSprite", LoadSprite("Seashell"));

        // ---- 4. Score pop and 5b. life-loss vignette.
        var ui = new GameObject("UIFeedback").transform;
        ui.SetParent(root.transform, false);
        SetRef(ui.gameObject.AddComponent<ScorePopFeedback>(), "target", (RectTransform)scoreText);
        var lifeLoss = ui.gameObject.AddComponent<LifeLossFeedback>();
        SetRef(lifeLoss, "livesText", null);
        SetRef(lifeLoss, "vignette", vigImg);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new Exception("[PocketOcean] Failed to save After.unity.");
    }

    private static Material BuildScoreMaterial(TMP_FontAsset font)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(ScoreMaterialPath) != null)
            AssetDatabase.DeleteAsset(ScoreMaterialPath);

        var m = new Material(font.material) { name = "PocketOcean_ScoreText" };
        m.SetFloat("_FaceDilate", 0.12f);       // slightly heavier strokes
        m.SetFloat("_OutlineSoftness", 0.04f);  // softened corners for a rounder look
        m.EnableKeyword("UNDERLAY_ON");         // soft aqua glow behind the text
        m.SetColor("_UnderlayColor", new Color(0.15f, 0.7f, 0.9f, 0.55f));
        m.SetFloat("_UnderlayDilate", 0.35f);
        m.SetFloat("_UnderlaySoftness", 0.6f);
        m.SetFloat("_UnderlayOffsetX", 0f);
        m.SetFloat("_UnderlayOffsetY", 0f);
        AssetDatabase.CreateAsset(m, ScoreMaterialPath);
        return m;
    }

    // ------------------------------------------------------------- VALIDATE

    private static void Validate(GameObject brickAfter)
    {
        var errors = new List<string>();
        Scene scene = SceneManager.GetActiveScene();

        // Exactly one of every created object (idempotency).
        GameObject canvas = FindRoot(scene, "Canvas");
        CheckCount(errors, CountRoots(scene, RootName), RootName);
        CheckCount(errors, CountRoots(scene, MusicName), MusicName);
        CheckCount(errors, CountChildren(canvas.transform, VignetteName), VignetteName);
        CheckCount(errors, CountChildren(canvas.transform.Find("ScoreText"), WaveLineName), WaveLineName);
        CheckCount(errors, CountChildren(canvas.transform.Find("LivesText"), ShellsName), ShellsName);
        foreach (Transform wall in FindRoot(scene, "Walls").transform)
            if (wall.GetComponent<Collider2D>() != null)
                CheckCount(errors, CountChildren(wall, WallVisualName), $"{wall.name}/{WallVisualName}");

        // Decoration must have no colliders.
        GameObject root = FindRoot(scene, RootName);
        if (root.GetComponentsInChildren<Collider2D>(true).Length > 0) errors.Add("Collider found under PocketOcean.");
        if (FindRoot(scene, MusicName).GetComponentsInChildren<Collider2D>(true).Length > 0) errors.Add("Collider on BackgroundMusic.");
        if (brickAfter.transform.Find("Visual").GetComponentsInChildren<Collider2D>(true).Length > 0)
            errors.Add("Collider found on Brick_After visual.");
        foreach (Transform wall in FindRoot(scene, "Walls").transform)
        {
            Transform wv = wall.Find(WallVisualName);
            if (wv != null && wv.GetComponentsInChildren<Collider2D>(true).Length > 0) errors.Add($"Collider on {wall.name} visual.");
        }

        // Brick_After keeps Brick's physics and values.
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(BrickPrefabPath);
        var a = original.GetComponent<BoxCollider2D>();
        var b = brickAfter.GetComponent<BoxCollider2D>();
        if (a.size != b.size || a.offset != b.offset || a.isTrigger != b.isTrigger)
            errors.Add("Brick_After collider differs from Brick.");
        if (original.transform.localScale != brickAfter.transform.localScale)
            errors.Add("Brick_After root scale differs from Brick.");
        var sa = new SerializedObject(original.GetComponent<Brick>());
        var sb = new SerializedObject(brickAfter.GetComponent<Brick>());
        foreach (string prop in new[] { "hitPoints", "scoreValue" })
            if (sa.FindProperty(prop).intValue != sb.FindProperty(prop).intValue)
                errors.Add($"Brick_After {prop} differs from Brick.");

        // Brick capsule visual (body) is exactly the collider in local space (both are then scaled by the layout).
        var body = brickAfter.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        Vector2 bodyLocal = Vector2.Scale(body.sprite.bounds.size, body.transform.localScale);
        if (!Approximately(bodyLocal, b.size)) errors.Add($"Brick body {bodyLocal} != collider {b.size}.");

        // Paddle, ball and walls: visual world size == collider world size.
        GameObject paddle = FindRoot(scene, "Paddle");
        var paddleSr = paddle.transform.Find("Visual").GetComponent<SpriteRenderer>();
        Vector2 paddleVis = Vector2.Scale(paddleSr.sprite.bounds.size, paddleSr.transform.lossyScale);
        Vector2 paddleCol = Vector2.Scale(paddle.GetComponent<BoxCollider2D>().size, paddle.transform.lossyScale);
        if (!Approximately(paddleVis, paddleCol)) errors.Add($"Paddle visual {paddleVis} != collider {paddleCol}.");

        GameObject ball = FindRoot(scene, "Ball");
        var ballSr = ball.transform.Find("Visual").GetComponent<SpriteRenderer>();
        Vector2 ballVis = Vector2.Scale(ballSr.sprite.bounds.size, ballSr.transform.lossyScale);
        float ballD = ball.GetComponent<CircleCollider2D>().radius * 2f * ball.transform.lossyScale.x;
        if (!Approximately(ballVis, new Vector2(ballD, ballD))) errors.Add($"Ball visual {ballVis} != collider {ballD}.");

        foreach (Transform wall in FindRoot(scene, "Walls").transform)
        {
            var col = wall.GetComponent<BoxCollider2D>();
            Transform wvT = wall.Find(WallVisualName);
            if (col == null || wvT == null) continue;
            var wv = wvT.GetComponent<SpriteRenderer>();
            Vector2 vis = Vector2.Scale(wv.sprite.bounds.size, wv.transform.lossyScale);
            Vector2 cs = Vector2.Scale(col.size, wall.lossyScale);
            if (!Approximately(vis, cs)) errors.Add($"{wall.name} visual {vis} != collider {cs}.");
        }

        // Background centred on the camera and covering a 16:9 view.
        var cam = FindRoot(scene, "Main Camera").GetComponent<Camera>();
        var bg = root.transform.Find("Background").GetComponent<SpriteRenderer>();
        Vector2 bgSize = Vector2.Scale(bg.sprite.bounds.size, bg.transform.lossyScale);
        float halfH = cam.orthographicSize, halfW = halfH * 16f / 9f;
        if (Mathf.Abs(bg.transform.position.x - cam.transform.position.x) > 0.001f ||
            Mathf.Abs(bg.transform.position.y - cam.transform.position.y) > 0.001f)
            errors.Add("Background is not centred on the camera.");
        if (bgSize.x < halfW * 2f || bgSize.y < halfH * 2f) errors.Add($"Background {bgSize} does not cover the view.");

        // Decor mirrored: x positions sum to ~0.
        float sumX = 0f;
        foreach (SpriteRenderer sr in root.transform.Find("Decor").GetComponentsInChildren<SpriteRenderer>())
            sumX += sr.transform.position.x;
        if (Mathf.Abs(sumX) > 0.01f) errors.Add($"Decor is not symmetric (sum of x = {sumX}).");

        if (errors.Count > 0)
            throw new Exception("[PocketOcean] Validation failed:\n" + string.Join("\n", errors));

        float contrast = PearlContrast();
        Debug.Log($"[PocketOcean] Validation OK. Paddle {paddleVis} = collider {paddleCol}; ball {ballVis.x} = collider {ballD}; " +
                  $"brick body = collider; walls = colliders; background centred {bgSize} covers {halfW * 2f}x{halfH * 2f}; " +
                  $"decor symmetric; one of each object; no decorative colliders. Pearl vs brightest water contrast {contrast:0.0}:1.");
    }

    /// <summary>WCAG-style contrast between the pearl's average colour and the brightest background pixel.</summary>
    private static float PearlContrast()
    {
        float pearl = AverageLuminance(ArtDir + "/Pearl.png", out _);
        AverageLuminance(ArtDir + "/OceanGradient.png", out float bgMax);
        return (pearl + 0.05f) / (bgMax + 0.05f);
    }

    private static float AverageLuminance(string path, out float max)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        Color[] px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        double sum = 0, weight = 0;
        max = 0f;
        foreach (Color c in px)
        {
            if (c.a < 0.5f) continue;
            float l = 0.2126f * c.linear.r + 0.7152f * c.linear.g + 0.0722f * c.linear.b;
            sum += l;
            weight += 1;
            max = Mathf.Max(max, l);
        }
        return weight > 0 ? (float)(sum / weight) : 0f;
    }

    // -------------------------------------------------------------- PREVIEW

    /// <summary>Renders the saved After scene (with bricks built in edit mode) to a PNG, then discards the changes.</summary>
    private static void RenderPreview(string outPath)
    {
        Scene scene = EditorSceneManager.OpenScene(AfterScenePath, OpenSceneMode.Single);
        var builder = FindRoot(scene, "LevelBuilder").GetComponent<LevelBuilder>();
        builder.Build();
        MethodInfo start = typeof(BrickVisualAfter).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (BrickVisualAfter v in builder.GetComponentsInChildren<BrickVisualAfter>())
            start?.Invoke(v, null);

        // GPU rendering is unreliable in batch mode, so composite the sprites on the CPU instead
        // (real textures, transforms, flips, tints and sorting order; world sprites only, no UI).
        var cam = FindRoot(scene, "Main Camera").GetComponent<Camera>();
        const int W = 1920, H = 1080;
        var img = new Color[W * H];
        Color clear = cam.backgroundColor;
        for (int i = 0; i < img.Length; i++) img[i] = clear;

        var renderers = new List<SpriteRenderer>();
        foreach (GameObject go in scene.GetRootGameObjects())
            renderers.AddRange(go.GetComponentsInChildren<SpriteRenderer>(false));
        renderers.RemoveAll(r => !r.enabled || r.sprite == null);
        // Stable sort by sorting order (ties keep hierarchy order).
        var ordered = new List<(SpriteRenderer r, int i)>();
        for (int i = 0; i < renderers.Count; i++) ordered.Add((renderers[i], i));
        ordered.Sort((p, q) => p.r.sortingOrder != q.r.sortingOrder ? p.r.sortingOrder.CompareTo(q.r.sortingOrder) : p.i.CompareTo(q.i));

        float halfH = cam.orthographicSize, halfW = halfH * W / H;
        Vector2 camPos = cam.transform.position;
        var cache = new Dictionary<Texture2D, (Color[] px, int w, int h)>();

        foreach (var (sr, _) in ordered)
        {
            Texture2D source = sr.sprite.texture;
            if (!cache.TryGetValue(source, out var t))
            {
                string p = AssetDatabase.GetAssetPath(source);
                if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
                {
                    var tx = new Texture2D(2, 2);
                    tx.LoadImage(File.ReadAllBytes(p));
                    t = (tx.GetPixels(), tx.width, tx.height);
                    Object.DestroyImmediate(tx);
                }
                else t = (new[] { Color.white }, 1, 1);   // built-in sprites (e.g. wall squares) as solid quads
                cache[source] = t;
            }

            Bounds sb = sr.sprite.bounds;
            Matrix4x4 m = sr.transform.localToWorldMatrix;
            Matrix4x4 inv = m.inverse;

            // Screen-space bounding box of the four corners.
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (Vector2 c in new[] { new Vector2(sb.min.x, sb.min.y), new Vector2(sb.max.x, sb.min.y),
                                          new Vector2(sb.min.x, sb.max.y), new Vector2(sb.max.x, sb.max.y) })
            {
                Vector2 lc = sr.flipX ? new Vector2(-c.x, c.y) : c;
                Vector3 w = m.MultiplyPoint3x4(lc);
                float px = (w.x - camPos.x + halfW) / (2f * halfW) * W;
                float py = (w.y - camPos.y + halfH) / (2f * halfH) * H;
                minX = Mathf.Min(minX, px); maxX = Mathf.Max(maxX, px);
                minY = Mathf.Min(minY, py); maxY = Mathf.Max(maxY, py);
            }
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(maxY));
            Color tint = sr.color;

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float wx = camPos.x - halfW + (x + 0.5f) / W * 2f * halfW;
                    float wy = camPos.y - halfH + (y + 0.5f) / H * 2f * halfH;
                    Vector3 l = inv.MultiplyPoint3x4(new Vector3(wx, wy, 0f));
                    if (sr.flipX) l.x = -l.x;
                    float u = (l.x - sb.min.x) / sb.size.x, v = (l.y - sb.min.y) / sb.size.y;
                    if (u < 0f || u >= 1f || v < 0f || v >= 1f) continue;
                    Color s = t.px[Mathf.Min(t.h - 1, (int)(v * t.h)) * t.w + Mathf.Min(t.w - 1, (int)(u * t.w))] * tint;
                    int k = y * W + x;
                    img[k] = Color.Lerp(img[k], new Color(s.r, s.g, s.b, 1f), s.a);
                }
        }

        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.SetPixels(img);
        tex.Apply();
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        // Throw away the edit-mode bricks: reopen the saved scene without saving.
        EditorSceneManager.OpenScene(AfterScenePath, OpenSceneMode.Single);
        Debug.Log($"[PocketOcean] Preview written to {outPath}");
    }

    // -------------------------------------------------------------- HELPERS

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    private static bool Approximately(Vector2 a, Vector2 b) =>
        Mathf.Abs(a.x - b.x) < 0.001f && Mathf.Abs(a.y - b.y) < 0.001f;

    private static void CheckCount(List<string> errors, int count, string what)
    {
        if (count != 1) errors.Add($"Expected exactly one '{what}', found {count}.");
    }

    private static int CountRoots(Scene scene, string name)
    {
        int n = 0;
        foreach (GameObject go in scene.GetRootGameObjects()) if (go.name == name) n++;
        return n;
    }

    private static int CountChildren(Transform parent, string name)
    {
        int n = 0;
        foreach (Transform t in parent) if (t.name == name) n++;
        return n;
    }

    private static void DestroyChildrenNamed(Transform parent, Func<string, bool> match)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            if (match(parent.GetChild(i).name)) Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        throw new Exception($"[PocketOcean] '{name}' not found in {scene.path}.");
    }

    /// <summary>Scales a renderer so its sprite is exactly 1 x 1 in its parent's space (= the parent's collider).</summary>
    private static void FitToUnit(SpriteRenderer sr)
    {
        Vector2 s = sr.sprite.bounds.size;
        sr.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
    }

    /// <summary>Scales a renderer (under an unscaled parent) to an exact world size.</summary>
    private static void SetWorldSize(SpriteRenderer sr, float width, float height)
    {
        Vector2 s = sr.sprite.bounds.size;
        sr.transform.localScale = new Vector3(width / s.x, height / s.y, 1f);
    }

    private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, int order, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (mat != null) sr.sharedMaterial = mat;
        return sr;
    }

    private static T AddChild<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static SerializedProperty Prop(SerializedObject so, string name)
    {
        SerializedProperty p = so.FindProperty(name);
        if (p == null) throw new Exception($"[PocketOcean] Property '{name}' not found on {so.targetObject.GetType().Name}.");
        return p;
    }

    private static void SetRef(Object target, string prop, Object value)
    {
        var so = new SerializedObject(target);
        Prop(so, prop).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefs(Object target, string prop, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = Prop(so, prop);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string prop, int value)
    {
        var so = new SerializedObject(target);
        Prop(so, prop).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
