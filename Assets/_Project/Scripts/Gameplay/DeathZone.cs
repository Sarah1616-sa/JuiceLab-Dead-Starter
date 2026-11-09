using UnityEngine;

/// <summary>Ekranın altındaki tetikleyici. Top buraya girerse can gider.</summary>
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Ball _))
        {
            GameManager.Instance.OnBallLost();
        }
    }
}