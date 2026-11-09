using UnityEngine;

/// <summary>
/// Skor, can ve combo sayacı. Tek sahnelik bir proje olduğu için basit singleton yeterli.
/// Durum değiştiğinde olay yayınlar; kimin dinlediğini bilmez.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Ball ball;
    [SerializeField] private int startingLives = 3;

    public int Score { get; private set; }
    public int Lives { get; private set; }

    /// <summary>Rakete değmeden arka arkaya kırılan tuğla sayısı.</summary>
    public int Combo { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Lives = startingLives;
    }

    public void AddScore(int amount)
    {
        Score += amount;
        GameEvents.RaiseScoreChanged(Score);
    }

    /// <summary>Combo'yu bir artırır ve yeni değeri döndürür.</summary>
    public int RegisterBrickCombo() => ++Combo;

    public void ResetCombo() => Combo = 0;

    public void OnBallLost()
    {
        Lives--;
        ResetCombo();

        if (Lives <= 0)
        {
            Lives = startingLives;
            Score = 0;
            GameEvents.RaiseScoreChanged(Score);
        }

        GameEvents.RaiseLivesChanged(Lives);
        GameEvents.RaiseBallLost();

        ball.Launch();
    }
}