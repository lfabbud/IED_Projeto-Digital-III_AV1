using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de spawn de objetos para o jogo de captura em VR (3DoF - Google Cardboard).
/// Gera objetos em "colunas de queda" posicionadas ao redor do jogador, a uma distância
/// (raio) fixa. A cada spawn, uma coluna é escolhida de forma aleatória, garantindo que:
///  - nunca haja duas colunas ATIVAS (com objeto ainda caindo) próximas demais entre si;
///  - os spawns nunca aconteçam no mesmo instante (são sempre serializados, um por vez).
/// Também agenda exatamente "Coupon Count" cupons, em instantes aleatórios ao longo da
/// duração da partida, entre os demais objetos.
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
    [Tooltip("Lista de assets 3D que podem ser spawnados. Marque 'Is Bomb' no item da bomba e 'Is Coupon' no item do cupom.")]
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

    [Header("Cupons")]
    [Tooltip("Quantidade total de cupons que devem aparecer ao longo da partida.")]
    [SerializeField, Min(0)] private int couponCount = 3;

    [Tooltip("Duração total da partida (segundos). Precisa ser igual ao valor configurado no GameTimer, para os cupons serem distribuídos corretamente dentro do tempo de jogo.")]
    [SerializeField] private float roundDuration = 180f;

    // --- Estado interno ---
    private float currentSpeedMultiplier = 1f;
    private int collectedObjectsCount = 0;
    private readonly List<float> activeColumnAngles = new List<float>(); // ângulos (graus) das colunas com objeto ainda caindo
    private Coroutine spawnRoutine;
    private List<float> couponSpawnTimes;
    private int nextCouponIndex;
    private float elapsedTime;

    /// <summary>Velocidade de queda atual (base * multiplicador acumulado). Lida pelos objetos instanciados.</summary>
    public float CurrentFallSpeed => baseFallSpeed * currentSpeedMultiplier;

    private void OnEnable()
    {
        ScheduleCoupons();
        elapsedTime = 0f;
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
    }

    private void ScheduleCoupons()
    {
        couponSpawnTimes = new List<float>();
        for (int i = 0; i < couponCount; i++)
        {
            couponSpawnTimes.Add(Random.Range(0f, roundDuration));
        }
        couponSpawnTimes.Sort();
        nextCouponIndex = 0;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);
            elapsedTime += wait;
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (spawnableAssets == null || spawnableAssets.Count == 0 || playerTransform == null)
            return;

        if (!TryGetValidColumnAngle(out float angle))
            return; // não encontrou posição válida nesta tentativa; tenta de novo no próximo ciclo

        bool forceCoupon = nextCouponIndex < couponSpawnTimes.Count && elapsedTime >= couponSpawnTimes[nextCouponIndex];

        SpawnableAsset chosen = forceCoupon ? GetCouponAsset() : ChooseWeightedAsset();
        if (chosen == null || chosen.prefab == null) return;

        if (forceCoupon) nextCouponIndex++;

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

        falling.Initialize(this, chosen.isBomb, chosen.isCoupon, despawnHeight, angle);
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
    /// normal (não-bomba, incluindo cupons). Incrementa o contador e aplica o aumento de
    /// velocidade quando atinge o número configurado de objetos coletados.
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

        const int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            int slotIndex = Random.Range(0, numberOfColumns);
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
        // exclui cupons da seleção aleatória normal: eles só aparecem nos instantes agendados
        List<SpawnableAsset> pool = spawnableAssets.FindAll(a => !a.isCoupon);
        if (pool.Count == 0) pool = spawnableAssets;

        float totalWeight = 0f;
        foreach (var asset in pool) totalWeight += Mathf.Max(0f, asset.spawnWeight);

        if (totalWeight <= 0f) return pool[Random.Range(0, pool.Count)];

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var asset in pool)
        {
            cumulative += Mathf.Max(0f, asset.spawnWeight);
            if (roll <= cumulative) return asset;
        }
        return pool[pool.Count - 1];
    }

    private SpawnableAsset GetCouponAsset()
    {
        SpawnableAsset coupon = spawnableAssets.Find(a => a.isCoupon);
        if (coupon == null)
        {
            Debug.LogWarning("ObjectSpawner: nenhum item marcado como 'Is Coupon' na lista Spawnable Assets — usando sorteio normal no lugar do cupom agendado.");
            return ChooseWeightedAsset();
        }
        return coupon;
    }

    private static float RandomSign() => Random.value < 0.5f ? -1f : 1f;
}

/// <summary>
/// Representa um asset 3D spawnável: identificação de bomba/cupom e peso de sorteio opcional.
/// </summary>
[System.Serializable]
public class SpawnableAsset
{
    public GameObject prefab;

    [Tooltip("Marque como true se este asset for a bomba.")]
    public bool isBomb;

    [Tooltip("Marque como true se este asset for o cupom. Cupons não entram no sorteio aleatório normal — eles são agendados separadamente (ver Coupon Count / Round Duration).")]
    public bool isCoupon;

    [Tooltip("Peso relativo de sorteio deste asset entre os itens normais (ignorado para o item marcado como cupom).")]
    [Min(0f)] public float spawnWeight = 1f;
}
