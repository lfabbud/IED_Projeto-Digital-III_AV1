using UnityEngine;

/// <summary>
/// Na cena de jogo (Cena 3), a mira 2D tradicional (crosshair) é substituída — tanto na imagem
/// quanto na função — pela sacola 3D (BasketController): é ela que passa a acompanhar o olhar do
/// jogador e a reagir à interação, só que capturando objetos por colisão física (BasketContactPoint),
/// em vez de seleção por dwell-time.
///
/// Este script apenas desativa a GazeReticle ao entrar nesta cena, evitando que o crosshair 2D e sua
/// lógica de raycast/dwell fiquem ativos ao mesmo tempo que a sacola. Coloque-o em qualquer GameObject
/// da Cena 3 (ex.: junto com o futuro script de gerenciamento do jogo) e arraste a referência da
/// GazeReticle da cena.
///
/// OBSERVAÇÃO: se, mais adiante, a Cena 3 precisar de algum botão acionável por mira durante a partida
/// (ex.: um ícone de pausa), esse fluxo vai precisar reativar a GazeReticle temporariamente
/// (chamando gazeReticle.SetActive(true)) enquanto o botão estiver visível, e desativá-la de novo ao
/// fechar — isso ainda não está implementado aqui, pois não foi pedido.
/// </summary>
public class GameSceneReticleOverride : MonoBehaviour
{
    [Tooltip("Referência à GazeReticle presente na cena (normalmente um componente na câmera).")]
    [SerializeField] private GazeReticle gazeReticle;

    private void Start()
    {
        if (gazeReticle != null)
        {
            gazeReticle.SetActive(false);
        }
    }
}
