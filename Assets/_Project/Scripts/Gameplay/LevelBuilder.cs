using UnityEngine;

/// <summary>
/// Tuğla ızgarasını kodla üretir. Elle 28 tuğla dizmek yerine
/// satır/sütun sayısını Inspector'dan değiştirip anında deneyebilirsin.
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    [Header("Izgara")]
    [SerializeField] private Brick brickPrefab;
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 4;
    [SerializeField] private Vector2 brickSize = new Vector2(2f, 0.7f);
    [SerializeField] private Vector2 spacing = new Vector2(0.15f, 0.15f);
    [SerializeField] private float topY = 4f;

    [Header("Renkler")]
    [SerializeField] private Color[] rowColors;

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        float stepX = brickSize.x + spacing.x;
        float stepY = brickSize.y + spacing.y;

        // Izgarayı ekranda ortalamak için toplam genişliğin yarısı kadar sola kaydır.
        float totalWidth = columns * stepX - spacing.x;
        float startX = -totalWidth * 0.5f + brickSize.x * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * stepX,
                    topY - row * stepY,
                    0f);

                Brick brick = Instantiate(brickPrefab, pos, Quaternion.identity, transform);
                brick.name = $"Brick_r{row}_c{col}";
                brick.transform.localScale = new Vector3(brickSize.x, brickSize.y, 1f);

                if (rowColors.Length > 0 && brick.TryGetComponent(out SpriteRenderer sr))
                {
                    Color c = rowColors[row % rowColors.Length];

                    // Unity'de Color dizisine yeni eklenen eleman (0,0,0,0) olarak doğar
                    // ve hex alanına RGB yazmak alpha'yı değiştirmez. Görünmez tuğla tuzağı.
                    if (c.a <= 0.01f) c.a = 1f;

                    sr.color = c;
                }
            }
        }
    }
}