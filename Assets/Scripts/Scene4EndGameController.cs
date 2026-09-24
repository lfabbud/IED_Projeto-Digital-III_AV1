using UnityEngine;
using TMPro;

/// <summary>
/// Controla a Cena 4 (resultado): lê o resultado gravado em GameResultData ao final da Cena 3.
/// Se o jogador perdeu (3 bombas), mostra a janela de derrota (Final 1). Se venceu (tempo esgotado),
/// mostra o carrossel de 2 telas: (1) placar final, (2) ícones de cupons capturados/faltantes —
/// cobrindo os casos Final 2.a (1 cupom), 2.b (2 cupons) e 2.c (3 cupons).
///
/// OBSERVAÇÃO: a descrição original não cobre o caso de vencer com 0 cupons capturados — deixei esse
/// caso mostrando os 3 ícones como "faltante" (comportamento neutro do CouponIconDisplay), mas vale
/// você confirmar se é isso mesmo que faz sentido para o jogo, ou se esse caso precisa de uma mensagem própria.
/// </summary>
public class Scene4EndGameController : MonoBehaviour
{
    [Header("Painéis Raiz")]
    [Tooltip("Painel exibido no Final 1 (perdeu).")]
    [SerializeField] private GameObject lossPanel;

    [Tooltip("Painel (carrossel) exibido no Final 2.a/b/c (venceu).")]
    [SerializeField] private GameObject winCarouselPanel;

    [Header("Derrota (Final 1)")]
    [SerializeField] private TMP_Text lossMessageText;
    [SerializeField] private string lossMessage = "Você perdeu!";

    [Header("Vitória — Slide 1: Placar")]
    [SerializeField] private GameObject scoreSlide;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private string scorePrefix = "Você fez ";
    [SerializeField] private string scoreSuffix = " pontos!";

    [Header("Vitória — Slide 2: Cupons")]
    [SerializeField] private GameObject couponSlide;
    [SerializeField] private CouponIconDisplay couponIconDisplay;

    [Header("Navegação")]
    [Tooltip("Nome exato da Cena 3 (jogo), para o botão 'jogar novamente'.")]
    [SerializeField] private string gameSceneName = "SCN_Jogo";
    [SerializeField] private SceneFadeController sceneFadeController;

    private void Start()
    {
        bool won = GameResultData.PlayerWon;

        if (lossPanel != null) lossPanel.SetActive(!won);
        if (winCarouselPanel != null) winCarouselPanel.SetActive(won);

        if (!won)
        {
            if (lossMessageText != null) lossMessageText.text = lossMessage;
            return;
        }

        if (finalScoreText != null)
            finalScoreText.text = scorePrefix + GameResultData.FinalScore + scoreSuffix;

        if (couponIconDisplay != null)
            couponIconDisplay.UpdateCoupons(GameResultData.CouponsCollected);

        ShowScoreSlide();
    }

    /// <summary>Conecte à seta "avançar" do carrossel de vitória.</summary>
    public void ShowCouponSlide()
    {
        if (scoreSlide != null) scoreSlide.SetActive(false);
        if (couponSlide != null) couponSlide.SetActive(true);
    }

    /// <summary>Conecte à seta "voltar" do carrossel de vitória.</summary>
    public void ShowScoreSlide()
    {
        if (scoreSlide != null) scoreSlide.SetActive(true);
        if (couponSlide != null) couponSlide.SetActive(false);
    }

    /// <summary>Conecte ao botão "jogar novamente" (presente tanto na derrota quanto na vitória).</summary>
    public void PlayAgain()
    {
        if (sceneFadeController != null)
        {
            sceneFadeController.GoToScene(gameSceneName);
        }
    }
}
