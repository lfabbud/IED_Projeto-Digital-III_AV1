using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Controla o cesto de coleta: acompanha o movimento horizontal da cabeça do jogador
/// (posição em rotação panorâmica em torno do eixo Y, no mesmo raio do spawner), mantendo
/// sempre a mesma altura e SEM rotacionar o próprio objeto (abertura sempre para cima).
/// Detecta a coleta de objetos (via BasketContactPoint, em um filho) e dispara os efeitos
/// visuais de coleta normal ou de captura de bomba.
/// </summary>
public class BasketController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Transform que define a posição base do jogador no mundo (o mesmo usado no ObjectSpawner, ex.: CAM_Player).")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Transform da cabeça/câmera do jogador, usado para ler a rotação horizontal (yaw). Em geral é a Main Camera, filha do rig, já que costuma ser ela que gira com o giroscópio do Cardboard — não tenho como confirmar isso sem ver a estrutura completa do seu rig, então vale testar em Play mode.")]
    [SerializeField] private Transform headTransform;

    [Tooltip("Referência ao Renderer do objeto visual do cesto (usado nos fades de início/fim de jogo e na mudança de cor ao pegar bomba).")]
    [SerializeField] private Renderer basketRenderer;

    [Tooltip("Transform do ponto de contato (fundo do cesto): o objeto-filho que deve conter um Collider marcado como 'Is Trigger' e o script BasketContactPoint.")]
    [SerializeField] private Transform contactPoint;

    [Header("Posicionamento")]
    [Tooltip("Raio de distância entre o jogador e o cesto. Recomenda-se usar o mesmo valor do 'Spawn Radius' do ObjectSpawner.")]
    [SerializeField] private float basketRadius = 5f;

    [Tooltip("Altura do cesto no eixo Y em relação ao nível de visão do jogador (a altura da câmera/cabeça). 0 = mesma altura dos olhos do jogador; valores negativos posicionam o cesto abaixo da linha de visão.")]
    [SerializeField] private float verticalOffset = -1.2f;

    [Header("Ponto de Contato")]
    [Tooltip("Ajusta a altura local (Y) do ponto de contato dentro do cesto, para posicioná-lo o mais próximo possível do fundo do modelo 3D, sem precisar arrastar manualmente na cena.")]
    [SerializeField] private float contactPointHeightOffset = 0f;

    [Header("Fade do Cesto (Início / Fim de Jogo)")]
    [Tooltip("Duração do fade-in do cesto (opacidade 0→1) quando o jogo começa, em segundos.")]
    [SerializeField] private float fadeInDuration = 1f;

    [Tooltip("Duração do fade-out do cesto (opacidade 1→0) quando o jogo termina, em segundos.")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Coleta de Objeto (não-bomba)")]
    [Tooltip("Duração do fade do objeto coletado até ele desaparecer. Este parâmetro não estava na lista de ajustes pedida — adicionei para viabilizar o efeito de 'objeto desaparece com um fade' descrito na mecânica.")]
    [SerializeField] private float collectedObjectFadeDuration = 0.4f;

    [Header("Mensagem de Coleta")]
    [Tooltip("Referência ao componente de texto (TextMeshPro - Text Mesh Pro UGUI) que exibe a mensagem de coleta. Assume que o pacote TextMeshPro já está importado no projeto (Window > TextMeshPro > Import TMP Essential Resources, caso ainda não tenha feito isso).")]
    [SerializeField] private TMP_Text collectionMessageText;

    [Tooltip("Texto exibido ao coletar um objeto (não-bomba).")]
    [SerializeField] private string collectionMessage = "+1";

    [Tooltip("Tempo (segundos) que a mensagem de coleta permanece visível.")]
    [SerializeField] private float messageDisplayDuration = 1f;

    [Tooltip("Fonte usada na mensagem de coleta. Deixe vazio para manter a fonte já configurada no componente de texto.")]
    [SerializeField] private TMP_FontAsset messageFont;

    [Tooltip("Tamanho da fonte da mensagem de coleta.")]
    [SerializeField] private float messageFontSize = 36f;

    [Tooltip("Cor da mensagem de coleta.")]
    [SerializeField] private Color messageColor = Color.white;

    [Header("Reação à Bomba")]
    [Tooltip("Cor para a qual o cesto pisca ao capturar uma bomba.")]
    [SerializeField] private Color bombFlashColor = Color.red;

    [Tooltip("Duração de CADA transição de fade de cor (ida OU volta) durante a piscada — uma piscada completa dura o dobro disso.")]
    [SerializeField] private float bombColorFadeDuration = 0.15f;

    [Tooltip("Quantidade de piscadas (ida + volta = 1 piscada) que o cesto faz ao capturar uma bomba.")]
    [SerializeField, Min(1)] private int bombBlinkCount = 3;

    [Header("Eventos (conectar aos futuros scripts de Placar e Vidas)")]
    [Tooltip("Disparado quando um objeto normal (não-bomba) é coletado. Conecte aqui o método que aumenta o placar, quando esse script existir.")]
    public UnityEvent OnObjectCollected;

    [Tooltip("Disparado quando uma bomba é coletada. Conecte aqui o método que remove uma vida, quando esse script existir.")]
    public UnityEvent OnBombCollected;

    [Tooltip("Disparado quando um cupom é coletado (além de 'On Object Collected', já que o cupom também é um objeto normal). Conecte aqui o método do CouponManager.")]
    public UnityEvent OnCouponCollected;

    // --- Estado interno ---
    private Material basketMaterial;
    private Color basketOriginalColor;
    private Quaternion fixedRotation;
    private Coroutine messageRoutine;
    private Coroutine colorRoutine;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // Guarda a rotação inicial do cesto: ela nunca mudará, garantindo que a abertura
        // permaneça sempre voltada para cima, independentemente da posição orbital.
        fixedRotation = transform.rotation;

        if (basketRenderer != null)
        {
            basketMaterial = basketRenderer.material; // instancia o material para não alterar o asset compartilhado
            basketOriginalColor = basketMaterial.color;
        }

        if (collectionMessageText != null)
        {
            collectionMessageText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        UpdateContactPointHeight();

        // Fade-in automático ao iniciar a cena. O ideal, futuramente, é que um GameManager
        // chame FadeIn()/FadeOut() nos momentos exatos de início/fim de partida — deixei essa
        // chamada aqui por enquanto para o cesto já funcionar de forma independente.
        FadeIn();
    }

    private void OnValidate()
    {
        UpdateContactPointHeight();
    }

    private void Update()
    {
        if (playerTransform == null || headTransform == null) return;

        // Ângulo horizontal (yaw) obtido a partir da direção para onde a cabeça está olhando,
        // projetada no plano XZ.
        Vector3 forward = headTransform.forward;
        forward.y = 0f;

        float angle;
        if (forward.sqrMagnitude > 0.0001f)
        {
            angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
        else
        {
            angle = transform.eulerAngles.y; // fallback: olhar quase reto para cima/baixo — mantém o último ângulo válido
        }

        float rad = angle * Mathf.Deg2Rad;
        Vector3 basePos = playerTransform.position;

        transform.position = new Vector3(
            basePos.x + basketRadius * Mathf.Sin(rad),
            headTransform.position.y + verticalOffset,
            basePos.z + basketRadius * Mathf.Cos(rad)
        );

        // Nunca rotaciona o cesto — a abertura permanece sempre voltada para cima.
        transform.rotation = fixedRotation;
    }

    private void UpdateContactPointHeight()
    {
        if (contactPoint == null) return;
        Vector3 local = contactPoint.localPosition;
        local.y = contactPointHeightOffset;
        contactPoint.localPosition = local;
    }

    /// <summary>Chamado pelo BasketContactPoint quando um objeto entra na área de contato do fundo do cesto.</summary>
    public void HandleObjectCaught(FallingObject obj)
    {
        if (obj.IsBomb)
        {
            obj.Collect(0f); // desaparece imediatamente; a mecânica não pede fade para o objeto-bomba em si
            TriggerBombReaction();
            OnBombCollected?.Invoke();
        }
        else
        {
            obj.Collect(collectedObjectFadeDuration);
            ShowCollectionMessage();
            OnObjectCollected?.Invoke(); // o cupom também soma ponto no placar, por ser um objeto capturado normalmente — presumi isso, já que a mecânica não deixou explícito se o cupom soma pontos além de contar para o desfecho final
            if (obj.IsCoupon)
            {
                OnCouponCollected?.Invoke();
            }
        }
    }

    private void ShowCollectionMessage()
    {
        if (collectionMessageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessageRoutine());
    }

    private IEnumerator ShowMessageRoutine()
    {
        collectionMessageText.text = collectionMessage;
        if (messageFont != null) collectionMessageText.font = messageFont;
        collectionMessageText.fontSize = messageFontSize;
        collectionMessageText.color = messageColor;
        collectionMessageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDisplayDuration);

        collectionMessageText.gameObject.SetActive(false);
    }

    private void TriggerBombReaction()
    {
        if (basketMaterial == null) return;
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(BombFlickerRoutine());
    }

    private IEnumerator BombFlickerRoutine()
    {
        for (int i = 0; i < bombBlinkCount; i++)
        {
            yield return LerpColorRoutine(basketOriginalColor, bombFlashColor, bombColorFadeDuration);
            yield return LerpColorRoutine(bombFlashColor, basketOriginalColor, bombColorFadeDuration);
        }
        basketMaterial.color = basketOriginalColor;
    }

    private IEnumerator LerpColorRoutine(Color from, Color to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            basketMaterial.color = Color.Lerp(from, to, duration > 0f ? t / duration : 1f);
            yield return null;
        }
        basketMaterial.color = to;
    }

    /// <summary>Inicia o fade-in do cesto (opacidade 0 → 1). Pode ser chamado por um futuro GameManager no início da partida.</summary>
    public void FadeIn()
    {
        if (basketMaterial == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAlphaRoutine(0f, 1f, fadeInDuration));
    }

    /// <summary>Inicia o fade-out do cesto (opacidade 1 → 0). Pode ser chamado por um futuro GameManager no fim da partida.</summary>
    public void FadeOut()
    {
        if (basketMaterial == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAlphaRoutine(basketMaterial.color.a, 0f, fadeOutDuration));
    }

    private IEnumerator FadeAlphaRoutine(float fromAlpha, float toAlpha, float duration)
    {
        float t = 0f;
        Color c = basketMaterial.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(fromAlpha, toAlpha, duration > 0f ? t / duration : 1f);
            basketMaterial.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        basketMaterial.color = new Color(c.r, c.g, c.b, toAlpha);
    }
}
