using UnityEngine;

/// <summary>
/// Tek bir tuğla. Yok olduğunda olayı duyurur; hangi efektin tetikleneceğini bilmez.
/// Ölüm animasyonu juice katmanındaki BrickDeathModule'e ait —
/// bu sayede efektler oyun mantığını bir kare bile geciktirmez.
/// </summary>
public class Brick : MonoBehaviour
{
    [SerializeField] private int hitPoints = 1;
    [SerializeField] private int scoreValue = 10;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out Ball _)) return;

        ContactPoint2D contact = collision.GetContact(0);
        TakeHit(contact.point, contact.normal);
    }

    private void TakeHit(Vector2 point, Vector2 normal)
    {
        hitPoints--;
        if (hitPoints > 0) return;

        GameManager.Instance.AddScore(scoreValue);

        Color color = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Vector2 size = spriteRenderer != null
            ? (Vector2)spriteRenderer.bounds.size
            : Vector2.one;

        int combo = GameManager.Instance.RegisterBrickCombo();

        // Merkezi ve boyutu şimdi kopyalıyoruz: bir satır sonra bu nesne yok olacak.
        GameEvents.RaiseBrickDestroyed(new ImpactInfo(
            point, normal, 1f, color, transform.position, size, combo));

        Destroy(gameObject);
    }
}