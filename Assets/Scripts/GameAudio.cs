using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal enum SoundEffect
{
    Dish,
    Cash,
    Click,
    LevelUp,
    TrashCan
}

public sealed class GameAudio : MonoBehaviour
{
    [System.Serializable]
    private sealed class Sound
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [System.NonSerialized] public AudioSource source;
    }

    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField] private Sound dish = new() { volume = 0.55f };
    [SerializeField] private Sound cash = new() { volume = 0.65f };
    [SerializeField] private Sound click = new() { volume = 0.35f };
    [SerializeField] private Sound levelUp = new() { volume = 0.65f };
    [SerializeField] private Sound trashCan = new() { volume = 0.65f };
    [SerializeField] private Sound robot = new() { volume = 0.45f };

    private static GameAudio instance;
    private Sound[] effects;
    private bool settingsChanged;
    private readonly HashSet<ScratchBot> scratchingBots = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameAudio prefab = Resources.Load<GameAudio>("GameAudio");
        if (prefab != null)
            Instantiate(prefab);
    }

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        effects = new[] { dish, cash, click, levelUp, trashCan };
        foreach (Sound effect in effects)
            CreateSource(effect);

        CreateSource(robot, loop: true);
        SceneManager.sceneLoaded += RegisterSceneButtons;
    }

    private void CreateSource(Sound sound, bool loop = false)
    {
        var audioObject = new GameObject(sound.clip != null ? sound.clip.name : "Unassigned Sound");
        audioObject.transform.SetParent(transform, false);
        sound.source = audioObject.AddComponent<AudioSource>();
        sound.source.playOnAwake = false;
        sound.source.loop = loop;
        sound.source.spatialBlend = 0f;
        ApplySettings(sound);
    }

    private void OnValidate()
    {
        settingsChanged = true;
    }

    private void ApplySettings(Sound sound)
    {
        AudioSource source = sound.source;
        if (source.clip != sound.clip)
        {
            bool wasPlaying = source.isPlaying;
            source.Stop();
            source.clip = sound.clip;
            if (source.loop && wasPlaying && source.clip != null)
                source.Play();
        }

        source.volume = masterVolume * sound.volume;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= RegisterSceneButtons;
        if (instance == this)
            instance = null;
    }

    internal static void Play(SoundEffect effect)
    {
        if (instance == null)
            return;

        AudioSource source = instance.effects[(int)effect].source;
        if (source.clip != null)
            source.PlayOneShot(source.clip);
    }

    internal static void SetRobotScratching(ScratchBot bot, bool scratching)
    {
        if (instance == null)
            return;

        if (scratching)
            instance.scratchingBots.Add(bot);
        else
            instance.scratchingBots.Remove(bot);
    }

    private void LateUpdate()
    {
        if (settingsChanged)
        {
            settingsChanged = false;
            foreach (Sound effect in effects)
                ApplySettings(effect);
            ApplySettings(robot);
        }

        SetLoop(robot.source, scratchingBots.Count > 0 && Time.timeScale > 0f);
    }

    private static void SetLoop(AudioSource source, bool playing)
    {
        if (playing && source.clip != null && !source.isPlaying)
            source.Play();
        else if (!playing && source.isPlaying)
            source.Stop();
    }

    private static void RegisterSceneButtons(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            RegisterButtons(root);
    }

    internal static void RegisterButtons(GameObject root)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            RegisterButton(button);
    }

    internal static void RegisterButton(Button button)
    {
        button.onClick.RemoveListener(PlayClick);
        if (button.GetComponentInParent<Draggable>(true) != null)
            return;

        var catalogSlot = button.GetComponentInParent<CatalogSlot>(true);
        if (catalogSlot != null && catalogSlot.PurchaseButton == button)
            return;

        button.onClick.AddListener(PlayClick);
    }

    private static void PlayClick()
    {
        Play(SoundEffect.Click);
    }
}
