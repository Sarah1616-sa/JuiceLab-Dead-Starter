# SWE402 Game Feel Submission

Read [START-HERE.md](START-HERE.md) to open the starter. Replace this README with your submission information.

## Student
Name and student ID:sarah almas-202261060

## Run
Unity version: 6000.5.2f1

Scenes, controls, and instructions:

## Intended feel

## Improvements
| Improvement | Trigger | Implementation file/object | Purpose |
|---|---|---|---|
| Brick impact | `BrickDestroyed` | `Scripts/Feedback/BrickBurstFeedback.cs` + `FxSpritePool.cs` + `CreatureDrifter.cs` on `PocketOcean/BrickBurst`, `FxPool`, `Creatures` | Pooled burst of bubbles, splash droplets, a splash ring and a few quickly fading glass shards in the brick's colour (0.2–0.7 s). The creature that lived inside the capsule (fish, coral, seahorse or jellyfish) is released and drifts away behind the playfield (max 6 alive). Makes every break feel like popping a tiny aquarium. |
| Reactive animation | `BallHitPaddle` | `Scripts/Feedback/PaddleSquashFeedback.cs` on `PocketOcean/PaddleSquash` (animates `Paddle/Visual` only) | The glass paddle visual squashes wider and shorter, then springs back to its stored original scale. A soft, flattened water ripple spreads from the contact point. Makes the paddle feel soft and bouncy. |
| Event-based audio | `BallHitPaddle`, `BrickDestroyed`, `BallLost` (+ music) | `Scripts/Feedback/OceanAudioFeedback.cs` on `PocketOcean/Audio`; `Scripts/Feedback/BackgroundMusicFeedback.cs` on `BackgroundMusic` | Soft "plip", glassy bubble pop and low muffled "bloop", played from a pool of 6 AudioSources with ±5 % pitch variation and a minimum repeat interval per sound. A calm ambient loop fades in over 2 s and briefly ducks on `BallLost` so the bloop stands out (Music Enabled toggle in the Inspector). |
| Interface feedback | `ScoreChanged` | `Scripts/Feedback/ScorePopFeedback.cs` on `PocketOcean/UIFeedback` | The light-aqua score (soft glow, wave line underneath) pops to 1.2× and settles to 1.0× with a small overshoot. Only its scale changes; the HUD still writes the score, so the value is always correct. |
| Additional technique | Ball movement; `BallLost` + `LivesChanged` | `Scripts/Feedback/BallTrailFeedback.cs` (`PocketOcean/BallTrail`); `Scripts/Feedback/LivesShellsFeedback.cs` (`Canvas/LivesText/PocketOcean_LivesShells`); `Scripts/Feedback/LifeLossFeedback.cs` + `Canvas/PocketOcean_Vignette` | A short trail of real-looking bubbles behind the pearl. Lives are shown as seashells (one per life, always matching the real value, including the reset after the last life). On life loss the lost shell pops away, the row flashes red to blue and wobbles, and a dark edge vignette pulses for under 0.5 s, so ball loss is visible even with sound muted. |

Theme visuals (After scene only): `Brick_After.prefab` (pastel glass capsule tinted with the original row colour, with a small creature inside in a fixed pattern), pearl ball and aqua glass paddle on the existing `Visual` children (fitted exactly to the colliders), frosted wall visuals, a centred camera and a mirrored teal ocean background with light rays, seaweed and coral on both sides. `OceanProgression.cs` slowly brightens the water and light rays as bricks are cleared. Setup is reproducible through `Tools > Pocket Ocean > Setup After Scene` (`Scripts/Feedback/Editor/PocketOceanSetup.cs`); running it again replaces its own objects without duplicates.

## Playtest and revision
Observation, change, and result:## Playtest and revision
Friends played Before and then After and were asked which felt more satisfying and why, which feedback made events clearer, and whether anything was distracting or hard to notice.

- **What they noticed:** All testers said After felt much better than Before. They pointed to the visuals and the sound together: the glass capsules popping with bubbles and a sound made breaking bricks satisfying, and the overall underwater look and audio made them want to keep playing. Before felt flat and silent in comparison. When asked about distractions, one tester said the released creatures were sometimes bright and large enough to pull attention away from the ball, especially when several bricks broke at once.
- **What I changed:** On `PocketOcean/Creatures` (`CreatureDrifter.cs`), I reduced the creature size from 0.8 to 0.6 of a brick's height and lowered the glow intensity, so creatures stay a background reward instead of competing with the pearl.
- **Result after the change:** On replay, the tester could follow the ball clearly during rapid brick breaks, and the creatures were still noticeable as a small reward after each break.

## Demonstration video
Link: https://drive.google.com/file/d/1ZwLy0caC9iF2m7S8r1xBJXhHtz11_J7o/view?usp=sharing

## AI use
Tools, contributions, and how you tested/revised the output (or state no AI used):- Tuned: background music volume so the event sounds stay clear, released creature size and glow after playtesting, and the creature sorting order so they appear above the background but below the ball and paddle.

## Credits
Starter adapted from [JuiceLab](https://github.com/serkoceyhan/JuiceLab) by Serkan Ceyhan, MIT. See LICENSE and SOURCE.md.

Pocket Ocean assets were all generated procedurally by code in `Assets/_Project/Scripts/Feedback/Editor/PocketOceanSetup.cs`. That code was written with AI assistance (Claude). No third-party art, audio, fonts or feedback packages are used; the score uses the starter's LiberationSans font with a custom TMP material.
- Sprites (`Assets/_Project/Art/PocketOcean/`): OceanGradient, Pearl, AquaPaddle, GlassCapsule, GlassHighlight, GlassShard, Fish, Coral, Seahorse, Jellyfish, Bubble, Droplet, RippleRing, Seaweed, LightRay, Seashell, WaveLine, WallFrost, Vignette.
- Material: `PocketOcean_ScoreText.mat` (copy of the LiberationSans SDF material with a soft glow).
- Sounds (`Assets/_Project/Audio/PocketOcean/`): Plip, BubblePop, Bloop (synthesised sine chirps with envelopes) and OceanAmbience (synthesised 16 s ambient pad loop).
- Prefab: `Assets/_Project/Prefabs/Brick_After.prefab` (copy of the starter's Brick with a visual child).
