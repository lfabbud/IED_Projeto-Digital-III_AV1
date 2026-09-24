using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe uma fileira de ícones de cupom. Cada ícone começa "inativo" e passa a "ativo" da
/// esquerda para a direita conforme cupons são capturados — comportamento inalterado, só os
/// nomes dos campos foram atualizados. Reutilizado no HUD da Cena 3 e na tela de resultado da Cena 4.
/// Conecte UpdateCoupons(int) ao 'On Coupon Collected' do CouponManager (Cena 3), ou chame
/// manualmente com GameResultData.CouponsCollected (Cena 4).
/// </summary>
public class CouponIconDisplay : MonoBehaviour
{
    [SerializeField] private Image[] couponIcons;

    [Tooltip("Sprite exibido depois que o cupom foi capturado.")]
    [SerializeField] private Sprite activeSprite;

    [Tooltip("Sprite exibido enquanto o cupom ainda não foi capturado.")]
    [SerializeField] private Sprite inactiveSprite;

    public void UpdateCoupons(int capturedCount)
    {
        for (int i = 0; i < couponIcons.Length; i++)
        {
            if (couponIcons[i] == null) continue;
            couponIcons[i].sprite = i < capturedCount ? activeSprite : inactiveSprite;
        }
    }
}
