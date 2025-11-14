using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameControlUI : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("ポーズ中にうっすら表示するオーバーレイ（なければ空でOK）")]
    public GameObject pauseOverlay;

    [Header("Pause / Play Button")]
    [Tooltip("右側の一時停止ボタンの Image コンポーネント")]
    public Image pauseButtonImage;   // Pause_Button の Image

    [Tooltip("通常再生中に表示する『一時停止』アイコン")]
    public Sprite pauseSprite;       // 「||」 のアイコン

    [Tooltip("一時停止中に表示する『再生』アイコン")]
    public Sprite playSprite;        // 「▶」 のアイコン

    private bool isPaused = false;

    private void Start()
    {
        Time.timeScale = 1f;          // 念のため毎シーン開始時に再生状態に
        UpdatePauseButtonVisual();    // 最初は一時停止アイコンを表示
    }

    // 🔁 左のリスタートボタン用
    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // ⏸ / ▶ 右のボタン用（同じボタンでトグル）
    public void OnPauseButton()
    {
        isPaused = !isPaused;

        // 時間を止める / 再開
        Time.timeScale = isPaused ? 0f : 1f;

        // オーバーレイ（あれば）をON/OFF
        if (pauseOverlay != null)
            pauseOverlay.SetActive(isPaused);

        // ボタン見た目を切り替え
        UpdatePauseButtonVisual();
    }

    // ボタンのアイコンを、再生中/停止中で切り替える
    private void UpdatePauseButtonVisual()
    {
        if (pauseButtonImage == null) return;

        // 再生中 → 「||」、一時停止中 → 「▶」
        pauseButtonImage.sprite = isPaused ? playSprite : pauseSprite;
    }

    public void HideAllUI()
    {
        gameObject.SetActive(false);
    }
}


