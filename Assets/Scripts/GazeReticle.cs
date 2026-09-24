using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mira (reticle) baseada em "dwell time": o jogador aciona um botão/ícone olhando fixamente para
/// ele por X segundos, sem precisar de um gatilho físico. Faz seu próprio raycast (não depende do
/// SendMessage do CardboardReticlePointer) e chama diretamente os métodos públicos do
/// GazeInteractable (OnPointerEnter/OnPointerExit/OnPointerClick).
///
/// IMPORTANTE — sobreposição com o plugin: se o CardboardReticlePointer do seu plugin também estiver
/// fazendo raycast e chamando OnPointerEnter/OnPointerExit por conta própria, você vai ter os dois
/// sistemas disparando os mesmos eventos ao mesmo tempo (duplicado). Recomendo desativar o
/// componente/lógica de raycast do CardboardReticlePointer (mantendo só a parte de renderização da
/// mira dele, se quiser, ou substituindo pela deste script) para este script assumir sozinho a
/// detecção de mira e o clique. Não tenho como confirmar a estrutura interna do plugin de aí, então
/// vale testar com atenção.
///
/// IMPORTANTE — colliders: este script usa Physics.Raycast, então qualquer botão/ícone precisa ter
/// um Collider (ex.: BoxCollider do tamanho do RectTransform) para ser detectado — não basta ter
/// apenas o Image/RectTransform de UI.
/// </summary>
public class GazeReticle : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Transform da câmera/cabeça, usado como origem e direção do raycast (ex.: Main Camera).")]
    [SerializeField] private Transform gazeOrigin;

    [Tooltip("Componente Image (UI) que exibe a mira. Deve estar num Canvas filho da câmera, centralizado na tela.")]
    [SerializeField] private Image reticleImage;

    [Tooltip("Asset 2D (sprite) da imagem da mira.")]
    [SerializeField] private Sprite reticleSprite;

    [Header("Raycast")]
    [Tooltip("Distância máxima do raycast da mira.")]
    [SerializeField] private float maxDistance = 20f;

    [Tooltip("Camadas consideradas para detecção de objetos interativos.")]
    [SerializeField] private LayerMask interactableLayerMask = ~0;

    [Header("Temporizador de Seleção (Dwell Time)")]
    [Tooltip("Tempo, em segundos, que o jogador precisa manter o olhar sobre o alvo para acioná-lo.")]
    [SerializeField] private float dwellTime = 2f;

    [Header("Animação — Estado Parado / Em Mira")]
    [SerializeField] private float idleScale = 1f;
    [SerializeField] private float maxHoverScale = 1.8f;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color hoverColor = Color.white;

    [Header("Animação — Confirmação de Clique")]
    [Tooltip("Cor de flash exibida rapidamente quando o clique é acionado.")]
    [SerializeField] private Color triggerFlashColor = Color.green;

    [Tooltip("Escala momentânea (\"pulso\") no instante do clique.")]
    [SerializeField] private float triggerPulseScale = 2.2f;

    [Tooltip("Duração do pulso/flash de confirmação de clique.")]
    [SerializeField] private float triggerFeedbackDuration = 0.2f;

    // --- Estado interno ---
    private GazeInteractable currentTarget;
    private float dwellTimer;
    private bool triggeredForCurrentTarget;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (reticleImage != null && reticleSprite != null)
        {
            reticleImage.sprite = reticleSprite;
        }
        ResetReticleVisual();
    }

    private void Update()
    {
        if (gazeOrigin == null) return;

        GazeInteractable hitTarget = RaycastForTarget();

        if (hitTarget != currentTarget)
        {
            HandleTargetChanged(hitTarget);
        }

        if (currentTarget != null && !triggeredForCurrentTarget)
        {
            UpdateDwell();
        }
    }

    private GazeInteractable RaycastForTarget()
    {
        Ray ray = new Ray(gazeOrigin.position, gazeOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayerMask))
        {
            return hit.collider.GetComponentInParent<GazeInteractable>();
        }
        return null;
    }

    private void HandleTargetChanged(GazeInteractable newTarget)
    {
        if (currentTarget != null)
        {
            currentTarget.OnPointerExit();
        }

        currentTarget = newTarget;
        dwellTimer = 0f;
        triggeredForCurrentTarget = false;
        ResetReticleVisual();

        if (currentTarget != null)
        {
            currentTarget.OnPointerEnter();
        }
    }

    private void UpdateDwell()
    {
        dwellTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(dwellTimer / dwellTime);

        ApplyHoverVisual(progress);

        if (dwellTimer >= dwellTime)
        {
            TriggerClick();
        }
    }

    private void ApplyHoverVisual(float progress)
    {
        if (reticleImage == null) return;

        float scale = Mathf.Lerp(idleScale, maxHoverScale, progress);
        reticleImage.transform.localScale = Vector3.one * scale;
        reticleImage.color = Color.Lerp(idleColor, hoverColor, progress);
    }

    private void TriggerClick()
    {
        triggeredForCurrentTarget = true;
        currentTarget.OnPointerClick();

        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(TriggerFeedbackRoutine());
    }

    private IEnumerator TriggerFeedbackRoutine()
    {
        if (reticleImage == null) yield break;

        float t = 0f;
        while (t < triggerFeedbackDuration)
        {
            t += Time.deltaTime;
            float k = t / triggerFeedbackDuration;
            // vai até o pico do pulso na metade do tempo, e volta na outra metade
            float pulse = k < 0.5f ? Mathf.Lerp(maxHoverScale, triggerPulseScale, k / 0.5f)
                                    : Mathf.Lerp(triggerPulseScale, maxHoverScale, (k - 0.5f) / 0.5f);
            reticleImage.transform.localScale = Vector3.one * pulse;
            reticleImage.color = Color.Lerp(hoverColor, triggerFlashColor, Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        ApplyHoverVisual(1f); // volta ao estado "em mira" normal (o alvo ainda pode estar sendo olhado)
    }

    private void ResetReticleVisual()
    {
        if (reticleImage == null) return;
        reticleImage.transform.localScale = Vector3.one * idleScale;
        reticleImage.color = idleColor;
    }

    /// <summary>
    /// Ativa ou desativa a mira por completo — tanto a imagem 2D quanto a lógica de raycast/dwell
    /// (Update deixa de rodar). Usado para trocar a mira por outro elemento (ex.: a sacola 3D) em
    /// cenas onde ela não deve aparecer, como a cena de jogo.
    /// </summary>
    public void SetActive(bool active)
    {
        enabled = active;

        if (reticleImage != null) reticleImage.gameObject.SetActive(active);

        if (!active)
        {
            // libera qualquer alvo que estivesse em hover, para não deixá-lo "preso" nesse estado
            if (currentTarget != null)
            {
                currentTarget.OnPointerExit();
                currentTarget = null;
            }
            dwellTimer = 0f;
            triggeredForCurrentTarget = false;

            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
                feedbackRoutine = null;
            }
        }
    }
}
