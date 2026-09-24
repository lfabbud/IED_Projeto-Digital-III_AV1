using UnityEngine;

/// <summary>
/// Anexe este script ao GameObject-filho que representa o "fundo" do cesto (a superfície de
/// contato). Esse GameObject precisa também ter um Collider com "Is Trigger" marcado.
/// O script encaminha a detecção de colisão para o BasketController no objeto pai.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BasketContactPoint : MonoBehaviour
{
    private BasketController basketController;

    private void Awake()
    {
        basketController = GetComponentInParent<BasketController>();
        if (basketController == null)
        {
            Debug.LogWarning($"BasketContactPoint em '{name}' não encontrou um BasketController em nenhum objeto pai.");
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"O Collider em '{name}' não está marcado como 'Is Trigger'. A detecção de coleta pode não funcionar como esperado.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (basketController == null) return;

        FallingObject falling = other.GetComponentInParent<FallingObject>();
        if (falling != null)
        {
            basketController.HandleObjectCaught(falling);
        }
    }
}
