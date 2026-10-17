using UnityEngine;
using UnityEngine.UI;

public class MenuConfigUI : MonoBehaviour
{
    [Header("Sliders de Audio")]
    public Slider masterSlider, musicSlider, sfxSlider;

    [Header("Opciones de Gráficos")]
    public Toggle bloomToggle, vintageToggle, filmGrainToggle;
    public Slider exposureSlider;

    private void Awake()
    {
        // Rango de sliders de audio (0% a 100%)
        masterSlider.minValue = 0f;
        masterSlider.maxValue = 1f;

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;

        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        // Rango de exposición/brillo (0 = oscuro, 2 = brillante)
        exposureSlider.minValue = 0f;
        exposureSlider.maxValue = 2f;
    }

    private void Start()
    {
        // --- Inicialización ---
        masterSlider.minValue = 0f;
        masterSlider.maxValue = 1f;
        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        exposureSlider.minValue = 0f;
        exposureSlider.maxValue = 2f;

        // --- Cargar valores previos ---
        masterSlider.value = PlayerPrefs.GetFloat("Master", 0.75f);
        musicSlider.value = PlayerPrefs.GetFloat("music", 0.75f);
        sfxSlider.value = PlayerPrefs.GetFloat("effect", 0.75f);

        bloomToggle.isOn = PlayerPrefs.GetInt("Bloom", 1) == 1;
        vintageToggle.isOn = PlayerPrefs.GetInt("Vintage", 0) == 1;
        filmGrainToggle.isOn = PlayerPrefs.GetInt("FilmGrain", 0) == 1;
        exposureSlider.value = PlayerPrefs.GetFloat("Exposure", 1f);

        // --- Eventos de actualización inmediata ---
        masterSlider.onValueChanged.AddListener(_ => ApplyAll());
        musicSlider.onValueChanged.AddListener(_ => ApplyAll());
        sfxSlider.onValueChanged.AddListener(_ => ApplyAll());
        exposureSlider.onValueChanged.AddListener(_ => ApplyAll());

        bloomToggle.onValueChanged.AddListener(_ => ApplyAll());
        vintageToggle.onValueChanged.AddListener(_ => ApplyAll());
        filmGrainToggle.onValueChanged.AddListener(_ => ApplyAll());

        ApplyAll();
    }


    public void ApplyAll()
    {
        var gsm = GameSettingsManager.Instance;
        if (gsm == null) return;

        gsm.SetMasterVolume(masterSlider.value);
        gsm.SetMusicVolume(musicSlider.value);
        gsm.SetSFXVolume(sfxSlider.value);

        gsm.SetBloom(bloomToggle.isOn);
        gsm.SetVintage(vintageToggle.isOn);
        gsm.SetFilmGrain(filmGrainToggle.isOn);
        gsm.SetExposure(exposureSlider.value);

        PlayerPrefs.Save(); // Guarda los ajustes inmediatamente
    }
}
