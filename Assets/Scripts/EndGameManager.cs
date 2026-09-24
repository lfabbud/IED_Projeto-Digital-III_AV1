using UnityEngine;

/// <summary>
/// Decide o desfecho da partida (Final 1 = perdeu por 3 bombas, Final 2.a/b/c = venceu por tempo,
/// com 1/2/3 cupons) e dispara a transição para a Cena 4, gravando o resultado em GameResultData.
/// Conecte 'HandleLivesDepleted' ao 'On Lives Depleted' do LivesManager, e 'HandleTimeUp' ao
/// 'On Time Up' do GameTimer.
/// </summary>
public class EndGameManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CouponManager couponManager;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private SceneFadeController sceneFadeController;

    [Tooltip("Nome exato da Cena 4 (tela de resultado), conforme cadastrado no Build Settings.")]
    [SerializeField] private string endGameSceneName = "SCN_Fim";

    private bool gameEnded;

    /// <summary>Conecte ao 'On Lives Depleted' do LivesManager (Final 1 — perdeu).</summary>
    public void HandleLivesDepleted()
    {
        if (gameEnded) return;
        gameEnded = true;

        if (gameTimer != null) gameTimer.StopTimer();

        GameResultData.PlayerWon = false;
        GameResultData.FinalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        GameResultData.CouponsCollected = couponManager != null ? couponManager.CollectedCount : 0;

        GoToEndScene();
    }

    /// <summary>Conecte ao 'On Time Up' do GameTimer (Final 2 — venceu por tempo).</summary>
    public void HandleTimeUp()
    {
        if (gameEnded) return;
        gameEnded = true;

        GameResultData.PlayerWon = true;
        GameResultData.FinalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        GameResultData.CouponsCollected = couponManager != null ? couponManager.CollectedCount : 0;

        GoToEndScene();
    }

    private void GoToEndScene()
    {
        if (sceneFadeController != null)
        {
            sceneFadeController.GoToScene(endGameSceneName);
        }
    }
}
