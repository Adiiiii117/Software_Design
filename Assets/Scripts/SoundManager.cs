using UnityEngine;

public enum SeType
{
    Move,
    Rotate,
    HardDrop,
    Lock,
    LineClear,
    TSpinSuccess,
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

    [Header("BGM")]
    public AudioSource bgmSource;      // BGM 用オーディオソース
    public AudioClip titleBGM;
    public AudioClip menuBGM;
    public AudioClip inGameBGM;

    [Header("SE Source")]
    public AudioSource seSource;       // SE 用オーディオソース（PlayOneShot 用）

    [Header("SE Clips")]
    public AudioClip moveSE;
    public AudioClip rotateSE;
    public AudioClip hardDropSE;
    public AudioClip lockSE;
    public AudioClip lineClearSE;
    public AudioClip tSpinSuccessSE;
    public AudioClip stageClearSE;
    public AudioClip stageFailSE;
    public AudioClip holdSE;
    public AudioClip buttonClickSE;

    [Header("SE Volumes (per clip, 0〜1)")]
    [Tooltip("ミノ移動音の個別ボリューム")]
    public float moveSEVolume        = 1f;
    [Tooltip("回転音の個別ボリューム")]
    public float rotateSEVolume      = 1f;
    [Tooltip("ハードドロップ音の個別ボリューム")]
    public float hardDropSEVolume    = 1f;
    [Tooltip("ロック（接地）音の個別ボリューム")]
    public float lockSEVolume        = 1f;
    [Tooltip("ライン消去音の個別ボリューム")]
    public float lineClearSEVolume   = 1f;
    [Tooltip("Tスピン成功音の個別ボリューム")]
    public float tSpinSuccessSEVolume = 1f;
    [Tooltip("ステージクリア音の個別ボリューム")]
    public float stageClearSEVolume  = 1f;
    [Tooltip("ステージ失敗音の個別ボリューム")]
    public float stageFailSEVolume   = 1f;
    [Tooltip("ホールド音の個別ボリューム")]
    public float holdSEVolume        = 1f;
    [Tooltip("ボタンクリック音の個別ボリューム")]
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

        // 万が一 AudioSource がアタッチされていない場合、ここで自動追加
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.loop = false;
            seSource.playOnAwake = false;
        }
    }

    // ==== SE 再生 ====

    public void PlaySE(SeType type)
    {
        if (seSource == null) return;

        AudioClip clip = null;
        float volumeScale = 1f;  // このSE専用の倍率（0〜1 推奨）

        switch (type)
        {
            case SeType.Move:
                clip = moveSE;
                volumeScale = moveSEVolume;
                break;

            case SeType.Rotate:
                clip = rotateSE;
                volumeScale = rotateSEVolume;
                break;

            case SeType.HardDrop:
                clip = hardDropSE;
                volumeScale = hardDropSEVolume;
                break;

            case SeType.Lock:
                clip = lockSE;
                volumeScale = lockSEVolume;
                break;

            case SeType.LineClear:
                clip = lineClearSE;
                volumeScale = lineClearSEVolume;
                break;

            case SeType.TSpinSuccess:
                clip = tSpinSuccessSE;
                volumeScale = tSpinSuccessSEVolume;
                break;

            case SeType.StageClear:
                clip = stageClearSE;
                volumeScale = stageClearSEVolume;
                break;

            case SeType.StageFail:
                clip = stageFailSE;
                volumeScale = stageFailSEVolume;
                break;

            case SeType.Hold:
                clip = holdSE;
                volumeScale = holdSEVolume;
                break;

            case SeType.ButtonClick:
                clip = buttonClickSE;
                volumeScale = buttonClickSEVolume;
                break;
        }

        if (clip != null && volumeScale > 0f)
        {
            // 実際の最終音量 = seSource.volume（全体） × volumeScale（個別）
            seSource.PlayOneShot(clip, volumeScale);
        }
    }

    // BGM 再生 

    public void PlayBGM(BgmType type)
    {
        if (bgmSource == null) return;

        AudioClip clip = null;

        switch (type)
        {
            case BgmType.Title:  clip = titleBGM;   break;
            case BgmType.Menu:   clip = menuBGM;    break;
            case BgmType.InGame: clip = inGameBGM;  break;
            case BgmType.None:   clip = null;       break;
        }

        // BGM なしにしたい場合
        if (clip == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }

        // 同じ曲がすでに鳴っているなら何もしない
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
    }

    // ==== ポーズ連動（任意） ====

    public void SetPaused(bool paused)
    {
        if (bgmSource != null)
        {
            if (paused) bgmSource.Pause();
            else        bgmSource.UnPause();
        }

        if (seSource != null)
        {
            if (paused) seSource.Pause();
            else        seSource.UnPause();
        }
    }
}
