using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla o carrossel de onboarding da Cena 2: uma sequência de slides (imagem + legenda),
/// navegável pelas setas, com transição de fade out/fade in entre eles. A seta de voltar some
/// no primeiro slide; a seta de avançar dá lugar ao botão de início de jogo no último slide.
/// Cada slide tem seu próprio tamanho/posição de imagem, já que os sprites variam de tamanho.
/// </summary>
public class Scene2CarouselController : MonoBehaviour
{
    [System.Serializable]
    public class OnboardingSlide
    {
        [Tooltip("Imagem estática (.png) ou primeiro quadro de uma sequência de animação.")]
        public Sprite image;

        [TextArea]
        public string caption;

        [Tooltip("Tamanho da imagem DESTE slide (Width/Height). Deixe em (0,0) para usar o tamanho nativo do sprite. Como cada sprite pode ter proporções diferentes, ajuste slide a slide aqui.")]
        public Vector2 imageSize = Vector2.zero;

        [Tooltip("Deslocamento de posição (X,Y) da imagem DESTE slide, em relação ao centro — útil para recentralizar sprites de tamanhos bem diferentes.")]
        public Vector2 imagePositionOffset = Vector2.zero;

        [Tooltip("Escala adicional da imagem DESTE slide (multiplica o tamanho já definido em Image Size). 1 = sem alteração. Útil para um ajuste fino rápido, sem precisar recalcular Width/Height.")]
        public float imageScale = 1f;
    }

    [Header("Referências")]
    [SerializeField] private SceneFadeController sceneFadeController;
    [SerializeField] private Image slideImage;
    [SerializeField] private TMP_Text captionText;

    [Tooltip("GameObject que contém o SlideImage e o CaptionText como filhos, com um componente Canvas Group nele — é isso que permite o fade out/fade in de ambos juntos.")]
    [SerializeField] private CanvasGroup slideContentCanvasGroup;

    [Header("Legenda")]
    [SerializeField] private TextAlignmentOptions captionAlignment = TextAlignmentOptions.Center;
    [SerializeField] private float lineSpacing = 0f;

    [Header("Profundidade da Imagem do Slide")]
    [Tooltip("Deslocamento no eixo Z local da imagem do slide (igual para todos os slides), para ajuste de profundidade percebida em VR.")]
    [SerializeField] private float slideImageZDepthOffset = 0f;

    [Header("Transição entre Slides")]
    [Tooltip("Duração do fade out (slide atual sumindo) e do fade in (novo slide aparecendo), em segundos, cada um.")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Slides")]
    [SerializeField] private OnboardingSlide[] slides;

    [Header("Pré-visualização no Editor")]
    [Tooltip("Mude este valor no Inspector (fora do Play mode) para visualizar cada slide na Scene/Game view, e ajustar Image Size/Position Offset de cada um com o resultado à vista.")]
    [SerializeField] private int previewIndex = 0;

    [Header("Botões de Navegação")]
    [Tooltip("GameObject da seta de voltar. É desativado automaticamente no primeiro slide.")]
    [SerializeField] private GameObject prevArrowButton;

    [Tooltip("GameObject da seta de avançar. É desativado automaticamente no último slide (dando lugar ao Close Button).")]
    [SerializeField] private GameObject nextArrowButton;

    [Tooltip("GameObject do botão de início de jogo. Fica escondido em todos os slides, exceto no último, onde substitui a seta de avançar.")]
    [SerializeField] private GameObject closeButton;

    [Header("Navegação de Cena")]
    [Tooltip("Nome exato da Cena 3 (jogo), conforme cadastrado no Build Settings.")]
    [SerializeField] private string nextSceneName = "SCN_Jogo";

    private int currentIndex;
    private bool isTransitioning;
    private Coroutine transitionRoutine;

    private void Start()
    {
        if (captionText != null)
        {
            captionText.alignment = captionAlignment;
            captionText.lineSpacing = lineSpacing;
        }
        if (slideContentCanvasGroup != null) slideContentCanvasGroup.alpha = 1f;
        ApplySlide(0);
    }

    /// <summary>
    /// Executa fora do Play mode também: mudar 'Preview Index' no Inspector chama isto e atualiza
    /// a imagem/legenda exibidas na Scene/Game view instantaneamente (sem fade), para você ajustar
    /// Image Size/Position Offset de cada slide vendo o resultado.
    /// </summary>
    private void OnValidate()
    {
        if (slides == null || slides.Length == 0) return;
        previewIndex = Mathf.Clamp(previewIndex, 0, slides.Length - 1);

        // Fora do Play mode, não faz fade nem mexe no CanvasGroup — só troca o conteúdo, instantâneo.
        if (!Application.isPlaying)
        {
            ApplySlide(previewIndex);
        }
    }

    /// <summary>Aplica o conteúdo (sprite, legenda, tamanho, posição, escala) do slide de índice 'index', sem fade.</summary>
    private void ApplySlide(int index)
    {
        if (slides == null || slides.Length == 0) return;
        currentIndex = Mathf.Clamp(index, 0, slides.Length - 1);
        OnboardingSlide slide = slides[currentIndex];

        if (slideImage != null)
        {
            slideImage.sprite = slide.image;

            if (slide.imageSize != Vector2.zero)
                slideImage.rectTransform.sizeDelta = slide.imageSize;

            Vector3 pos = slideImage.transform.localPosition;
            slideImage.transform.localPosition = new Vector3(
                slide.imagePositionOffset.x,
                slide.imagePositionOffset.y,
                slideImageZDepthOffset
            );
            slideImage.transform.localScale = Vector3.one * slide.imageScale;
        }

        if (captionText != null) captionText.text = slide.caption;

        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        bool isFirstSlide = currentIndex == 0;
        bool isLastSlide = slides != null && currentIndex == slides.Length - 1;

        if (prevArrowButton != null) prevArrowButton.SetActive(!isFirstSlide);
        if (nextArrowButton != null) nextArrowButton.SetActive(!isLastSlide);
        if (closeButton != null) closeButton.SetActive(isLastSlide);
    }

    /// <summary>Conecte à seta "avançar" (GazeInteractable.onGazeClick).</summary>
    public void NextSlide()
    {
        if (isTransitioning || slides == null || currentIndex >= slides.Length - 1) return;
        GoToSlide(currentIndex + 1);
    }

    /// <summary>Conecte à seta "voltar" (GazeInteractable.onGazeClick).</summary>
    public void PreviousSlide()
    {
        if (isTransitioning || currentIndex <= 0) return;
        GoToSlide(currentIndex - 1);
    }

    private void GoToSlide(int index)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionToSlide(index));
    }

    private IEnumerator TransitionToSlide(int newIndex)
    {
        isTransitioning = true;

        yield return FadeCanvasGroup(1f, 0f, fadeDuration); // fade out do slide atual
        ApplySlide(newIndex);                                 // troca sprite/legenda/tamanho enquanto invisível
        yield return FadeCanvasGroup(0f, 1f, fadeDuration); // fade in do novo slide

        isTransitioning = false;
        transitionRoutine = null;
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (slideContentCanvasGroup == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            slideContentCanvasGroup.alpha = Mathf.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }
        slideContentCanvasGroup.alpha = to;
    }

    /// <summary>Conecte ao botão de início de jogo, exibido no último slide (GazeInteractable.onGazeClick).</summary>
    public void CloseAndStartGame()
    {
        if (sceneFadeController != null)
        {
            sceneFadeController.GoToScene(nextSceneName);
        }
    }
}
