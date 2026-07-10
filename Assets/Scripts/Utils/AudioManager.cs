using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct SoundEffect
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] private float sfxVolumeMultiplier = 1f;

    [Header("Configurazione Audio Globale")]
    [SerializeField] private SoundEffect musicaMenu;
    [SerializeField] private SoundEffect musicaGameplay;
    [SerializeField] private SoundEffect suonoFineFase;

    private const string MENU_MUSIC_ID = "MenuMusic";
    private const string GAMEPLAY_MUSIC_ID = "GameplayMusic";

    public float SFXVolumeMultiplier
    {
        get => sfxVolumeMultiplier;
        set
        {
            sfxVolumeMultiplier = Mathf.Clamp01(value);
            UpdateActiveLoopsVolume();
        }
    }

    private class ActiveLoop
    {
        public string id;
        public AudioSource source;
        public Coroutine fadeCoroutine;
        public float baseVolume;
    }

    private Dictionary<string, ActiveLoop> activeLoops = new Dictionary<string, ActiveLoop>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[AudioManager] Istanza duplicata rilevata su {gameObject.name}. Distruzione della copia in eccesso.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[AudioManager] Inizializzato su {gameObject.name}. SFX Volume Multiplier: {sfxVolumeMultiplier}.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] Scena caricata: {scene.name} (Build Index: {scene.buildIndex})");

        if (scene.buildIndex == 0)
        {
            // Ferma musica gameplay se attiva, avvia musica menu
            StopLoop(GAMEPLAY_MUSIC_ID, fadeTime: 0.5f);
            if (musicaMenu.clip != null)
            {
                StartLoop(musicaMenu, MENU_MUSIC_ID, fadeTime: 1.0f);
            }
        }
        else if (scene.buildIndex == 1 || scene.buildIndex == 2)
        {
            // Ferma musica menu se attiva, avvia musica gameplay
            StopLoop(MENU_MUSIC_ID, fadeTime: 0.5f);
            if (musicaGameplay.clip != null)
            {
                StartLoop(musicaGameplay, GAMEPLAY_MUSIC_ID, fadeTime: 1.0f);
            }
        }
    }

    /// <summary>
    /// Riproduce il suono di completamento fase.
    /// </summary>
    public void PlayFineFase()
    {
        if (suonoFineFase.clip != null)
        {
            Play2D(suonoFineFase);
        }
    }


    private void UpdateActiveLoopsVolume()
    {
        foreach (var loop in activeLoops.Values)
        {
            if (loop.source != null)
            {
                loop.source.volume = loop.baseVolume * sfxVolumeMultiplier;
            }
        }
    }

    /// <summary>
    /// Plays a 2D sound (for UI, inventory, etc.) with optional volume and pitch variation.
    /// </summary>
    public void Play2D(AudioClip clip, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Play2D: clip è null. Impossibile riprodurre il suono.");
            return;
        }

        float finalVolume = volume * sfxVolumeMultiplier;
        float pitch = Random.Range(pitchMin, pitchMax);
        Debug.Log($"[AudioManager] Play2D: clip='{clip.name}', volume={volume}, sfxMultiplier={sfxVolumeMultiplier}, finalVolume={finalVolume}, pitch={pitch:F2}.");

        if (finalVolume <= 0f)
        {
            Debug.LogWarning($"[AudioManager] Play2D: volume finale è {finalVolume}. Il suono non sarà udibile! Controlla sfxVolumeMultiplier ({sfxVolumeMultiplier}) e il volume del SoundEffect ({volume}).");
        }

        GameObject go = new GameObject("TempSFX_2D");
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = finalVolume;
        source.pitch = pitch;
        source.spatialBlend = 0f; // 2D
        source.Play();

        Debug.Log($"[AudioManager] Play2D: AudioSource.isPlaying={source.isPlaying} dopo Play() sul clip '{clip.name}'.");

        Destroy(go, clip.length + 0.5f);
    }

    /// <summary>
    /// Plays a 2D sound effect.
    /// </summary>
    public void Play2D(SoundEffect effect, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (effect.clip == null)
        {
            Debug.LogWarning("[AudioManager] Play2D(SoundEffect): il campo 'clip' del SoundEffect è null. Assegna un AudioClip nell'Inspector.");
            return;
        }
        Play2D(effect.clip, effect.volume, pitchMin, pitchMax);
    }

    /// <summary>
    /// Plays a 3D sound at a specific position with optional volume and pitch variation.
    /// </summary>
    public void Play3D(AudioClip clip, Vector3 position, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f, float minDistance = 1f, float maxDistance = 20f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Play3D: clip è null. Impossibile riprodurre il suono.");
            return;
        }

        float finalVolume = volume * sfxVolumeMultiplier;
        float pitch = Random.Range(pitchMin, pitchMax);
        Debug.Log($"[AudioManager] Play3D: clip='{clip.name}', position={position}, volume={volume}, sfxMultiplier={sfxVolumeMultiplier}, finalVolume={finalVolume}, minDist={minDistance}, maxDist={maxDistance}.");

        if (finalVolume <= 0f)
        {
            Debug.LogWarning($"[AudioManager] Play3D: volume finale è {finalVolume}. Il suono non sarà udibile! Controlla sfxVolumeMultiplier ({sfxVolumeMultiplier}) e il volume del SoundEffect ({volume}).");
        }

        GameObject go = new GameObject("TempSFX_3D");
        go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = finalVolume;
        source.pitch = pitch;
        source.spatialBlend = 1f; // 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.Play();

        Debug.Log($"[AudioManager] Play3D: AudioSource.isPlaying={source.isPlaying} dopo Play(). Distanza camera: {(Camera.main != null ? Vector3.Distance(Camera.main.transform.position, position).ToString("F2") : "N/A")}.");

        Destroy(go, clip.length + 0.5f);
    }

    /// <summary>
    /// Plays a 3D sound effect at a specific position.
    /// </summary>
    public void Play3D(SoundEffect effect, Vector3 position, float pitchMin = 0.95f, float pitchMax = 1.05f, float minDistance = 1f, float maxDistance = 20f)
    {
        if (effect.clip == null)
        {
            Debug.LogWarning("[AudioManager] Play3D(SoundEffect): il campo 'clip' del SoundEffect è null. Assegna un AudioClip nell'Inspector.");
            return;
        }
        Play3D(effect.clip, position, effect.volume, pitchMin, pitchMax, minDistance, maxDistance);
    }

    /// <summary>
    /// Starts playing a looping sound, or resumes it if it was already playing.
    /// </summary>
    public void StartLoop(AudioClip clip, string loopId, float targetVolume = 1f, float fadeTime = 0.15f)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] StartLoop '{loopId}': clip è null. Impossibile avviare il loop.");
            return;
        }
        if (string.IsNullOrEmpty(loopId))
        {
            Debug.LogWarning("[AudioManager] StartLoop: loopId è vuoto o null.");
            return;
        }

        if (targetVolume <= 0f)
        {
            Debug.LogWarning($"[AudioManager] StartLoop '{loopId}': targetVolume={targetVolume}. Il loop non sarà udibile! Controlla il volume del SoundEffect.");
        }

        float effectiveFinalVolume = targetVolume * sfxVolumeMultiplier;
        Debug.Log($"[AudioManager] StartLoop '{loopId}': clip='{clip.name}', targetVolume={targetVolume}, sfxMultiplier={sfxVolumeMultiplier}, volumeFinale={effectiveFinalVolume}.");

        if (activeLoops.TryGetValue(loopId, out ActiveLoop activeLoop))
        {
            Debug.Log($"[AudioManager] StartLoop '{loopId}': loop già attivo, ripresa con fade-in.");
            // Already active. If it was fading out, stop fading and fade back in.
            if (activeLoop.fadeCoroutine != null)
            {
                StopCoroutine(activeLoop.fadeCoroutine);
            }
            activeLoop.fadeCoroutine = StartCoroutine(FadeSource(activeLoop, targetVolume, fadeTime, false));
            return;
        }

        GameObject go = new GameObject($"LoopSFX_{loopId}");
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f; // 2D loop by default
        source.volume = 0f;
        source.Play();

        Debug.Log($"[AudioManager] StartLoop '{loopId}': AudioSource.isPlaying={source.isPlaying} dopo Play().");

        ActiveLoop newLoop = new ActiveLoop
        {
            id = loopId,
            source = source,
            baseVolume = 0f
        };
        activeLoops[loopId] = newLoop;

        newLoop.fadeCoroutine = StartCoroutine(FadeSource(newLoop, targetVolume, fadeTime, false));
    }

    /// <summary>
    /// Starts playing a looping sound effect.
    /// </summary>
    public void StartLoop(SoundEffect effect, string loopId, float fadeTime = 0.15f)
    {
        if (effect.clip == null)
        {
            Debug.LogWarning($"[AudioManager] StartLoop(SoundEffect) '{loopId}': il campo 'clip' del SoundEffect è null. Assegna un AudioClip nell'Inspector.");
            return;
        }
        StartLoop(effect.clip, loopId, effect.volume, fadeTime);
    }

    /// <summary>
    /// Stops a looping sound with a smooth fade-out.
    /// </summary>
    public void StopLoop(string loopId, float fadeTime = 0.15f)
    {
        if (string.IsNullOrEmpty(loopId))
        {
            Debug.LogWarning("[AudioManager] StopLoop: loopId è vuoto o null.");
            return;
        }
        if (!activeLoops.TryGetValue(loopId, out ActiveLoop activeLoop))
        {
            Debug.LogWarning($"[AudioManager] StopLoop '{loopId}': nessun loop attivo trovato con questo ID. Loop attivi: [{string.Join(", ", activeLoops.Keys)}].");
            return;
        }

        Debug.Log($"[AudioManager] StopLoop '{loopId}': avvio fade-out in {fadeTime}s.");

        if (activeLoop.fadeCoroutine != null)
        {
            StopCoroutine(activeLoop.fadeCoroutine);
        }

        activeLoop.fadeCoroutine = StartCoroutine(FadeSource(activeLoop, 0f, fadeTime, true));
    }

    private IEnumerator FadeSource(ActiveLoop loop, float targetBaseVolume, float duration, bool destroyOnComplete)
    {
        float startBaseVolume = loop.baseVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (loop == null || loop.source == null) yield break;
            elapsed += Time.deltaTime;
            loop.baseVolume = Mathf.Lerp(startBaseVolume, targetBaseVolume, elapsed / duration);
            loop.source.volume = loop.baseVolume * sfxVolumeMultiplier;
            yield return null;
        }

        if (loop != null && loop.source != null)
        {
            loop.baseVolume = targetBaseVolume;
            loop.source.volume = targetBaseVolume * sfxVolumeMultiplier;
            if (destroyOnComplete)
            {
                loop.source.Stop();
                Destroy(loop.source.gameObject);
                activeLoops.Remove(loop.id);
            }
        }
    }
}
