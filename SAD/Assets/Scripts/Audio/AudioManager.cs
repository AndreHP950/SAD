using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Clips")]
    public AudioClip collectBoxSFX;
    public AudioClip deliverySuccessSFX;
    public AudioClip deliveryFailedSFX;
    // Adicione mais SFX aqui conforme necessário

    [Header("Audio Sources")]
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
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
        if (musicSource != null)
            musicSource.volume = musicVolume;
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

    // Métodos para controle de volume
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
}