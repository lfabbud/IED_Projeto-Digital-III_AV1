using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla o fade in/out da cena (tela) e o fade da trilha sonora ambiente, além da transição
/// para a próxima cena. Reutilizável nas 3 cenas — cada uma tem sua própria instância, com seus
/// próprios tempos e clipe de música.
/// </summary>
public class SceneFadeController : MonoBehaviour
{
    [Header("Fade de Tela")]
    [Tooltip("CanvasGroup de uma imagem preta full-screen usada para o fade. Alpha 1 = tela preta, 0 = cena visível. Precisa existir na Canvas da cena.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Tempo de espera antes do fade-in começar, ao carregar a cena (segundos).")]
    [SerializeField] private float fadeInDelay = 0f;

    [Tooltip("Duração do fade-in (segundos).")]
    [SerializeField] private float fadeInDuration = 1f;

    [Tooltip("Tempo de espera antes do fade-out começar, ao trocar de cena (segundos).")]
    [SerializeField] private float fadeOutDelay = 0f;

    [Tooltip("Duração do fade-out (segundos).")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Trilha Sonora Ambiente")]
    [SerializeField] private AudioSource ambientAudioSource;

    [Tooltip("Clipe de trilha sonora ambiente desta cena.")]
    [SerializeField] private AudioClip ambientMusicClip;

    [Tooltip("Se marcado, o volume da trilha sonora acompanha o fade da tela (silenciosa quando a tela está preta).")]
    [SerializeField] private bool fadeMusicWithScene = true;

    [Tooltip("Volume máximo da trilha sonora ambiente.")]
    [SerializeField, Range(0f, 1f)] private float musicMaxVolume = 1f;

    private Coroutine fadeRoutine;

    private void Start()
    {
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f; // começa preto

        if (ambientAudioSource != null && ambientMusicClip != null)
        {
            ambientAudioSource.clip = ambientMusicClip;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = fadeMusicWithScene ? 0f : musicMaxVolume;
            ambientAudioSource.Play();
        }

        FadeIn();
    }

    public void FadeIn()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, fadeInDelay, fadeInDuration));
    }

    /// <summary>
    /// Faz fade-out e, ao terminar, carrega a cena indicada (nome exato da cena no Build Settings).
    /// Conecte este método ao evento de clique do botão de avançar (ex.: GazeInteractable.onGazeClick).
    /// </summary>
    public void GoToScene(string sceneName)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return FadeRoutine(0f, 1f, fadeOutDelay, fadeOutDuration);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? t / duration : 1f;
            float screenAlpha = Mathf.Lerp(fromAlpha, toAlpha, k);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = screenAlpha;
            if (fadeMusicWithScene && ambientAudioSource != null)
                ambientAudioSource.volume = (1f - screenAlpha) * musicMaxVolume;

            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = toAlpha;
        if (fadeMusicWithScene && ambientAudioSource != null)
            ambientAudioSource.volume = (1f - toAlpha) * musicMaxVolume;
    }
}
