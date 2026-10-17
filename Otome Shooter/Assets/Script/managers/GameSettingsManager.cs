using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    [Header("Audio")]
    public AudioMixer audioMixer;

    [Header("Gráficos URP (opcional)")]
    public Volume postProcessVolume;

    private Bloom bloom;
    private Vignette vignette;
    private FilmGrain filmGrain;
    private ColorAdjustments colorAdj;

    private void Awake()
    {
        // Instancia única por escena, sin persistencia
        Instance = this;

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out bloom);
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out filmGrain);
            postProcessVolume.profile.TryGet(out colorAdj);
        }
        else
        {
            Debug.Log("[GameSettingsManager] No hay Volume asignado en esta escena — módulo gráfico inactivo.");
        }


    }
    private void Start()
    {
        LoadSettings();
    }


    // ----- AUDIO -----
    public void SetMasterVolume(float value)
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("Master", value);
    }

    public void SetMusicVolume(float value)
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat("music", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("music", value);
    }

    public void SetSFXVolume(float value)
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat("effect", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        PlayerPrefs.SetFloat("effect", value);
    }

    // ----- GRÁFICOS -----
    public void SetBloom(bool on)
    {
        if (bloom == null) return;
        bloom.active = on;
        PlayerPrefs.SetInt("Bloom", on ? 1 : 0);
    }

    public void SetVintage(bool on)
    {
        if (vignette == null) return;
        vignette.active = on;
        PlayerPrefs.SetInt("Vintage", on ? 1 : 0);
    }

    public void SetFilmGrain(bool on)
    {
        if (filmGrain == null) return;
        filmGrain.active = on;
        PlayerPrefs.SetInt("FilmGrain", on ? 1 : 0);
    }

    public void SetExposure(float val)
    {
        if (colorAdj == null) return;
        colorAdj.postExposure.value = val;
        PlayerPrefs.SetFloat("Exposure", val);
    }

    // ----- CARGA -----
    public void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("Master", 0.75f);
        float music = PlayerPrefs.GetFloat("music", 0.75f);
        float sfx = PlayerPrefs.GetFloat("effect", 0.75f);

        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);


        bool bloomOn = PlayerPrefs.GetInt("Bloom", 1) == 1;
        bool vignOn = PlayerPrefs.GetInt("Vintage", 1) == 1;
        bool filmOn = PlayerPrefs.GetInt("FilmGrain", 1) == 1;
        float expo = PlayerPrefs.GetFloat("Exposure", 0f);

        SetBloom(bloomOn);
        SetVintage(vignOn);
        SetFilmGrain(filmOn);
        SetExposure(expo);
    }
}