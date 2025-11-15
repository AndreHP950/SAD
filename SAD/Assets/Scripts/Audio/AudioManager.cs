using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioMixer audioMixer;
    public AudioSource sfxSource;        // Para efeitos curtos (coleta, clique, etc.)
    public AudioSource musicSource;      // Para a música tema do menu
    public AudioSource ambienceSource;   // Para o som de fundo do tráfego

    // Dicionário para guardar os clipes de áudio carregados
    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioClips(); // Carrega todos os clipes de áudio
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("masterVol", PlayerPrefs.GetFloat("masterVol", 0f));
            audioMixer.SetFloat("musicVol", PlayerPrefs.GetFloat("musicVol", 0f));
            audioMixer.SetFloat("effectsVol", PlayerPrefs.GetFloat("effectsVol", 0f));
        }
        
        // Inicia o áudio da cena atual
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LoadAudioClips()
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        foreach (AudioClip clip in clips)
        {
            if (!audioClips.ContainsKey(clip.name))
            {
                audioClips.Add(clip.name, clip);
            }
        }
        Debug.Log($"AudioManager: {audioClips.Count} clipes de áudio carregados.");
    }

    public void PlaySFX(string clipName)
    {
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        else
        {
            Debug.LogWarning($"AudioManager: SFX '{clipName}' não encontrado!");
        }
    }

    public void PlayLoopingSound(AudioSource source, string clipName)
    {
        if (source == null) return;

        if (audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            source.clip = clip;
            source.loop = true;
            source.Play();
        }
        else
        {
            Debug.LogWarning($"AudioManager: Som em loop '{clipName}' não encontrado!");
        }
    }

    public void StopAllSounds()
    {
        sfxSource?.Stop();
        musicSource?.Stop();
        ambienceSource?.Stop();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllSounds();

        // Assumindo que a cena de menu se chama "MainMenu"
        if (scene.name == "MainMenu") 
        {
            PlayLoopingSound(musicSource, "ThemeSong");
        }
        // Assumindo que a cena de jogo se chama "Game" 
        else if (scene.name == "Game") 
        {
            PlayLoopingSound(ambienceSource, "TrafficSound");
        }
    }

    // --- Métodos de Volume ---
    public void SetVolumeMaster(float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        audioMixer.SetFloat("masterVol", db);
        PlayerPrefs.SetFloat("masterVol", db);
        PlayerPrefs.Save();
    }

    public void SetVolumeMusic(float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        audioMixer.SetFloat("musicVol", db);
        PlayerPrefs.SetFloat("musicVol", db);
        PlayerPrefs.Save();
    }

    public void SetVolumeEffects(float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        audioMixer.SetFloat("effectsVol", db);
        PlayerPrefs.SetFloat("effectsVol", db);
        PlayerPrefs.Save();
    }
}