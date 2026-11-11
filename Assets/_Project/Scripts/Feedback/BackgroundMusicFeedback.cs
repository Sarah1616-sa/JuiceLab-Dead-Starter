// BackgroundMusicFeedback
// Trigger: Start (fade-in) and GameEvents.BallLost (duck). Lives on the BackgroundMusic object in After.unity.
// Purpose: calm underwater music that fades in over ~2 s at start. When the ball is lost the music briefly dips
// (ducks) so the ball-loss "bloop" stands out, then recovers smoothly over ~1 s. "Music Enabled" mutes only this
// AudioSource; event sounds play from OceanAudioFeedback's own sources and are not affected.
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicFeedback : MonoBehaviour
{
    [Tooltip("Turn the background music on or off. Event sounds are not affected.")]
    [SerializeField] private bool musicEnabled = true;
    [Tooltip("Normal music volume (0-1).")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
    [Tooltip("Seconds to fade the music in when the scene starts.")]
    [SerializeField, Min(0f)] private float fadeInTime = 2f;

    [Header("Duck on ball lost")]
    [Tooltip("How much the music drops when the ball is lost (0 = no duck, 1 = silent).")]
    [SerializeField, Range(0f, 1f)] private float duckAmount = 0.6f;
    [Tooltip("Seconds to dip down to the ducked volume.")]
    [SerializeField, Min(0f)] private float duckAttackTime = 0.08f;
    [Tooltip("Seconds to stay ducked before recovering.")]
    [SerializeField, Min(0f)] private float duckHoldTime = 0.25f;
    [Tooltip("Seconds to return smoothly to normal volume after the duck.")]
    [SerializeField, Min(0.01f)] private float duckRecoveryTime = 1f;

    private AudioSource source;
    private float fadeElapsed;
    private float duckElapsed = float.PositiveInfinity;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    private void OnEnable() => GameEvents.BallLost += OnBallLost;
    private void OnDisable() => GameEvents.BallLost -= OnBallLost;

    private void Start()
    {
        fadeElapsed = 0f;
        if (source.clip != null && !source.isPlaying) source.Play();
        Apply();
    }

    private void OnBallLost() => duckElapsed = 0f;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        fadeElapsed += dt;
        duckElapsed += dt;
        Apply();
    }

    private void Apply()
    {
        source.mute = !musicEnabled;

        float fade = fadeInTime > 0f ? Mathf.Clamp01(fadeElapsed / fadeInTime) : 1f;
        fade = fade * fade * (3f - 2f * fade);   // smooth start and end
        source.volume = musicVolume * fade * DuckFactor();
    }

    private float DuckFactor()
    {
        float t = duckElapsed;
        float low = 1f - duckAmount;
        if (float.IsInfinity(t)) return 1f;
        if (t < duckAttackTime) return Mathf.Lerp(1f, low, t / duckAttackTime);
        t -= duckAttackTime;
        if (t < duckHoldTime) return low;
        t -= duckHoldTime;
        if (t < duckRecoveryTime)
        {
            float u = t / duckRecoveryTime;
            u = u * u * (3f - 2f * u);
            return Mathf.Lerp(low, 1f, u);
        }
        return 1f;
    }
}
