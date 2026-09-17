using System.Collections;
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
    private bool isCollected;
    private Renderer cachedRenderer;

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
        if (isCollected) return; // parado: já foi coletado, aguardando o fade (se houver) antes de ser destruído

        float fallSpeed = spawner != null ? spawner.CurrentFallSpeed : 2f;
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);

        if (transform.position.y <= despawnHeight)
        {
            Despawn();
        }
    }

    /// <summary>
    /// Chame este método a partir do script da cesta quando o objeto for coletado pelo jogador.
    /// Libera a coluna imediatamente (para o spawner poder reutilizá-la) e para o movimento de queda.
    /// Se fadeDuration for maior que 0, o objeto some com um fade de opacidade antes de ser destruído;
    /// caso contrário, é destruído imediatamente.
    /// IMPORTANTE: o fade só funciona visualmente se o material do objeto estiver configurado para
    /// suportar transparência (ex.: Rendering Mode "Fade"/"Transparent" no Built-in RP, ou Surface Type
    /// "Transparent" no URP/HDRP). Não tenho como confirmar qual pipeline de render seu projeto usa,
    /// então vale conferir isso no material dos seus prefabs.
    /// </summary>
    public void Collect(float fadeDuration)
    {
        if (isCollected) return;
        isCollected = true;

        if (spawner != null) spawner.ReleaseColumn(columnAngle);

        if (fadeDuration <= 0f)
        {
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(FadeOutAndDestroy(fadeDuration));
        }
    }

    private IEnumerator FadeOutAndDestroy(float duration)
    {
        cachedRenderer = GetComponent<Renderer>();
        Material mat = cachedRenderer != null ? cachedRenderer.material : null;

        if (mat == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Color startColor = mat.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startColor.a, 0f, t / duration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, a);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Despawn()
    {
        if (spawner != null) spawner.ReleaseColumn(columnAngle);
        Destroy(gameObject);
    }
}
