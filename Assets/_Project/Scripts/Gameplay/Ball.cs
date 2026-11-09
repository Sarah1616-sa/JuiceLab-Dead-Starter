using UnityEngine;

/// <summary>
/// Topun hareketi. Fizik motoru sekmeyi yapar; biz hızın sabit kalmasını,
/// açının bozulmamasını garanti eder ve çarpışmaları duyururuz.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
    [Header("Hız")]
    [SerializeField] private float speed = 9f;

    [Header("Açı kontrolü")]
    [Tooltip("Raketin kenarına çarpınca oluşacak maksimum sapma açısı.")]
    [SerializeField] private float maxBounceAngle = 60f;

    [Tooltip("Topun dikey bileşeninin alt sınırı. Yataya yapışmasını engeller.")]
    [SerializeField] private float minVerticalRatio = 0.25f;

    private Rigidbody2D rb;
    private Vector2 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void Start()
    {
        Launch();
    }

    /// <summary>Topu başlangıç noktasına alıp yukarı doğru rastgele bir açıyla fırlatır.</summary>
    public void Launch()
    {
        transform.position = startPosition;

        float angle = Random.Range(-45f, 45f);
        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
        rb.linearVelocity = dir * speed;
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude < 0.0001f) return;

        Vector2 dir = velocity.normalized;

        // Top neredeyse yatay giderse duvarlar arasında sonsuza kadar sekip
        // oyun kilitlenir. Dikey bileşene taban koyuyoruz.
        if (Mathf.Abs(dir.y) < minVerticalRatio)
        {
            float sign = dir.y >= 0f ? 1f : -1f;
            dir.y = sign * minVerticalRatio;
            dir = dir.normalized;
        }

        // Fizik materyali tam 1.0 bounciness'ta bile zamanla hız kaybettirir.
        rb.linearVelocity = dir * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // collision.contacts[] her çağrıda yeni dizi ayırır (çöp üretir).
        // GetContact(0) ayırma yapmaz.
        ContactPoint2D contact = collision.GetContact(0);
        var info = new ImpactInfo(contact.point, contact.normal);

        if (collision.gameObject.TryGetComponent(out Paddle paddle))
        {
            BounceFromPaddle(paddle);
            GameManager.Instance.ResetCombo();   // rakete değdi, seri bitti
            GameEvents.RaiseBallHitPaddle(info);
        }
        else if (!collision.gameObject.TryGetComponent(out Brick _))
        {
            // Tuğla kendi ölümünü kendi duyuruyor; burada sadece duvarlar kalıyor.
            GameEvents.RaiseBallHitWall(info);
        }
    }

    /// <summary>
    /// Arkanoid'den beri değişmeyen kural: topun rakete nereye çarptığı
    /// sekme açısını belirler. Ortadan çarparsa dik, kenardan çarparsa yatık.
    /// Oyuncuya kontrol hissi veren şey bu.
    /// </summary>
    private void BounceFromPaddle(Paddle paddle)
    {
        float halfWidth = paddle.Width * 0.5f;
        float offset = (transform.position.x - paddle.transform.position.x) / halfWidth;
        offset = Mathf.Clamp(offset, -1f, 1f);

        float angle = offset * maxBounceAngle;
        Vector2 dir = Quaternion.Euler(0f, 0f, -angle) * Vector2.up;

        rb.linearVelocity = dir * speed;
    }
}