using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botão interativo por mira (gaze), reutilizável em qualquer cena. Depende de um GazeInteractable
/// no mesmo GameObject para receber os eventos de clique. O botão é uma única arte 2D (Sprite) já
/// pronta, com ícone e texto embutidos — não há geração de fundo nem texto separado.
/// Ao ser acionado (dwell time completo), o botão anima um movimento de "pressionar e soltar" no
/// eixo Z local, dando feedback visual de toque.
/// PREMISSA: assumi que "pressionar" significa mover no sentido -Z local (afastando o botão do
/// jogador). Se na prática o movimento sair na direção contrária, inverta o sinal de Press Distance.
/// </summary>
[RequireComponent(typeof(GazeInteractable))]
public class GazeButton : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Componente Image (UI) do botão — a mesma arte final, já com ícone e texto embutidos.")]
    [SerializeField] private Image buttonImage;

    [Tooltip("AudioSource usado para tocar o som de acionamento.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Aparência")]
    [Tooltip("Sprite 2D do botão (arte pronta, com ícone e texto).")]
    [SerializeField] private Sprite buttonSprite;

    [Tooltip("Tamanho do botão (Width/Height do RectTransform). Deixe em (0,0) para manter o tamanho nativo do sprite.")]
    [SerializeField] private Vector2 buttonSize = Vector2.zero;

    [Header("Profundidade (Estereoscopia)")]
    [Tooltip("Deslocamento no eixo Z local — posição de repouso do botão, para ajuste de profundidade percebida em VR.")]
    [SerializeField] private float zDepthOffset = 0f;

    [Header("Animação de Clique (Pressionar / Soltar)")]
    [Tooltip("Distância, no eixo Z local, que o botão se desloca ao ser pressionado.")]
    [SerializeField] private float pressDistance = 0.02f;

    [Tooltip("Velocidade do movimento de pressionar/soltar, em unidades de Z por segundo. Quanto maior, mais rápida a animação.")]
    [SerializeField] private float pressSpeed = 0.2f;

    [Header("Som")]
    [Tooltip("Efeito sonoro tocado ao acionar o botão.")]
    [SerializeField] private AudioClip clickSound;

    private GazeInteractable interactable;
    private Vector3 restLocalPosition;
    private Coroutine pressRoutine;

    private void Awake()
    {
        interactable = GetComponent<GazeInteractable>();
        interactable.onGazeClick.AddListener(HandleClick);
        ApplyAppearance();
    }

    private void OnValidate()
    {
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (buttonImage != null && buttonSprite != null) buttonImage.sprite = buttonSprite;
        if (buttonImage != null && buttonSize != Vector2.zero) buttonImage.rectTransform.sizeDelta = buttonSize;

        Vector3 pos = transform.localPosition;
        restLocalPosition = new Vector3(pos.x, pos.y, zDepthOffset);

        // só reposiciona de fato se não estiver no meio da animação de clique
        if (pressRoutine == null)
        {
            transform.localPosition = restLocalPosition;
        }
    }

    private void HandleClick()
    {
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);

        if (pressRoutine != null) StopCoroutine(pressRoutine);
        pressRoutine = StartCoroutine(PressAndRelease());
    }

    private IEnumerator PressAndRelease()
    {
        Vector3 pressedPosition = restLocalPosition + new Vector3(0f, 0f, -pressDistance);
        float travelTime = pressSpeed > 0f ? Mathf.Abs(pressDistance) / pressSpeed : 0.05f;

        yield return MoveLocal(restLocalPosition, pressedPosition, travelTime);
        yield return MoveLocal(pressedPosition, restLocalPosition, travelTime);
        pressRoutine = null;
    }

    private IEnumerator MoveLocal(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }
        transform.localPosition = to;
    }
}
