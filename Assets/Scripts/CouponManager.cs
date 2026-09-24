using UnityEngine;

/// <summary>Gerencia a contagem de cupons coletados. Conecte CollectCoupon() ao evento 'On Coupon Collected' do BasketController.</summary>
public class CouponManager : MonoBehaviour
{
    [Tooltip("Quantidade total de cupons disponíveis na partida. Deve bater com o 'Coupon Count' do ObjectSpawner.")]
    [SerializeField, Min(0)] private int totalCoupons = 3;

    [Header("Eventos")]
    [Tooltip("Disparado sempre que um cupom é coletado, com a quantidade total já coletada. Conecte ao CouponIconDisplay (HUD).")]
    public IntUnityEvent OnCouponCollected;

    public int CollectedCount { get; private set; }
    public int TotalCoupons => totalCoupons;

    /// <summary>Conecte ao evento 'On Coupon Collected' do BasketController.</summary>
    public void CollectCoupon()
    {
        CollectedCount = Mathf.Min(totalCoupons, CollectedCount + 1);
        OnCouponCollected?.Invoke(CollectedCount);
    }

    public void ResetCoupons()
    {
        CollectedCount = 0;
    }
}
