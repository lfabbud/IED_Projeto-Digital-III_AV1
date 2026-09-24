using UnityEngine;

/// <summary>Gerencia o placar do jogo. Conecte AddPoint() ao evento 'On Object Collected' do BasketController.</summary>
public class ScoreManager : MonoBehaviour
{
    [Tooltip("Pontos ganhos por objeto coletado.")]
    [SerializeField] private int pointsPerObject = 1;

    [Header("Eventos")]
    [Tooltip("Disparado sempre que o placar muda, com o novo valor total. Conecte ao ScoreDisplay.")]
    public IntUnityEvent OnScoreChanged;

    public int CurrentScore { get; private set; }

    /// <summary>Conecte ao evento 'On Object Collected' do BasketController.</summary>
    public void AddPoint()
    {
        CurrentScore += pointsPerObject;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
