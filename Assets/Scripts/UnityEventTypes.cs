using UnityEngine.Events;

/// <summary>
/// UnityEvent não expõe parâmetros genéricos diretamente no Inspector — é preciso uma subclasse
/// concreta e serializável para cada tipo. Estas são compartilhadas pelo ScoreManager, LivesManager,
/// CouponManager e GameTimer.
/// </summary>
[System.Serializable]
public class IntUnityEvent : UnityEvent<int> { }

[System.Serializable]
public class FloatUnityEvent : UnityEvent<float> { }
