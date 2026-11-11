# SWE402 Game Feel Submission

Read [START-HERE.md](START-HERE.md) to open the starter. Replace this README with your submission information.

## Student
Name and student ID:

## Run
Unity version: 6000.3.2f1

Scenes, controls, and instructions:

## Intended feel

## Improvements
| Improvement | Trigger | Implementation file/object | Purpose |
|---|---|---|---|
| Brick impact | `BrickDestroyed` | `Scripts/Feedback/BrickBurstFeedback.cs` + `FxSpritePool.cs` + `CreatureDrifter.cs` on `PocketOcean/BrickBurst`, `FxPool`, `Creatures` | Pooled burst of bubbles, splash droplets and a splash ring in the brick's colour (0.4–0.7 s). Sometimes a tiny jellyfish, fish or seahorse drifts away behind the playfield (max 6 alive). Makes every break feel like popping a water capsule. |
| Reactive animation | `BallHitPaddle` | `Scripts/Feedback/PaddleSquashFeedback.cs` on `PocketOcean/PaddleSquash` (animates `Paddle/Visual` only) | The paddle visual squashes wider and shorter, then springs back to its stored original scale. A water ripple ring spreads from the contact point. Makes the paddle feel soft and bouncy. |
| Event-based audio | `BallHitPaddle`, `BrickDestroyed`, `BallLost` | `Scripts/Feedback/OceanAudioFeedback.cs` on `PocketOcean/Audio` | Soft "plip", glassy bubble pop and low muffled "bloop", played from a pool of 6 AudioSources. Each play gets ±5 % pitch variation, and each sound has a minimum repeat interval so rapid hits stay balanced. |
| Interface feedback | `ScoreChanged` | `Scripts/Feedback/ScorePopFeedback.cs` on `PocketOcean/UIFeedback` | The score text pops to 1.2× and settles to 1.0× with a small overshoot. Only its scale changes; the HUD still writes the score, so the value is always correct. |
| Additional technique | Ball movement; `BallLost` + `LivesChanged` | `Scripts/Feedback/BallTrailFeedback.cs` (`PocketOcean/BallTrail`), `Scripts/Feedback/LifeLossFeedback.cs` (`PocketOcean/UIFeedback`) + `Canvas/PocketOcean_Vignette` | A faint bubble trail behind the pearl. On life loss, the lives text flashes red to blue and wobbles, and a dark edge vignette pulses for under 0.5 s, so ball loss is visible even with sound muted. |

Theme visuals (After scene only): `Brick_After.prefab` (glass-capsule visual child tinted with the original row colour), pearl ball sprite and coral paddle sprite on the existing `Visual` children (same 1-unit size as the colliders), ocean gradient background, and `OceanProgression.cs` (background slowly brightens and seaweed and light rays fade in as bricks are cleared). Setup is reproducible through `Tools > Pocket Ocean > Setup After Scene` (`Scripts/Feedback/Editor/PocketOceanSetup.cs`).

## Playtest and revision
Observation, change, and result:

## Demonstration video
Link:

## AI use
Tools, contributions, and how you tested/revised the output (or state no AI used):

## Credits
Starter adapted from [JuiceLab](https://github.com/serkoceyhan/JuiceLab) by Serkan Ceyhan, MIT. See LICENSE and SOURCE.md.

Pocket Ocean assets were all generated procedurally by code in `Assets/_Project/Scripts/Feedback/Editor/PocketOceanSetup.cs`. That code was written with AI assistance (Claude). No third-party art, audio or feedback packages are used.
- Sprites (`Assets/_Project/Art/PocketOcean/`): OceanGradient, Pearl, CoralPaddle, GlassCapsule, CapsuleWater, Bubble, Droplet, RippleRing, Jellyfish, Fish, Seahorse, Seaweed, LightRay, Vignette.
- Sounds (`Assets/_Project/Audio/PocketOcean/`): Plip, BubblePop, Bloop (synthesised sine chirps with envelopes).
- Prefab: `Assets/_Project/Prefabs/Brick_After.prefab` (copy of the starter's Brick with a visual child).
