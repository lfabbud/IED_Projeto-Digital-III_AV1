using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Timer de contagem regressiva da partida. A duração precisa ser igual ao 'Round Duration'
/// configurado no ObjectSpawner, para os cupons serem agendados corretamente dentro do tempo total.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [Tooltip("Duração total da partida, em segundos (ex.: 180 = 3 minutos). Deve ser igual ao 'Round Duration' do ObjectSpawner.")]
    [SerializeField] private float roundDuration = 180f;

    [Tooltip("Se marcado, a contagem começa automaticamente ao carregar a cena.")]
    [SerializeField] private bool startAutomatically = true;

    [Header("Eventos")]
    [Tooltip("Disparado a cada frame com o tempo restante, em segundos. Conecte ao TimerDisplay.")]
    public FloatUnityEvent OnTimeChanged;

    [Tooltip("Disparado uma única vez quando o tempo chega a zero. Conecte ao EndGameManager.")]
    public UnityEvent OnTimeUp;

    public float RemainingTime { get; private set; }
    public bool IsRunning { get; private set; }

    private void Start()
    {
        RemainingTime = roundDuration;
        if (startAutomatically) StartTimer();
    }

    public void StartTimer()
    {
        RemainingTime = roundDuration;
        IsRunning = true;
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    private void Update()
    {
        if (!IsRunning) return;

        RemainingTime -= Time.deltaTime;

        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            IsRunning = false;
            OnTimeChanged?.Invoke(RemainingTime);
            OnTimeUp?.Invoke();
            return;
        }

        OnTimeChanged?.Invoke(RemainingTime);
    }
}
