using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Orquestra a Cena 1 (introdução). A aparência da janela e dos botões já é resolvida pelos
/// componentes reutilizáveis (WindowPanelController, GazeButton) — este script cuida apenas da
/// orquestração específica desta cena: aplicar a arte 2D do título, exibir a saudação, e navegar.
/// </summary>
public class Scene1Controller : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SceneFadeController sceneFadeController;

    [Header("Título 2D")]
    [Tooltip("Componente Image (UI) que exibe a arte 2D do título.")]
    [SerializeField] private Image titleImage;

    [Tooltip("Sprite 2D do título.")]
    [SerializeField] private Sprite titleSprite;

    [Tooltip("Deslocamento no eixo Z local do título, para ajuste de profundidade percebida em VR.")]
    [SerializeField] private float titleZDepthOffset = 0f;

    [Header("Mensagem de Saudação")]
    [SerializeField] private TMP_Text greetingText;
    [SerializeField] private string greetingMessage = "Bem-vindo!";

    [Header("Painel de Informações")]
    [Tooltip("Painel/janela de informações do projeto, alternado pelo botão 'Informações'.")]
    [SerializeField] private GameObject infoPanel;

    [Header("Navegação")]
    [Tooltip("Nome exato da Cena 2 (onboarding), conforme cadastrado no Build Settings.")]
    [SerializeField] private string nextSceneName = "SCN_Onboarding";

    private void Start()
    {
        ApplyTitle();

        if (greetingText != null) greetingText.text = greetingMessage;
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    private void ApplyTitle()
    {
        if (titleImage != null && titleSprite != null) titleImage.sprite = titleSprite;
        if (titleImage != null)
        {
            Vector3 pos = titleImage.transform.localPosition;
            titleImage.transform.localPosition = new Vector3(pos.x, pos.y, titleZDepthOffset);
        }
    }

    /// <summary>Conecte ao clique do botão "Informações" (GazeInteractable.onGazeClick).</summary>
    public void ToggleInfoPanel()
    {
        if (infoPanel != null) infoPanel.SetActive(!infoPanel.activeSelf);
    }

    /// <summary>Conecte ao clique do botão "Iniciar" (GazeInteractable.onGazeClick).</summary>
    public void StartGame()
    {
        if (sceneFadeController != null)
        {
            sceneFadeController.GoToScene(nextSceneName);
        }
    }
}
