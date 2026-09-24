using UnityEngine;
using TMPro;

/// <summary>Exibe o placar. Conecte UpdateScore(int) ao evento 'On Score Changed' do ScoreManager.</summary>
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string prefix = "Pontos: ";

    /// <summary>Conecte ao 'On Score Changed' do ScoreManager.</summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = prefix + score;
    }
}
