// OceanAudioFeedback
// Trigger: GameEvents.BallHitPaddle (soft "plip"), GameEvents.BrickDestroyed (glassy bubble pop),
//          GameEvents.BallLost (low, muffled "bloop").
// Purpose: event-based underwater audio from a small AudioSource pool. Each play gets a slight random pitch
// variation, and each sound has a minimum interval between repeats so rapid hits stay balanced.
using System;
using UnityEngine;

public class OceanAudioFeedback : MonoBehaviour
{
    [Serializable]
    private class Sound
    {
        [Tooltip("Clip to play.")]
        public AudioClip clip;
        [Tooltip("Volume for this sound (multiplied by the master volume).")]
        [Range(0f, 1f)] public float volume = 0.5f;
        [Tooltip("Minimum seconds between two plays of this sound. Extra requests inside this window are skipped.")]
        [Min(0f)] public float minInterval = 0.05f;
        [NonSerialized] public float lastPlayed = float.NegativeInfinity;
    }

    [Header("Sounds")]
    [Tooltip("Ball hits paddle: soft, short plip.")]
    [SerializeField] private Sound paddlePlip = new Sound { volume = 0.45f, minInterval = 0.06f };
    [Tooltip("Brick destroyed: glassy bubble pop.")]
    [SerializeField] private Sound brickPop = new Sound { volume = 0.55f, minInterval = 0.05f };
    [Tooltip("Ball lost: low, muffled bloop.")]
    [SerializeField] private Sound ballLostBloop = new Sound { volume = 0.7f, minInterval = 0.3f };

    [Header("Mix")]
    [Tooltip("Overall volume of all Pocket Ocean sounds.")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [Tooltip("Random pitch variation, e.g. 0.05 = +/-5 %.")]
    [SerializeField, Range(0f, 0.2f)] private float pitchVariation = 0.05f;
    [Tooltip("Number of pooled AudioSources (how many sounds can overlap).")]
    [SerializeField, Min(1)] private int poolSize = 6;

    private AudioSource[] sources;
    private int cursor;

    private void Awake()
    {
        sources = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            sources[i] = src;
        }

        paddlePlip.lastPlayed = float.NegativeInfinity;
        brickPop.lastPlayed = float.NegativeInfinity;
        ballLostBloop.lastPlayed = float.NegativeInfinity;
    }

    private void OnEnable()
    {
        GameEvents.BallHitPaddle += OnBallHitPaddle;
        GameEvents.BrickDestroyed += OnBrickDestroyed;
        GameEvents.BallLost += OnBallLost;
    }

    private void OnDisable()
    {
        GameEvents.BallHitPaddle -= OnBallHitPaddle;
        GameEvents.BrickDestroyed -= OnBrickDestroyed;
        GameEvents.BallLost -= OnBallLost;
    }

    private void OnBallHitPaddle(ImpactInfo _) => Play(paddlePlip);
    private void OnBrickDestroyed(ImpactInfo _) => Play(brickPop);
    private void OnBallLost() => Play(ballLostBloop);

    private void Play(Sound sound)
    {
        if (sound.clip == null || sources == null) return;

        float now = Time.unscaledTime;
        if (now - sound.lastPlayed < sound.minInterval) return;
        sound.lastPlayed = now;

        AudioSource src = NextSource();
        src.clip = sound.clip;
        src.volume = sound.volume * masterVolume;
        src.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
        src.Play();
    }

    private AudioSource NextSource()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            int idx = (cursor + i) % sources.Length;
            if (!sources[idx].isPlaying)
            {
                cursor = (idx + 1) % sources.Length;
                return sources[idx];
            }
        }

        // All busy: steal the next one in round-robin order.
        AudioSource stolen = sources[cursor];
        cursor = (cursor + 1) % sources.Length;
        return stolen;
    }
}
