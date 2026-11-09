# SWE402 — JuiceLab Dead Starter

A deliberately unpolished brick-breaker, adapted from Serkan Ceyhan's MIT-licensed JuiceLab. See SOURCE.md and LICENSE. Completed feedback implementations are removed, not just disabled.

## Open and play

1. Extract this whole project and add its root folder in Unity Hub.
2. Use Unity **6000.3.2f1**. Let packages and assets import.
3. Open `Assets/_Project/Scenes/Before.unity`.
4. Set Game view to 16:9 and press Play. Focus Game view.
5. Move using the mouse, A/D, or left/right arrows. The ball launches automatically.

Bricks disappear and add points. Missing the ball reduces lives and relaunches it. Losing all lives resets score and lives but does not rebuild the board. Stop and restart Play mode for a fresh board. No sound, particles, trail, shake, hit-stop, animated score, or squash/stretch is included.

## Start the assignment

Duplicate Before.unity as After.unity. Keep Before unchanged. Add new components in After; duplicate shared assets before modifying them. The separate assignment brief contains requirements and grading, including AI permission and the top-three bonus.

## Code map

- `Scripts/Gameplay`: paddle, ball, brick, death zone, board generation.
- `Scripts/Core/GameManager.cs`: score, lives, combo.
- `Scripts/Core/GameEvents.cs`: event hooks for adding feedback.
- `Scripts/Core/ImpactInfo.cs`: collision position, normal, color and other event data.
- `Scripts/Core/HUD.cs`: plain score/lives text. Its score ownership property supports your future UI animation.

All paths above are relative to `Assets/_Project`.

Subscribe to events when enabled and unsubscribe when disabled. Preserve the gameplay rules and physics. Animate visual children rather than the collision objects. The upstream comments are in Turkish; identifiers and this guide are in English.
