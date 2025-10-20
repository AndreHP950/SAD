using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Clips")]
    public AudioClip collectBoxSFX;
    public AudioClip deliverySuccessSFX;
    public AudioClip deliveryFailedSFX;
    // Adicione mais SFX aqui conforme necessário

    [Header("Audio Sources")]
    public AudioMixer audioMixer;
    public AudioSource sfxSource;
    public AudioSource musicSource; // Para música de fundo futura

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("masterVol", PlayerPrefs.GetFloat("masterVol", 1f));
            audioMixer.SetFloat("musicVol", PlayerPrefs.GetFloat("musicVol", 1f));
            audioMixer.SetFloat("effectsVol", PlayerPrefs.GetFloat("effectsVol", 1f));
        }

        //if (sfxSource != null)
        //    sfxSource.volume = sfxVolume;
        //if (musicSource != null)
        //    musicSource.volume = musicVolume;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayCollectBoxSFX()
    {
        PlaySFX(collectBoxSFX);
    }

    public void PlayDeliverySuccessSFX()
    {
        PlaySFX(deliverySuccessSFX);
    }

    public void PlayDeliveryFailedSFX()
    {
        PlaySFX(deliveryFailedSFX);
    }

    //// Métodos para controle de volume
    //public void SetSFXVolume(float volume)
    //{
    //    sfxVolume = Mathf.Clamp01(volume);
    //    if (sfxSource != null)
    //        sfxSource.volume = sfxVolume;
    //}

    //public void SetMusicVolume(float volume)
    //{
    //    musicVolume = Mathf.Clamp01(volume);
    //    if (musicSource != null)
    //        musicSource.volume = musicVolume;
    //}

    public void SetVolumeMaster(float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 40f;

        audioMixer.SetFloat("masterVol", db);

        PlayerPrefs.SetFloat("masterSlider", sliderValue);
        PlayerPrefs.SetFloat("masterVol", db);

        PlayerPrefs.Save();
    }

    public void SetVolumeMusic(float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 40f;

        audioMixer.SetFloat("musicVol", db);

        PlayerPrefs.SetFloat("musicSlider", sliderValue);
        PlayerPrefs.SetFloat("musicVol", db);

        PlayerPrefs.Save();
    }

    public void SetVolumeEffects(float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 40f;

        audioMixer.SetFloat("effectsVol", db);

        PlayerPrefs.SetFloat("effectsSlider", sliderValue);
        PlayerPrefs.SetFloat("effectsVol", db);

        PlayerPrefs.Save();
    }
}