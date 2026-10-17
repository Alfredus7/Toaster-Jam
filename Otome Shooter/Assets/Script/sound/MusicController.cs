using UnityEngine;
using System.Collections;

public class MusicController : MonoBehaviour
{
    [Header("Configuracion de Musica")]
    public AudioSource musicSource;
    public AudioClip musicAction;
    public AudioClip musicAmbiental;
    public float fadeDuration = 2.0f;

    private float originalVolume;
    private Coroutine currentFadeCoroutine;
    private AudioClip currentClip;

    void Start()
    {
        if (musicSource != null)
        {
            originalVolume = musicSource.volume;
            musicSource.volume = 0f; // Empezar con volumen en 0

            // Reproducir música ambiental automáticamente al inicio
            if (musicAmbiental != null)
            {
                PlayMusicAmbiental();
            }
        }
    }

    // Método para reproducir música de acción con fade
    public void PlayMusicAction()
    {
        if (musicSource == null || musicAction == null) return;

        // Si ya está reproduciendo la misma canción, no hacer nada
        if (currentClip == musicAction && musicSource.isPlaying) return;

        PlayMusic(musicAction);
    }

    // Método para reproducir música ambiental con fade
    public void PlayMusicAmbiental()
    {
        if (musicSource == null || musicAmbiental == null) return;

        // Si ya está reproduciendo la misma canción, no hacer nada
        if (currentClip == musicAmbiental && musicSource.isPlaying) return;

        PlayMusic(musicAmbiental);
    }

    // Método genérico para cambiar de música con fade
    private void PlayMusic(AudioClip newClip)
    {
        if (musicSource == null || newClip == null) return;

        // Detener fade actual si existe
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeToNewClipCoroutine(newClip));
    }

    // Corrutina para fade entre canciones
    private IEnumerator FadeToNewClipCoroutine(AudioClip newClip)
    {
        // Fade Out de la canción actual si está sonando
        if (musicSource.isPlaying)
        {
            float elapsedTime = 0f;
            float startVolume = musicSource.volume;

            while (elapsedTime < fadeDuration / 2f)
            {
                elapsedTime += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / (fadeDuration / 2f));
                yield return null;
            }

            musicSource.volume = 0f;
        }

        // Cambiar el clip y hacer Fade In
        musicSource.clip = newClip;
        currentClip = newClip;
        musicSource.Play();

        float fadeInTime = 0f;
        while (fadeInTime < fadeDuration / 2f)
        {
            fadeInTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, originalVolume, fadeInTime / (fadeDuration / 2f));
            yield return null;
        }

        musicSource.volume = originalVolume;
        currentFadeCoroutine = null;
    }

    // Método para detener música con fade out
    public void StopMusic()
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    // Corrutina para Fade Out suave
    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0f;
        float startVolume = musicSource.volume;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        currentClip = null;
        currentFadeCoroutine = null;
    }

    // Métodos para verificar qué música está sonando
    public bool IsPlayingAction()
    {
        return currentClip == musicAction && musicSource.isPlaying;
    }

    public bool IsPlayingAmbiental()
    {
        return currentClip == musicAmbiental && musicSource.isPlaying;
    }

    public bool IsMusicPlaying()
    {
        return musicSource.isPlaying;
    }
}