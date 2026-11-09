using TMPro;
using UnityEngine;

/// <summary>
/// Skor ve can göstergesi.
/// Juice katmanı devredeyken skor yazısının sahipliğini ona devrediyor —
/// iki sistemin aynı alana yazması juice mimarilerinde klasik bir çakışma kaynağı.
/// </summary>
public class HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;

    /// <summary>Bir juice modülü skoru devraldıysa HUD ona karışmaz.</summary>
    public bool ScoreHandledExternally { get; set; }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.LivesChanged += OnLivesChanged;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.LivesChanged -= OnLivesChanged;
    }

    private void Start()
    {
        RefreshScore();
        OnLivesChanged(GameManager.Instance.Lives);
    }

    /// <summary>Sahiplik geri alındığında güncel değeri anında yazmak için.</summary>
    public void RefreshScore()
    {
        if (GameManager.Instance != null)
            scoreText.text = $"SCORE {GameManager.Instance.Score}";
    }

    private void OnScoreChanged(int score)
    {
        if (ScoreHandledExternally) return;
        scoreText.text = $"SCORE {score}";
    }

    private void OnLivesChanged(int lives) => livesText.text = $"LIVES {lives}";
}