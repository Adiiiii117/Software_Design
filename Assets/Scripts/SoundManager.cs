using UnityEngine;
using UnityEngine.SceneManagement;

public enum SeType
{
    Move,
    Rotate,
    HardDrop,
    Lock,
    LineClear,
    StageClear,
    StageFail,
    Hold,
    ButtonClick,
}

public enum BgmType
{
    None,
    Title,
    Menu,
    InGame,
}

public class SoundManager : MonoBehaviour
{
    // シングルトン
    public static SoundManager Instance { get; private set; }

    [Header("BGM Sources & Clips")]
    public AudioSource bgmSource;
    public AudioClip titleBGM;
    public AudioClip menuBGM;
    public AudioClip inGameBGM;

    [Header("BGM Volumes (per BGM, 0〜1)")]
    public float titleBGMVolume = 1f;
    public float menuBGMVolume = 1f;
    public float inGameBGMVolume = 1f;

    [Header("SE Source & Clips")]
    public AudioSource seSource;

    public AudioClip moveSE;
    public AudioClip rotateSE;
    public AudioClip hardDropSE;
    public AudioClip lockSE;
    public AudioClip lineClearSE;
    public AudioClip stageClearSE;
    public AudioClip stageFailSE;
    public AudioClip holdSE;
    public AudioClip buttonClickSE;

    [Header("SE Volumes (per SE, 0〜1)")]
    public float moveSEVolume = 1f;
    public float rotateSEVolume = 1f;
    public float hardDropSEVolume = 1f;
    public float lockSEVolume = 1f;
    public float lineClearSEVolume = 1f;
    public float stageClearSEVolume = 1f;
    public float stageFailSEVolume = 1f;
    public float holdSEVolume = 1f;
    public float buttonClickSEVolume = 1f;

    private void Awake()
    {
        // シングルトン化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM Source 自動生成
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        // SE Source 自動生成
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.loop = false;
            seSource.playOnAwake = false;
        }

        // ★ シーン読み込みイベント登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // イベント解除
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // ========================================================================
    //                               SE 再生
    // ========================================================================
    public void PlaySE(SeType type)
    {
        if (seSource == null) return;

        AudioClip clip = null;
        float volumeScale = 1f;

        switch (type)
        {
            case SeType.Move:        clip = moveSE;        volumeScale = moveSEVolume; break;
            case SeType.Rotate:      clip = rotateSE;      volumeScale = rotateSEVolume; break;
            case SeType.HardDrop:    clip = hardDropSE;    volumeScale = hardDropSEVolume; break;
            case SeType.Lock:        clip = lockSE;        volumeScale = lockSEVolume; break;
            case SeType.LineClear:   clip = lineClearSE;   volumeScale = lineClearSEVolume; break;
            case SeType.StageClear:  clip = stageClearSE;  volumeScale = stageClearSEVolume; break;
            case SeType.StageFail:   clip = stageFailSE;   volumeScale = stageFailSEVolume; break;
            case SeType.Hold:        clip = holdSE;        volumeScale = holdSEVolume; break;
            case SeType.ButtonClick: clip = buttonClickSE; volumeScale = buttonClickSEVolume; break;
        }

        if (clip != null && volumeScale > 0f)
        {
            seSource.PlayOneShot(clip, volumeScale);
        }
    }

    // ========================================================================
    //                               BGM 再生
    // ========================================================================
    public void PlayBGM(BgmType type)
    {
        if (bgmSource == null) return;

        AudioClip clip = null;
        float volume = 1f;

        switch (type)
        {
            case BgmType.Title:
                clip = titleBGM;
                volume = titleBGMVolume;
                break;

            case BgmType.Menu:
                clip = menuBGM;
                volume = menuBGMVolume;
                break;

            case BgmType.InGame:
                clip = inGameBGM;
                volume = inGameBGMVolume;
                break;

            case BgmType.None:
                clip = null;
                break;
        }

        if (clip == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }

        // すでに同じ曲が再生中ならスキップ
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // ========================================================================
    //                         シーン名で BGM 自動切替
    // ========================================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // --- タイトル ---
        if (sceneName.Contains("Title"))
        {
            PlayBGM(BgmType.Title);
            return;
        }

        // --- メニュー系シーン (TechniqueSelect / StageSelect) ---
        if (sceneName.Contains("TechniqueSelect") ||
            sceneName.Contains("StageSelect"))
        {
            PlayBGM(BgmType.Menu);
            return;
        }

        // --- ゲーム中ステージ (REN/TSD/TST 系の E/N/H) ---
        if (sceneName.Contains("REN_E") ||
            sceneName.Contains("REN_N") ||
            sceneName.Contains("REN_H") ||
            sceneName.Contains("TSD_B") ||
            sceneName.Contains("TSD_E") ||
            sceneName.Contains("TSD_N") ||
            sceneName.Contains("TSD_H") ||
            sceneName.Contains("TST_E") ||
            sceneName.Contains("TST_N") ||
            sceneName.Contains("TST_H"))
        {
            PlayBGM(BgmType.InGame);
            return;
        }

        // --- それ以外 ---
        PlayBGM(BgmType.None);
    }

    // ========================================================================
    //                               ポーズ連動
    // ========================================================================
    public void SetPaused(bool paused)
    {
        if (bgmSource != null)
        {
            if (paused) bgmSource.Pause();
            else bgmSource.UnPause();
        }

        if (seSource != null)
        {
            if (paused) seSource.Pause();
            else seSource.UnPause();
        }
    }
}
