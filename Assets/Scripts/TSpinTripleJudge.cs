using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// T-Spin Triple (TST) 用のクリア判定。
/// T固定時に3列消したらクリア。
/// TST_E / TST_B は失敗で自動リスタート、それ以外は失敗しても続行。
/// </summary>
public class TSpinTripleJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    [Tooltip("成功時に表示するUIルート（クリアパネル）")]
    public GameObject clearUIRoot;

    [Tooltip("ステージ一覧シーン名")]
    public string stageSelectSceneName = "TechniqueSelect";

    [Tooltip("次のステージのシーン名（なければ空でOK）")]
    public string nextStageSceneName = "";

    [Tooltip("クリア時に Time.timeScale = 0 にするか")]
    public bool stopTimeOnClear = true;

    public bool IsStageCleared { get; private set; } = false;

    /// <summary>TST_E / TST_B のような Easy系モードかどうか</summary>
    private bool isEasyLikeMode = false;

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // ★ TST_E, TST_B は Easy系扱い（失敗で自動リスタート）
        isEasyLikeMode = sceneName.Contains("TST_E") || sceneName.Contains("TST_B");

        if (clearUIRoot != null) clearUIRoot.SetActive(false);
    }

    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        // Tミノ以外は無視
        if (piece.typeIndex != 5) return;

        if (isEasyLikeMode)
        {
            // ★ Easy系（TST_E / TST_B）:
            //    Tで3列消したらクリア、それ以外は即リスタート
            if (linesCleared == 3)
            {
                HandleStageClear();
            }
            else
            {
                ForceRestartScene();
            }
        }
        else
        {
            // ★ Normal / Hard:
            //    Tで3列消したときだけクリア
            if (linesCleared == 3)
            {
                HandleStageClear();
            }
        }
    }

    private void HandleStageClear()
    {
        IsStageCleared = true;

        var controlUI = FindObjectOfType<GameControlUI>();
        if (controlUI != null)
            controlUI.HideAllUI();

        // ★ 追加：タイマー停止
        if (GameTimer.Instance != null)
            GameTimer.Instance.StopTimer();

        if (clearUIRoot != null)
            clearUIRoot.SetActive(true);

        if (stopTimeOnClear)
            Time.timeScale = 0f;
    }

    private void ForceRestartScene()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // クリアUIボタン用
    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void OnNextStageButton()
    {
        if (string.IsNullOrEmpty(nextStageSceneName))
        {
            Debug.LogWarning("TSpinTripleJudge: nextStageSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageSceneName);
    }

    public void OnStageSelectButton()
    {
        if (string.IsNullOrEmpty(stageSelectSceneName))
        {
            Debug.LogWarning("TSpinTripleJudge: stageSelectSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
