using UnityEngine;
using UnityEngine.Events;

/// <summary>Gerencia as vidas do jogo. Conecte LoseLife() ao evento 'On Bomb Collected' do BasketController.</summary>
public class LivesManager : MonoBehaviour
{
    [Tooltip("Quantidade de vidas no início da partida.")]
    [SerializeField, Min(1)] private int startingLives = 3;

    [Header("Eventos")]
    [Tooltip("Disparado sempre que a quantidade de vidas muda, com o novo valor. Conecte ao LivesDisplay.")]
    public IntUnityEvent OnLivesChanged;

    [Tooltip("Disparado uma única vez quando as vidas chegam a zero. Conecte ao EndGameManager (Final 1 — perdeu).")]
    public UnityEvent OnLivesDepleted;

    public int CurrentLives { get; private set; }
    private bool depleted;

    private void Start()
    {
        CurrentLives = startingLives;
        OnLivesChanged?.Invoke(CurrentLives);
    }

    /// <summary>Conecte ao evento 'On Bomb Collected' do BasketController.</summary>
    public void LoseLife()
    {
        if (depleted) return;

        CurrentLives = Mathf.Max(0, CurrentLives - 1);
        OnLivesChanged?.Invoke(CurrentLives);

        if (CurrentLives <= 0)
        {
            depleted = true;
            OnLivesDepleted?.Invoke();
        }
    }

    public void ResetLives()
    {
        CurrentLives = startingLives;
        depleted = false;
        OnLivesChanged?.Invoke(CurrentLives);
    }
}
