using UnityEngine;

/// <summary>
/// Componente adicionado automaticamente pelo ObjectSpawner a cada objeto instanciado.
/// Controla o movimento de queda (eixo Y), a rotação aleatória nos 3 eixos, e o despawn
/// quando o objeto ultrapassa a altura mínima sem ser coletado pelo jogador.
/// </summary>
public class FallingObject : MonoBehaviour
{
    /// <summary>Indica se este objeto é a bomba (definido pelo ObjectSpawner ao instanciar).</summary>
    public bool IsBomb { get; private set; }

    private ObjectSpawner spawner;
    private float despawnHeight;
    private float columnAngle;
    private Vector3 rotationSpeed;

    /// <summary>Chamado pelo ObjectSpawner logo após a instanciação.</summary>
    public void Initialize(ObjectSpawner spawnerRef, bool isBomb, float despawnY, float angle)
    {
        spawner = spawnerRef;
        IsBomb = isBomb;
        despawnHeight = despawnY;
        columnAngle = angle;
    }

    public void SetRotationSpeed(Vector3 speedPerAxis)
    {
        rotationSpeed = speedPerAxis;
    }

    private void Update()
    {
        float fallSpeed = spawner != null ? spawner.CurrentFallSpeed : 2f;
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);

        if (transform.position.y <= despawnHeight)
        {
            Despawn();
        }
    }

    /// <summary>
    /// Chame este método a partir do script da cesta (a ser criado) quando o objeto for
    /// coletado pelo jogador — antes de aplicar a lógica de pontuação/vidas.
    /// Isso libera a coluna e destrói o objeto.
    /// </summary>
    public void OnCollected()
    {
        if (spawner != null) spawner.ReleaseColumn(columnAngle);
        Destroy(gameObject);
    }

    private void Despawn()
    {
        if (spawner != null) spawner.ReleaseColumn(columnAngle);
        Destroy(gameObject);
    }
}
