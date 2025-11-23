using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenuController : MonoBehaviour
{
    [Header("UI Root")]
    [Tooltip("ESCメニューのパネル（Canvas配下のPanel）")]
    public GameObject escMenuPanel;

    [Tooltip("操作方法パネル（Canvas配下のPanel）")]
    public GameObject howToPlayPanel;   // ★ 新しく追加！

    [Header("Scene Names")]
    [Tooltip("テクニック選択画面のシーン名")]
    public string stageSelectSceneName = "StageSelect";

    [Tooltip("タイトル画面のシーン名")]
    public string titleSceneName = "Title";

    private bool isMenuOpen = false;       // ESCメニューが開いているか
    private bool isHowToOpen = false;      // 操作説明が開いているか

    private void Start()
    {
        if (escMenuPanel != null)
            escMenuPanel.SetActive(false);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    private void Update()
    {
        // ESCキーでメニューの開閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SoundManager.Instance?.PlaySE(SeType.ButtonClick);

            // ★ 操作説明表示中なら → 操作説明だけ閉じる
            if (isHowToOpen)
            {
                CloseHowToPlay();
                return;
            }

            // ★ 通常のESCメニュー開閉
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (escMenuPanel != null)
            escMenuPanel.SetActive(isMenuOpen);

        Time.timeScale = isMenuOpen ? 0f : 1f;
    }

    // =========================================================
    //  操作説明パネルを開く
    // =========================================================
    public void OnOpenHowToPlayButton()
    {
        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);

        if (escMenuPanel != null)
            escMenuPanel.SetActive(false);

        isHowToOpen = true;
        isMenuOpen = false;
    }

    // 操作説明パネルを閉じる
    private void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        isHowToOpen = false;

        // メニューを開いたままに戻す
        if (escMenuPanel != null)
            escMenuPanel.SetActive(true);

        isMenuOpen = true;
    }

    // =========================================================
    //  既存のボタン機能
    // =========================================================
    public void OnStageSelectButton()
    {
        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(stageSelectSceneName)) return;
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void OnTitleButton()
    {
        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(titleSceneName)) return;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnCloseButton()
    {
        if (!isMenuOpen) return;

        SoundManager.Instance?.PlaySE(SeType.ButtonClick);
        ToggleMenu();
    }
}
