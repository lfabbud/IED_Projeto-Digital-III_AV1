using UnityEngine;
using TMPro;

/// <summary>Exibe o timer no formato mm:ss. Conecte UpdateTime(float) ao evento 'On Time Changed' do GameTimer.</summary>
public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    /// <summary>Conecte ao 'On Time Changed' do GameTimer.</summary>
    public void UpdateTime(float remainingSeconds)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
