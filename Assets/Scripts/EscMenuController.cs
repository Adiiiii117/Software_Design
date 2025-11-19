using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenuController : MonoBehaviour
{
    [Header("UI Root")]
    [Tooltip("ESCメニューのパネル（Canvas配下のPanel）")]
    public GameObject escMenuPanel;

    [Header("Scene Names")]
    [Tooltip("テクニック選択画面のシーン名")]
    public string techniqueSelectSceneName = "TechniqueSelect";

    [Tooltip("タイトル画面のシーン名")]
    public string titleSceneName = "Title";

    private bool isOpen = false;

    private void Start()
    {
        if (escMenuPanel != null)
            escMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // ESCでメニューの開閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();

            // ★ ESC押したときもクリック音
            SoundManager.Instance?.PlaySE(SeType.ButtonClick);
        }
    }

    private void ToggleMenu()
    {
        isOpen = !isOpen;

        if (escMenuPanel != null)
            escMenuPanel.SetActive(isOpen);

        Time.timeScale = isOpen ? 0f : 1f;
    }

    // ===== ボタン用メソッド =====

    public void OnTechniqueSelectButton()
    {
        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(techniqueSelectSceneName))
        {
            Debug.LogWarning("EscMenuController: techniqueSelectSceneName が設定されていません。");
            return;
        }

        SceneManager.LoadScene(techniqueSelectSceneName);
    }

    public void OnTitleButton()
    {
        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogWarning("EscMenuController: titleSceneName が設定されていません。");
            return;
        }

        SceneManager.LoadScene(titleSceneName);
    }

    public void OnCloseButton()
    {
        if (!isOpen) return;

        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        ToggleMenu();
    }
}
