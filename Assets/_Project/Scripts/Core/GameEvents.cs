using System;
using UnityEngine;

/// <summary>
/// Oyun mantığı ile his katmanı arasındaki TEK temas noktası.
/// Gameplay kodu buraya "ne olduğunu" duyurur; kimin dinlediğini bilmez.
/// Bu sayede juice modülleri oyun koduna hiç dokunmadan eklenip çıkarılabilir.
/// </summary>
public static class GameEvents
{
    public static event Action<ImpactInfo> BallHitPaddle;
    public static event Action<ImpactInfo> BallHitWall;
    public static event Action<ImpactInfo> BrickDestroyed;
    public static event Action BallLost;
    public static event Action<int> ScoreChanged;
    public static event Action<int> LivesChanged;

    public static void RaiseBallHitPaddle(ImpactInfo info) => BallHitPaddle?.Invoke(info);
    public static void RaiseBallHitWall(ImpactInfo info)   => BallHitWall?.Invoke(info);
    public static void RaiseBrickDestroyed(ImpactInfo info) => BrickDestroyed?.Invoke(info);
    public static void RaiseBallLost()                     => BallLost?.Invoke();
    public static void RaiseScoreChanged(int score)        => ScoreChanged?.Invoke(score);
    public static void RaiseLivesChanged(int lives)        => LivesChanged?.Invoke(lives);

    /// <summary>
    /// Statik olaylar sahne yeniden yüklendiğinde kendiliğinden temizlenmez.
    /// "Enter Play Mode Options" ile domain reload kapatılırsa eski aboneler
    /// bir sonraki oyuna sızar ve efektler iki kez tetiklenir.
    /// Bu metot her oyun başlangıcında listeyi sıfırlar.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        BallHitPaddle = null;
        BallHitWall = null;
        BrickDestroyed = null;
        BallLost = null;
        ScoreChanged = null;
        LivesChanged = null;
    }
}