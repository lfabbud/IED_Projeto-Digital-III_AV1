using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Recebe os eventos de mira (gaze) do plugin do Cardboard. O CardboardReticlePointer do seu
/// projeto envia OnPointerEnter/OnPointerExit via GameObject.SendMessage, então os métodos abaixo
/// precisam ter exatamente esses nomes, sem parâmetros, para serem recebidos.
///
/// IMPORTANTE: não tenho confirmação do nome exato do método de CLIQUE — só vi, nos avisos do seu
/// Console, o envio de Enter/Exit por SendMessage. Presumi "OnPointerClick" (padrão comum em forks
/// do SDK do Google VR). Teste o botão em Play mode; se o clique não disparar nada, provavelmente o
/// nome do método é outro, e será só renomear o método abaixo para o correto.
/// </summary>
public class GazeInteractable : MonoBehaviour
{
    [Header("Eventos de Mira")]
    public UnityEvent onGazeEnter;
    public UnityEvent onGazeExit;
    public UnityEvent onGazeClick;

    public void OnPointerEnter()
    {
        onGazeEnter?.Invoke();
    }

    public void OnPointerExit()
    {
        onGazeExit?.Invoke();
    }

    public void OnPointerClick()
    {
        onGazeClick?.Invoke();
    }
}
