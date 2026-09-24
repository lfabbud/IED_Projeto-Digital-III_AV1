using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe os 3 ícones de vida (ex.: corações). Cada ícone começa "ativo" e passa a "inativo" da
/// direita para a esquerda conforme vidas são perdidas — comportamento inalterado, só os nomes
/// dos campos foram atualizados para refletir os sprites 2D (ex.: coração cheio / coração vazio).
/// Conecte UpdateLives(int) ao evento 'On Lives Changed' do LivesManager.
/// </summary>
public class LivesDisplay : MonoBehaviour
{
    [Tooltip("Ícones das vidas, na ordem em que devem ser preenchidos/esvaziados (ex.: 3 imagens lado a lado).")]
    [SerializeField] private Image[] lifeIcons;

    [Tooltip("Sprite exibido enquanto a vida está ativa (ex.: coração vermelho).")]
    [SerializeField] private Sprite activeSprite;

    [Tooltip("Sprite exibido depois que a vida foi perdida (ex.: coração cinza/apagado).")]
    [SerializeField] private Sprite inactiveSprite;

    /// <summary>Conecte ao 'On Lives Changed' do LivesManager.</summary>
    public void UpdateLives(int currentLives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            lifeIcons[i].sprite = i < currentLives ? activeSprite : inactiveSprite;
        }
    }
}
