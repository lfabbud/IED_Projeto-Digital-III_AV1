using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente reutilizável para as "janelas" de conteúdo do jogo (janela de título na Cena 1,
/// carrossel de onboarding na Cena 2, carrossel de resultado final na Cena 4). A janela é uma
/// única arte 2D (Sprite) já pronta — não há geração de fundo, cor de preenchimento ou
/// arredondamento de borda por script.
/// </summary>
public class WindowPanelController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Componente Image (UI) que exibe a arte 2D da janela.")]
    [SerializeField] private Image windowImage;

    [Header("Aparência")]
    [Tooltip("Sprite 2D da janela (arte pronta).")]
    [SerializeField] private Sprite windowSprite;

    [Tooltip("Tamanho da janela (Width/Height do RectTransform). Deixe em (0,0) para manter o tamanho nativo do sprite.")]
    [SerializeField] private Vector2 windowSize = Vector2.zero;

    [Header("Profundidade (Estereoscopia)")]
    [Tooltip("Deslocamento no eixo Z local, para ajuste de profundidade percebida em VR.")]
    [SerializeField] private float zDepthOffset = 0f;

    private void Awake() { Apply(); }
    private void OnValidate() { Apply(); }

    private void Apply()
    {
        if (windowImage != null && windowSprite != null) windowImage.sprite = windowSprite;
        if (windowImage != null && windowSize != Vector2.zero) windowImage.rectTransform.sizeDelta = windowSize;

        Vector3 pos = transform.localPosition;
        transform.localPosition = new Vector3(pos.x, pos.y, zDepthOffset);
    }
}
