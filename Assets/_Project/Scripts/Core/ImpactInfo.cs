using UnityEngine;

/// <summary>
/// Bir çarpışmanın his katmanına anlatılması gereken her şeyi taşır.
/// readonly struct: her olayda heap'te çöp üretmez, GC'yi tetiklemez.
/// </summary>
public readonly struct ImpactInfo
{
    /// <summary>Çarpmanın dünya koordinatı — partikül buradan püskürür.</summary>
    public readonly Vector2 Point;

    /// <summary>Çarpma yüzeyinin normali — efektlerin yönünü belirler.</summary>
    public readonly Vector2 Normal;

    /// <summary>Olayın ağırlığı, 0-1. Efektler büyüklüklerini bununla ölçekler.</summary>
    public readonly float Strength;

    /// <summary>Kaynağın rengi — partikül ve flash rengi için.</summary>
    public readonly Color Color;

    /// <summary>Kaynağın merkezi. Nesne yok edildikten sonra da lazım olduğu için
    /// referans değil değer olarak kopyalanıyor.</summary>
    public readonly Vector2 Center;

    /// <summary>Kaynağın dünya birimi cinsinden boyutu.</summary>
    public readonly Vector2 Size;

    /// <summary>Rakete değmeden arka arkaya kaçıncı vuruş. Ses pitch'i için.</summary>
    public readonly int Combo;

    public ImpactInfo(Vector2 point, Vector2 normal, float strength = 1f,
        Color? color = null, Vector2? center = null,
        Vector2? size = null, int combo = 0)
    {
        Point = point;
        Normal = normal;
        Strength = Mathf.Clamp01(strength);
        Color = color ?? Color.white;
        Center = center ?? point;
        Size = size ?? Vector2.one;
        Combo = combo;
    }
}