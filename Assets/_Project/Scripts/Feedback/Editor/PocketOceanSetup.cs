// PocketOceanSetup (Editor only)
// Trigger: menu "Tools/Pocket Ocean/Setup After Scene", or batch mode:
//          Unity -batchmode -quit -projectPath . -executeMethod PocketOceanSetup.RunAll
// Purpose: one-click, repeatable setup for the Pocket Ocean theme.
//   1. Procedurally generates every sprite (PNG) into Assets/_Project/Art/PocketOcean.
//   2. Procedurally synthesises every sound (WAV) into Assets/_Project/Audio/PocketOcean.
//   3. Creates Assets/_Project/Prefabs/Brick_After.prefab (copy of Brick with a visual child).
//   4. Opens Assets/HW3/After.unity, swaps visual sprites and adds the feedback objects, then saves.
//   5. Validates: no colliders on decoration, Brick_After physics/values identical to Brick, visuals = collider size.
// It never opens Before.unity and never edits Brick.prefab or any gameplay script. Re-running replaces its own objects.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PocketOceanSetup
{
    private const string ArtDir = "Assets/_Project/Art/PocketOcean";
    private const string AudioDir = "Assets/_Project/Audio/PocketOcean";
    private const string BrickPrefabPath = "Assets/_Project/Prefabs/Brick.prefab";
    private const string BrickAfterPath = "Assets/_Project/Prefabs/Brick_After.prefab";
    private const string AfterScenePath = "Assets/HW3/After.unity";
    private const string RootName = "PocketOcean";
    private const string VignetteName = "PocketOcean_Vignette";

    // Sorting orders (bricks' root is 0).
    private const int OrderBackground = -100;
    private const int OrderLightRays = -90;
    private const int OrderSeaweed = -60;
    private const int OrderCreatures = -50;
    private const int OrderBrickWater = 1;
    private const int OrderBrickGlass = 2;
    private const int OrderPaddle = 5;
    private const int OrderBall = 10;

    [MenuItem("Tools/Pocket Ocean/Setup After Scene")]
    public static void RunAll()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(AudioDir);
        AssetDatabase.Refresh();

        GenerateArt();
        GenerateAudio();
        AssetDatabase.Refresh();

        GameObject brickAfter = BuildBrickAfterPrefab();
        BuildScene(brickAfter);
        Validate(brickAfter);

        AssetDatabase.SaveAssets();
        Debug.Log("[PocketOcean] Setup complete. After.unity updated; Before.unity untouched.");
    }

    // ------------------------------------------------------------------ ART

    private delegate Color Painter(float x, float y);

    private static readonly List<(string path, int size, SpriteAlignment align)> PendingSprites = new();

    private static void GenerateArt()
    {
        PendingSprites.Clear();

        // Ocean background: dark, low-contrast vertical gradient with a faint glow from the surface.
        Png("OceanGradient", 256, (x, y) =>
        {
            Color bottom = new Color(0.02f, 0.07f, 0.16f);
            Color top = new Color(0.09f, 0.24f, 0.38f);
            Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y));
            float glow = Mathf.Exp(-((x - 0.5f) * (x - 0.5f) / 0.08f + (y - 1f) * (y - 1f) / 0.15f)) * 0.05f;
            float caustic = 0.012f * Mathf.Sin(x * 25f + Mathf.Sin(y * 17f) * 2f) * y;
            return new Color(c.r + glow + caustic, c.g + glow * 1.2f + caustic, c.b + glow * 1.3f + caustic, 1f);
        });

        // Pearl: 1-unit circle (= ball collider), white with soft pink/blue sheen and a highlight.
        Png("Pearl", 128, (x, y) =>
        {
            Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
            float r = p.magnitude;
            float a = Fill(r - 0.497f, 0.012f);
            float n = Mathf.Clamp01(r / 0.5f);
            Color c = Color.Lerp(Color.white, new Color(0.82f, 0.86f, 0.95f), n * n);
            c = Color.Lerp(c, new Color(0.96f, 0.84f, 0.93f), SmoothStep(0.7f, 1f, n) * 0.5f);
            float hl = 1f - SmoothStep(0f, 0.17f, (p - new Vector2(-0.15f, 0.17f)).magnitude);
            c = Color.Lerp(c, Color.white, hl);
            float sheen = 1f - SmoothStep(0f, 0.13f, (p - new Vector2(0.17f, -0.16f)).magnitude);
            c = Color.Lerp(c, new Color(0.88f, 0.95f, 1f), sheen * 0.4f);
            c.a = a;
            return c;
        });

        // Coral/shell paddle: painted for a 3 x 0.5 world shape; edges sit on the collider edges.
        Png("CoralPaddle", 512, (x, y) =>
        {
            Vector2 w = new Vector2((x - 0.5f) * 3f, (y - 0.5f) * 0.5f);
            float d = SdRoundBox(w, new Vector2(1.5f, 0.25f), 0.25f);
            float a = Fill(d + 0.002f, 0.009f);
            float t = (w.y + 0.25f) / 0.5f;
            Color c = Color.Lerp(new Color(0.86f, 0.36f, 0.38f), new Color(1f, 0.64f, 0.54f), t);
            float ridge = 0.5f + 0.5f * Mathf.Cos(w.x * 14f);
            c *= 1f - 0.07f * ridge * (1f - t * 0.5f);
            float band = Mathf.Exp(-Mathf.Pow((w.y - 0.13f) / 0.04f, 2f)) *
                         (1f - SmoothStep(1.0f, 1.35f, Mathf.Abs(w.x)));
            c = Color.Lerp(c, new Color(1f, 0.9f, 0.85f), band * 0.6f);
            float rim = SmoothStep(-0.05f, 0f, d);
            c = Color.Lerp(c, c * 0.75f, rim * 0.6f);
            c.a = a;
            return c;
        });

        // Glass capsule shell: painted for a 2 x 0.7 world brick. Mostly clear with a bright rim and highlight.
        Png("GlassCapsule", 256, (x, y) =>
        {
            Vector2 w = new Vector2((x - 0.5f) * 2f, (y - 0.5f) * 0.7f);
            float d = SdRoundBox(w, new Vector2(1f, 0.35f), 0.3f);
            float inside = Fill(d, 0.012f);
            float rim = SmoothStep(-0.07f, -0.015f, d);
            float hl = Mathf.Exp(-Mathf.Pow((w.y - 0.2f) / 0.035f, 2f)) *
                       (1f - SmoothStep(0.55f, 0.8f, Mathf.Abs(w.x + 0.1f)));
            float low = Mathf.Exp(-Mathf.Pow((w.y + 0.24f) / 0.03f, 2f)) *
                        (1f - SmoothStep(0.4f, 0.7f, Mathf.Abs(w.x - 0.2f))) * 0.25f;
            float alpha = inside * Mathf.Clamp01(0.14f + rim * 0.75f + hl * 0.6f + low);
            return new Color(0.94f, 0.99f, 1f, alpha);
        });

        // Water inside the capsule: greyscale so the brick colour tints it; wavy surface and a few tiny bubbles.
        Vector3[] innerBubbles =
        {
            new Vector3(-0.5f, -0.1f, 0.035f), new Vector3(0.35f, -0.05f, 0.025f),
            new Vector3(0.62f, -0.16f, 0.03f), new Vector3(-0.2f, -0.18f, 0.02f)
        };
        Png("CapsuleWater", 256, (x, y) =>
        {
            Vector2 w = new Vector2((x - 0.5f) * 2f, (y - 0.5f) * 0.7f);
            float d = SdRoundBox(w, new Vector2(0.93f, 0.28f), 0.24f);
            float inside = Fill(d, 0.012f);
            float level = 0.12f + 0.025f * Mathf.Sin(w.x * 5.5f);
            float below = Fill(w.y - level, 0.012f);
            float depth = Mathf.Clamp01((level - w.y) / 0.5f);
            float v = Mathf.Lerp(1f, 0.72f, depth);
            float surface = Mathf.Exp(-Mathf.Pow((w.y - level) / 0.018f, 2f));
            float alpha = inside * Mathf.Clamp01(below * 0.95f + surface * 0.6f);
            foreach (Vector3 b in innerBubbles)
            {
                float bd = (w - new Vector2(b.x, b.y)).magnitude - b.z;
                float bub = Fill(bd, 0.008f);
                v = Mathf.Lerp(v, 1f, bub);
                alpha = Mathf.Lerp(alpha, 0.55f * inside, bub);
            }
            return new Color(v, v, v, alpha);
        });

        // Bubble: clear centre, bright rim, small highlight.
        Png("Bubble", 64, (x, y) =>
        {
            Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
            float r = p.magnitude;
            float inside = Fill(r - 0.47f, 0.03f);
            float rim = SmoothStep(0.33f, 0.46f, r);
            float alpha = inside * (0.18f + 0.75f * rim);
            float hl = 1f - SmoothStep(0.04f, 0.1f, (p - new Vector2(-0.15f, 0.15f)).magnitude);
            return new Color(1f, 1f, 1f, Mathf.Max(alpha, hl * inside));
        });

        // Droplet: soft round dot.
        Png("Droplet", 64, (x, y) =>
        {
            float r = new Vector2(x - 0.5f, y - 0.5f).magnitude;
            return new Color(1f, 1f, 1f, 1f - SmoothStep(0.2f, 0.48f, r));
        });

        // Ripple ring.
        Png("RippleRing", 128, (x, y) =>
        {
            float r = new Vector2(x - 0.5f, y - 0.5f).magnitude;
            float a = Mathf.Exp(-Mathf.Pow((r - 0.45f) / 0.025f, 2f)) +
                      0.25f * Mathf.Exp(-Mathf.Pow((r - 0.38f) / 0.02f, 2f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        });

        // Jellyfish: rounded bell + wavy tentacles.
        Png("Jellyfish", 128, (x, y) =>
        {
            float ex = (x - 0.5f) / 0.3f, ey = (y - 0.62f) / 0.24f;
            float bellD = (Mathf.Sqrt(ex * ex + ey * ey) - 1f) * 0.24f;
            float bell = Fill(bellD, 0.012f) * Fill(0.52f + 0.015f * Mathf.Sin(x * 60f) - y, 0.01f);
            float tent = 0f;
            for (int k = 0; k < 5; k++)
            {
                float x0 = 0.34f + k * 0.08f;
                float cx = x0 + 0.025f * Mathf.Sin(y * 22f + k);
                float width = 0.008f + 0.01f * Mathf.Clamp01((y - 0.12f) / 0.4f);
                float tk = Fill(Mathf.Abs(x - cx) - width, 0.008f) * Fill(0.12f - y, 0.01f) * Fill(y - 0.54f, 0.01f);
                tent = Mathf.Max(tent, tk * Mathf.Lerp(0.3f, 0.7f, (y - 0.12f) / 0.42f));
            }
            float glow = 1f - SmoothStep(0f, 0.25f, new Vector2(x - 0.5f, y - 0.66f).magnitude);
            Color c = Color.Lerp(new Color(0.9f, 0.7f, 1f), new Color(1f, 0.92f, 1f), glow);
            c.a = Mathf.Max(bell * 0.85f, tent);
            return c;
        });

        // Fish (faces left): oval body, triangle tail, stripe and eye.
        Png("Fish", 128, (x, y) =>
        {
            float ex = (x - 0.44f) / 0.27f, ey = (y - 0.5f) / 0.16f;
            float bodyD = (Mathf.Sqrt(ex * ex + ey * ey) - 1f) * 0.16f;
            float body = Fill(bodyD, 0.01f);
            float tailD = Mathf.Max(Mathf.Max(Mathf.Abs(y - 0.5f) - (x - 0.64f) * 0.85f, 0.64f - x), x - 0.88f);
            float tail = Fill(tailD, 0.01f);
            float t = Mathf.Clamp01((y - 0.34f) / 0.32f);
            Color c = Color.Lerp(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.74f, 0.32f), t);
            float stripe = Mathf.Exp(-Mathf.Pow((x - 0.5f) / 0.025f, 2f)) * body;
            c = Color.Lerp(c, Color.white, stripe * 0.7f);
            float eyeD = new Vector2(x - 0.27f, y - 0.54f).magnitude;
            c = Color.Lerp(c, Color.white, Fill(eyeD - 0.045f, 0.008f));
            c = Color.Lerp(c, new Color(0.1f, 0.1f, 0.2f), Fill(eyeD - 0.025f, 0.008f));
            c.a = Mathf.Max(body, tail);
            return c;
        });

        // Seahorse: chain of circles along an S-curve, snout, curled tail, eye.
        const int seg = 40;
        var centers = new Vector2[seg];
        var radii = new float[seg];
        for (int i = 0; i < seg; i++)
        {
            float t = i / (seg - 1f);
            centers[i] = new Vector2(0.46f + 0.12f * Mathf.Sin(t * Mathf.PI * 1.1f), 0.80f - 0.62f * t);
            radii[i] = 0.1f * (1f - t) + 0.02f + 0.03f * Mathf.Sin(t * Mathf.PI);
        }
        Png("Seahorse", 128, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float d = float.MaxValue;
            for (int i = 0; i < seg; i++) d = Mathf.Min(d, (p - centers[i]).magnitude - radii[i]);
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.5f, 0.82f), new Vector2(0.68f, 0.79f)) - 0.03f);
            d = Mathf.Min(d, Mathf.Abs((p - new Vector2(0.43f, 0.14f)).magnitude - 0.06f) - 0.022f);
            float a = Fill(d, 0.01f);
            Color c = Color.Lerp(new Color(0.5f, 0.9f, 0.75f), new Color(0.85f, 1f, 0.75f), Mathf.Clamp01(x * 1.6f - 0.5f));
            c = Color.Lerp(c, new Color(0.08f, 0.12f, 0.2f), Fill((p - new Vector2(0.5f, 0.85f)).magnitude - 0.02f, 0.008f));
            c.a = a;
            return c;
        });

        // Seaweed (pivot bottom): three tapering wavy strands.
        Png("Seaweed", 128, (x, y) =>
        {
            float a = 0f;
            float[] x0 = { 0.3f, 0.5f, 0.7f };
            float[] h = { 0.75f, 0.97f, 0.85f };
            for (int k = 0; k < 3; k++)
            {
                float cx = x0[k] + 0.07f * Mathf.Sin(y * 9f + k * 1.3f) * y;
                float width = Mathf.Lerp(0.06f, 0.012f, Mathf.Clamp01(y / h[k]));
                a = Mathf.Max(a, Fill(Mathf.Abs(x - cx) - width, 0.01f) * Fill(y - h[k], 0.02f));
            }
            Color c = Color.Lerp(new Color(0.12f, 0.45f, 0.36f), new Color(0.38f, 0.82f, 0.58f), y);
            c.a = a;
            return c;
        }, SpriteAlignment.BottomCenter);

        // Light ray (pivot top): soft beam, strongest at the top.
        Png("LightRay", 128, (x, y) =>
        {
            float across = 1f - Mathf.Abs(x - 0.5f) * 2f;
            float a = across * across * SmoothStep(0f, 1f, y) * 0.9f;
            return new Color(0.75f, 0.95f, 1f, a);
        }, SpriteAlignment.TopCenter);

        // Vignette: transparent centre, opaque edges. Coloured by the UI Image.
        Png("Vignette", 256, (x, y) =>
        {
            float r = new Vector2(x - 0.5f, y - 0.5f).magnitude / 0.7071f;
            return new Color(1f, 1f, 1f, SmoothStep(0.45f, 1f, r));
        });

        foreach (var (path, size, align) in PendingSprites) ConfigureSprite(path, size, align);
    }

    private static void Png(string name, int size, Painter painter, SpriteAlignment align = SpriteAlignment.Center)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                Color c = painter((i + 0.5f) / size, (j + 0.5f) / size);
                px[j * size + i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), Mathf.Clamp01(c.a));
            }
        tex.SetPixels(px);
        tex.Apply();
        string path = $"{ArtDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        PendingSprites.Add((path, size, align));
    }

    private static void ConfigureSprite(string path, int size, SpriteAlignment align)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spritePixelsPerUnit = size;            // every sprite is exactly 1 x 1 world unit
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

    // ---------------------------------------------------------------- AUDIO

    private static void GenerateAudio()
    {
        // Soft, short "plip": quick upward chirp with a fast decay.
        Wav("Plip", 0.12f, 0.6f, 0f, t =>
        {
            float ph = ExpChirpPhase(t, 700f, 1150f, 0.05f);
            float env = (1f - Mathf.Exp(-t / 0.003f)) * Mathf.Exp(-t / 0.028f);
            return (Mathf.Sin(ph) + 0.12f * Mathf.Sin(2f * ph)) * env;
        });

        // Glassy bubble pop: rising bubble chirp + short inharmonic "glass" partials.
        Wav("BubblePop", 0.18f, 0.6f, 0f, t =>
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
        Wav("Bloop", 0.5f, 0.7f, 900f, t =>
        {
            float ph = ExpChirpPhase(t, 300f, 95f, 0.4f) + 0.15f * Mathf.Sin(Mathf.PI * 2f * 7f * t);
            float env = (1f - Mathf.Exp(-t / 0.01f)) * Mathf.Exp(-t / 0.14f);
            return (Mathf.Sin(ph) + 0.25f * Mathf.Sin(2f * ph)) * env;
        });
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

    private static void Wav(string name, float seconds, float peak, float lowpassHz, Func<float, float> synth)
    {
        const int rate = 44100;
        int n = Mathf.CeilToInt(seconds * rate);
        var samples = new float[n];
        float max = 0.0001f;
        float lp = 0f;
        float alpha = lowpassHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * lowpassHz / rate) : 1f;
        int fadeOut = Mathf.Min(n, rate * 3 / 1000);

        for (int i = 0; i < n; i++)
        {
            float v = synth(i / (float)rate);
            lp += alpha * (v - lp);
            v = lp;
            if (i >= n - fadeOut) v *= (n - 1 - i) / (float)fadeOut;  // click-free end
            samples[i] = v;
            max = Mathf.Max(max, Mathf.Abs(v));
        }

        string path = $"{AudioDir}/{name}.wav";
        using (var bw = new BinaryWriter(File.Create(path)))
        {
            bw.Write(new[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + n * 2);
            bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            bw.Write(16);
            bw.Write((short)1);       // PCM
            bw.Write((short)1);       // mono
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
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
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

            Transform old = root.transform.Find("Visual");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            Material mat = rootSr.sharedMaterial;
            SpriteRenderer water = CreateSprite("Water", visual.transform, LoadSprite("CapsuleWater"), OrderBrickWater, mat);
            SpriteRenderer glass = CreateSprite("Glass", visual.transform, LoadSprite("GlassCapsule"), OrderBrickGlass, mat);

            var bv = visual.AddComponent<BrickVisualAfter>();
            SetRef(bv, "sourceRenderer", rootSr);
            SetRef(bv, "water", water);
            SetRef(bv, "glass", glass);

            PrefabUtility.SaveAsPrefabAsset(root, BrickAfterPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(BrickAfterPath);
    }

    // ---------------------------------------------------------------- SCENE

    private static void BuildScene(GameObject brickAfter)
    {
        var scene = EditorSceneManager.OpenScene(AfterScenePath, OpenSceneMode.Single);

        GameObject ball = FindRoot(scene, "Ball");
        GameObject paddle = FindRoot(scene, "Paddle");
        GameObject level = FindRoot(scene, "LevelBuilder");
        GameObject camGo = FindRoot(scene, "Main Camera");
        GameObject canvas = FindRoot(scene, "Canvas");

        // Clean previous runs.
        GameObject oldRoot = FindRoot(scene, RootName, required: false);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);
        Transform oldVignette = canvas.transform.Find(VignetteName);
        if (oldVignette != null) Object.DestroyImmediate(oldVignette.gameObject);

        // Ball & paddle: only the sprite on the existing Visual child changes (same 1-unit size as before).
        var ballSr = ball.transform.Find("Visual").GetComponent<SpriteRenderer>();
        ballSr.sprite = LoadSprite("Pearl");
        ballSr.color = Color.white;
        ballSr.sortingOrder = OrderBall;
        EditorUtility.SetDirty(ballSr);

        Transform paddleVisual = paddle.transform.Find("Visual");
        var paddleSr = paddleVisual.GetComponent<SpriteRenderer>();
        paddleSr.sprite = LoadSprite("CoralPaddle");
        paddleSr.color = Color.white;
        paddleSr.sortingOrder = OrderPaddle;
        EditorUtility.SetDirty(paddleSr);

        Material mat = ballSr.sharedMaterial;

        // Bricks: same LevelBuilder, pointing at the After copy of the prefab.
        var builder = level.GetComponent<LevelBuilder>();
        SetRef(builder, "brickPrefab", brickAfter.GetComponent<Brick>());

        // Camera clear colour behind everything.
        var cam = camGo.GetComponent<Camera>();
        cam.backgroundColor = new Color(0.02f, 0.06f, 0.13f, 1f);
        EditorUtility.SetDirty(cam);

        Vector3 camPos = cam.transform.position;
        float halfH = cam.orthographicSize;
        float halfW = halfH * 16f / 9f;

        var root = new GameObject(RootName);

        // Background.
        SpriteRenderer bg = CreateSprite("Background", root.transform, LoadSprite("OceanGradient"), OrderBackground, mat);
        bg.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        bg.transform.localScale = new Vector3(halfH * 2f * 2.2f, halfH * 2f + 2f, 1f);
        bg.color = new Color(0.78f, 0.78f, 0.82f, 1f);

        // Progression decor.
        var decor = new GameObject("Decor").transform;
        decor.SetParent(root.transform, false);
        float bottom = camPos.y - halfH;
        float leftEdge = camPos.x - halfW;
        Vector3[] weeds =
        {
            new Vector3(leftEdge + 0.6f, 1.4f, 3.2f), new Vector3(leftEdge + 1.7f, 1.2f, 2.4f),
            new Vector3(leftEdge + 2.7f, 1.3f, 2.8f), new Vector3(-6.8f, 0.9f, 1.3f), new Vector3(6.8f, 0.9f, 1.2f)
        };
        var weedRenderers = new SpriteRenderer[weeds.Length];
        for (int i = 0; i < weeds.Length; i++)
        {
            SpriteRenderer sr = CreateSprite($"Seaweed_{i}", decor, LoadSprite("Seaweed"), OrderSeaweed, mat);
            sr.transform.position = new Vector3(weeds[i].x, bottom, 0f);
            sr.transform.localScale = new Vector3(weeds[i].y, weeds[i].z, 1f);
            sr.color = new Color(1f, 1f, 1f, 0.08f);
            weedRenderers[i] = sr;
        }
        float[] rayX = { -5f, -0.5f, 4f };
        float[] rayRot = { -12f, -4f, 8f };
        var rayRenderers = new SpriteRenderer[rayX.Length];
        for (int i = 0; i < rayX.Length; i++)
        {
            SpriteRenderer sr = CreateSprite($"LightRay_{i}", decor, LoadSprite("LightRay"), OrderLightRays, mat);
            sr.transform.position = new Vector3(rayX[i], camPos.y + halfH, 0f);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, rayRot[i]);
            sr.transform.localScale = new Vector3(2.2f, 11f, 1f);
            sr.color = new Color(1f, 1f, 1f, 0f);
            rayRenderers[i] = sr;
        }
        var progression = decor.gameObject.AddComponent<OceanProgression>();
        SetRef(progression, "background", bg);
        SetRef(progression, "brickParent", level.transform);
        SetRefs(progression, "seaweed", weedRenderers);
        SetRefs(progression, "lightRays", rayRenderers);

        // Shared particle pool.
        var pool = AddChild<FxSpritePool>(root.transform, "FxPool");
        SetRef(pool, "spriteMaterial", mat);

        // Creatures.
        var creatures = AddChild<CreatureDrifter>(root.transform, "Creatures");
        SetRefs(creatures, "creatureSprites", new Object[] { LoadSprite("Jellyfish"), LoadSprite("Fish"), LoadSprite("Seahorse") });
        SetRef(creatures, "spriteMaterial", mat);
        SetInt(creatures, "sortingOrder", OrderCreatures);

        // 1. Brick burst.
        var burst = AddChild<BrickBurstFeedback>(root.transform, "BrickBurst");
        SetRef(burst, "pool", pool);
        SetRef(burst, "bubbleSprite", LoadSprite("Bubble"));
        SetRef(burst, "dropletSprite", LoadSprite("Droplet"));
        SetRef(burst, "ringSprite", LoadSprite("RippleRing"));
        SetRef(burst, "creatures", creatures);

        // 2. Paddle squash + ripple.
        var squash = AddChild<PaddleSquashFeedback>(root.transform, "PaddleSquash");
        SetRef(squash, "visual", paddleVisual);
        SetRef(squash, "pool", pool);
        SetRef(squash, "rippleSprite", LoadSprite("RippleRing"));

        // 3. Audio.
        var audio = AddChild<OceanAudioFeedback>(root.transform, "Audio");
        SetRef(audio, "paddlePlip.clip", LoadClip("Plip"));
        SetRef(audio, "brickPop.clip", LoadClip("BubblePop"));
        SetRef(audio, "ballLostBloop.clip", LoadClip("Bloop"));

        // 5a. Ball trail.
        var trail = AddChild<BallTrailFeedback>(root.transform, "BallTrail");
        SetRef(trail, "target", ballSr.transform);
        SetRef(trail, "pool", pool);
        SetRef(trail, "bubbleSprite", LoadSprite("Bubble"));

        // UI: vignette as the first Canvas child so HUD text draws on top. Never blocks raycasts.
        var vig = new GameObject(VignetteName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vig.layer = canvas.layer;
        vig.transform.SetParent(canvas.transform, false);
        vig.transform.SetSiblingIndex(0);
        var vrt = (RectTransform)vig.transform;
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        var img = vig.GetComponent<Image>();
        img.sprite = LoadSprite("Vignette");
        img.color = new Color(0.01f, 0.03f, 0.10f, 0f);
        img.raycastTarget = false;

        // 4. Score pop and 5b. life-loss cue.
        var scoreRect = (RectTransform)canvas.transform.Find("ScoreText");
        var livesGraphic = canvas.transform.Find("LivesText").GetComponent<Graphic>();
        var ui = new GameObject("UIFeedback").transform;
        ui.SetParent(root.transform, false);
        var scorePop = ui.gameObject.AddComponent<ScorePopFeedback>();
        SetRef(scorePop, "target", scoreRect);
        var lifeLoss = ui.gameObject.AddComponent<LifeLossFeedback>();
        SetRef(lifeLoss, "livesText", livesGraphic);
        SetRef(lifeLoss, "vignette", img);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new Exception("[PocketOcean] Failed to save After.unity.");
    }

    // ------------------------------------------------------------- VALIDATE

    private static void Validate(GameObject brickAfter)
    {
        var errors = new List<string>();

        // Decoration must have no colliders.
        var scene = EditorSceneManager.GetActiveScene();
        GameObject root = FindRoot(scene, RootName);
        if (root.GetComponentsInChildren<Collider2D>(true).Length > 0) errors.Add("Collider found under PocketOcean.");
        if (brickAfter.transform.Find("Visual").GetComponentsInChildren<Collider2D>(true).Length > 0)
            errors.Add("Collider found on Brick_After visual.");

        // Brick_After must keep Brick's physics and values.
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

        // Visual sprites must be exactly 1 x 1 unit so ball/paddle visuals match their colliders.
        foreach (string s in new[] { "Pearl", "CoralPaddle", "GlassCapsule", "CapsuleWater" })
        {
            Vector3 size = LoadSprite(s).bounds.size;
            if (Mathf.Abs(size.x - 1f) > 0.001f || Mathf.Abs(size.y - 1f) > 0.001f)
                errors.Add($"{s} sprite is {size.x}x{size.y}, expected 1x1.");
        }

        GameObject paddle = FindRoot(scene, "Paddle");
        var paddleCol = paddle.GetComponent<BoxCollider2D>();
        var paddleSr = paddle.transform.Find("Visual").GetComponent<SpriteRenderer>();
        float colW = paddleCol.size.x * paddle.transform.lossyScale.x;
        float visW = paddleSr.sprite.bounds.size.x * paddleSr.transform.lossyScale.x;
        if (Mathf.Abs(colW - visW) > 0.001f) errors.Add($"Paddle visual width {visW} != collider width {colW}.");

        GameObject ball = FindRoot(scene, "Ball");
        var ballCol = ball.GetComponent<CircleCollider2D>();
        var ballSr = ball.transform.Find("Visual").GetComponent<SpriteRenderer>();
        float colD = ballCol.radius * 2f * ball.transform.lossyScale.x;
        float visD = ballSr.sprite.bounds.size.x * ballSr.transform.lossyScale.x;
        if (Mathf.Abs(colD - visD) > 0.001f) errors.Add($"Ball visual diameter {visD} != collider diameter {colD}.");

        if (errors.Count > 0)
            throw new Exception("[PocketOcean] Validation failed:\n" + string.Join("\n", errors));

        Debug.Log($"[PocketOcean] Validation OK. Paddle visual {visW} = collider {colW}; ball visual {visD} = collider {colD}; " +
                  "no decorative colliders; Brick_After physics and values match Brick.");
    }

    // -------------------------------------------------------------- HELPERS

    private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name, bool required = true)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        if (required) throw new Exception($"[PocketOcean] '{name}' not found in {scene.path}.");
        return null;
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
