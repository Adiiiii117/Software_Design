using UnityEngine;
using UnityEngine.SceneManagement;

public class EscMenuController : MonoBehaviour
{
    [Header("UI Root")]
    [Tooltip("ESCメニューのパネル（Canvas配下のPanel）")]
    public GameObject escMenuRoot;

    [Header("Scene Names")]
    [Tooltip("テクニック選択画面のシーン名")]
    public string techniqueSelectSceneName = "TechniqueSelect";

    [Tooltip("タイトル画面のシーン名")]
    public string titleSceneName = "Title";

    private bool isOpen = false;

    private void Start()
    {
        // 最初はメニューを隠しておく
        if (escMenuRoot != null)
            escMenuRoot.SetActive(false);
    }

    private void Update()
    {
        // ESCでメニューを開閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        isOpen = !isOpen;

        if (escMenuRoot != null)
            escMenuRoot.SetActive(isOpen);

        // メニューを開いている間はゲームを停止
        Time.timeScale = isOpen ? 0f : 1f;
    }

    // ===== ボタン用メソッド =====

    // 「Technique Select に戻る」ボタン
    public void OnTechniqueSelectButton()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(techniqueSelectSceneName))
        {
            Debug.LogWarning("EscMenuController: techniqueSelectSceneName が設定されていません。");
            return;
        }
        SceneManager.LoadScene(techniqueSelectSceneName);
    }

    // 「Title に戻る」ボタン
    public void OnTitleButton()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(titleSceneName))
        {
            Debug.LogWarning("EscMenuController: titleSceneName が設定されていません。");
            return;
        }
        SceneManager.LoadScene(titleSceneName);
    }

    // 「閉じる」ボタンを置きたい時用（任意）
    public void OnCloseButton()
    {
        if (!isOpen) return;
        ToggleMenu();
    }
}
