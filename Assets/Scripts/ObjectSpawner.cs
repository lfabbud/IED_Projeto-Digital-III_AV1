using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de spawn de objetos para o jogo de captura em VR (3DoF - Google Cardboard).
/// Gera objetos em "colunas de queda" posicionadas ao redor do jogador, a uma distância
/// (raio) fixa. A cada spawn, uma coluna é escolhida de forma aleatória, garantindo que:
///  - nunca haja duas colunas ATIVAS (com objeto ainda caindo) próximas demais entre si;
///  - os spawns nunca aconteçam no mesmo instante (são sempre serializados, um por vez).
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Transform do jogador (normalmente a câmera / Cardboard Main Rig). As colunas são calculadas em torno da posição XZ deste objeto.")]
    [SerializeField] private Transform playerTransform;

    [Header("Posicionamento das Colunas")]
    [Tooltip("Raio de distância entre o jogador e as colunas de queda (em metros).")]
    [SerializeField] private float spawnRadius = 5f;

    [Tooltip("Quantidade de colunas de queda possíveis ao redor do jogador (os 360° são divididos entre elas).")]
    [SerializeField, Min(2)] private int numberOfColumns = 8;

    [Tooltip("Distância mínima (em metros, medida ao longo do círculo) entre duas colunas ativas ao mesmo tempo.")]
    [SerializeField] private float minDistanceBetweenColumns = 2f;

    [Header("Altura de Spawn / Despawn")]
    [Tooltip("Altura (eixo Y) em que os objetos são instanciados.")]
    [SerializeField] private float spawnHeight = 10f;

    [Tooltip("Altura (eixo Y) em que os objetos NÃO coletados são destruídos.")]
    [SerializeField] private float despawnHeight = -1f;

    [Header("Assets Spawnáveis")]
    [Tooltip("Lista de assets 3D que podem ser spawnados (ex.: 5 itens, incluindo a bomba). Marque 'Is Bomb' no item que representa a bomba.")]
    [SerializeField] private List<SpawnableAsset> spawnableAssets = new List<SpawnableAsset>();

    [Header("Ritmo de Spawn")]
    [Tooltip("Intervalo mínimo entre spawns, em segundos.")]
    [SerializeField] private float minSpawnInterval = 0.5f;

    [Tooltip("Intervalo máximo entre spawns, em segundos.")]
    [SerializeField] private float maxSpawnInterval = 1.5f;

    [Header("Velocidade de Queda")]
    [Tooltip("Velocidade base de queda dos objetos (unidades/segundo).")]
    [SerializeField] private float baseFallSpeed = 2f;

    [Tooltip("Faixa (mín/máx) de velocidade de rotação aleatória aplicada a cada eixo dos objetos, em graus/segundo.")]
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(30f, 180f);

    [Header("Aumento de Dificuldade")]
    [Tooltip("Quantidade de objetos coletados necessária para aumentar a velocidade de queda.")]
    [SerializeField, Min(1)] private int objectsToIncreaseSpeed = 15;

    [Tooltip("Porcentagem de aumento na velocidade de queda a cada 'Objects To Increase Speed' objetos coletados (ex.: 10 = +10%).")]
    [SerializeField] private float speedIncreasePercent = 10f;

    // --- Estado interno ---
    private float currentSpeedMultiplier = 1f;
    private int collectedObjectsCount = 0;
    private readonly List<float> activeColumnAngles = new List<float>(); // ângulos (graus) das colunas com objeto ainda caindo
    private Coroutine spawnRoutine;

    /// <summary>Velocidade de queda atual (base * multiplicador acumulado). Lida pelos objetos instanciados.</summary>
    public float CurrentFallSpeed => baseFallSpeed * currentSpeedMultiplier;

    private void OnEnable()
    {
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (spawnableAssets == null || spawnableAssets.Count == 0 || playerTransform == null)
            return;

        if (!TryGetValidColumnAngle(out float angle))
            return; // não encontrou posição válida nesta tentativa; tenta de novo no próximo ciclo

        SpawnableAsset chosen = ChooseWeightedAsset();
        if (chosen == null || chosen.prefab == null) return;

        Vector3 basePos = playerTransform.position;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 spawnPos = new Vector3(
            basePos.x + spawnRadius * Mathf.Sin(rad),
            spawnHeight,
            basePos.z + spawnRadius * Mathf.Cos(rad)
        );

        GameObject instance = Instantiate(chosen.prefab, spawnPos, Random.rotation);

        FallingObject falling = instance.GetComponent<FallingObject>();
        if (falling == null) falling = instance.AddComponent<FallingObject>();

        Vector3 randomRotSpeed = new Vector3(
            Random.Range(rotationSpeedRange.x, rotationSpeedRange.y) * RandomSign(),
            Random.Range(rotationSpeedRange.x, rotationSpeedRange.y) * RandomSign(),
            Random.Range(rotationSpeedRange.x, rotationSpeedRange.y) * RandomSign()
        );

        falling.Initialize(this, chosen.isBomb, despawnHeight, angle);
        falling.SetRotationSpeed(randomRotSpeed);

        activeColumnAngles.Add(angle);
    }

    /// <summary>
    /// Chamado pelo FallingObject quando ele é destruído (coletado ou despawnado por altura),
    /// para liberar a coluna e permitir que ela seja reutilizada por um próximo spawn.
    /// </summary>
    public void ReleaseColumn(float angle)
    {
        activeColumnAngles.Remove(angle);
    }

    /// <summary>
    /// Deve ser chamado pelo script de coleta (a cesta) quando o jogador captura um objeto
    /// normal (não-bomba). Incrementa o contador e aplica o aumento de velocidade quando
    /// atinge o número configurado de objetos coletados.
    /// </summary>
    public void RegisterObjectCollected()
    {
        collectedObjectsCount++;
        if (collectedObjectsCount % objectsToIncreaseSpeed == 0)
        {
            currentSpeedMultiplier *= 1f + (speedIncreasePercent / 100f);
        }
    }

    private bool TryGetValidColumnAngle(out float validAngle)
    {
        float slotSize = 360f / numberOfColumns;
        float minAngleDistance = Mathf.Rad2Deg * (minDistanceBetweenColumns / spawnRadius);

        // tenta um número limitado de vezes achar um slot aleatório suficientemente longe dos ativos
        const int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            int slotIndex = Random.Range(0, numberOfColumns);
            // jitter dentro do slot: garante que a posição não fique sempre "engessada" nas mesmas marcações
            float jitter = Random.Range(-slotSize * 0.4f, slotSize * 0.4f);
            float candidate = Mathf.Repeat(slotIndex * slotSize + jitter, 360f);

            bool valid = true;
            foreach (float active in activeColumnAngles)
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(candidate, active));
                if (diff < minAngleDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                validAngle = candidate;
                return true;
            }
        }

        validAngle = 0f;
        return false;
    }

    private SpawnableAsset ChooseWeightedAsset()
    {
        float totalWeight = 0f;
        foreach (var asset in spawnableAssets) totalWeight += Mathf.Max(0f, asset.spawnWeight);

        if (totalWeight <= 0f) return spawnableAssets[Random.Range(0, spawnableAssets.Count)];

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var asset in spawnableAssets)
        {
            cumulative += Mathf.Max(0f, asset.spawnWeight);
            if (roll <= cumulative) return asset;
        }
        return spawnableAssets[spawnableAssets.Count - 1];
    }

    private static float RandomSign() => Random.value < 0.5f ? -1f : 1f;
}

/// <summary>
/// Representa um asset 3D spawnável, com sua identificação de "bomba" e peso de sorteio opcional.
/// </summary>
[System.Serializable]
public class SpawnableAsset
{
    public GameObject prefab;

    [Tooltip("Marque como true se este asset for a bomba.")]
    public bool isBomb;

    [Tooltip("Peso relativo de sorteio deste asset (itens com peso maior aparecem com mais frequência). Deixe todos em 1 para chance igual entre os 5 assets.")]
    [Min(0f)] public float spawnWeight = 1f;
}
