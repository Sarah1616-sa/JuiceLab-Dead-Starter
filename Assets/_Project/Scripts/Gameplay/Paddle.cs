using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Raket kontrolü. Fare varsa fareyi, klavye basılıysa klavyeyi takip eder.
/// Görsel bir çocuk objede yaşar; juice modülleri sadece ona dokunur.
/// </summary>
public class Paddle : MonoBehaviour
{
    [SerializeField] private float xLimit = 7.8f;
    [SerializeField] private float followSpeed = 40f;

    private Camera cam;
    private SpriteRenderer sr;
    private float width;

    /// <summary>
    /// Raketin dünya birimi cinsinden genişliği.
    /// Awake'te bir kez ölçülüyor: squash efekti görseli ezerken
    /// topun sekme açısı hesabının titrememesi için.
    /// </summary>
    public float Width => width;

    private void Awake()
    {
        cam = Camera.main;

        // Görsel artık çocuk objede, bu yüzden InChildren.
        sr = GetComponentInChildren<SpriteRenderer>();
        width = sr != null ? sr.bounds.size.x : transform.localScale.x;
    }

    private void Update()
    {
        float targetX = Mathf.Clamp(GetTargetX(), -xLimit, xLimit);

        Vector3 pos = transform.position;
        // Anında ışınlanmak yerine sınırlı hızla takip: raketin bir ağırlığı olsun.
        pos.x = Mathf.MoveTowards(pos.x, targetX, followSpeed * Time.deltaTime);
        transform.position = pos;
    }

    private float GetTargetX()
    {
        float keyboard = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(keyboard) > 0.01f)
        {
            // Uzak bir hedef ver; hız sınırını MoveTowards zaten koyuyor.
            return transform.position.x + keyboard * 100f;
        }

        // İmleç UI'nin üzerindeyken raket hareket etmesin:
        // demo panelinde slider sürüklerken raketin fırlamasını engelliyor.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return transform.position.x;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        return mouseWorld.x;
    }
}