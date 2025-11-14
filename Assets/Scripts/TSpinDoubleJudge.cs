using UnityEngine;
using UnityEngine.SceneManagement;

public class TSpinDoubleJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    [Tooltip("成功時に表示するUIルート（クリアパネル）")]
    public GameObject clearUIRoot;

    [Tooltip("ステージ一覧シーン名（Technique Select など）")]
    public string stageSelectSceneName = "TechniqueSelect";

    [Tooltip("次のステージのシーン名（なければ空でOK）")]
    public string nextStageSceneName = "";

    [Tooltip("クリア時に Time.timeScale = 0 にするか")]
    public bool stopTimeOnClear = true;

    /// <summary>ステージがクリア状態かどうか</summary>
    public bool IsStageCleared { get; private set; } = false;

    /// <summary>Easy系モードかどうか（TSD_E / TSD_B）</summary>
    private bool isEasyLikeMode = false;

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // ★ TSD_E, TSD_B はどちらも「Easy系」扱い（失敗で自動リスタート）
        isEasyLikeMode = sceneName.Contains("TSD_E") || sceneName.Contains("TSD_B");

        if (clearUIRoot != null) clearUIRoot.SetActive(false);
    }

    /// <summary>
    /// Tetromino がロックされたときに Tetromino 側から呼ばれる。
    /// </summary>
    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        // このJudgeはTスピンダブル用なので、T以外は無視
        // tetrominoPrefabs が I,J,L,O,S,T,Z なら T は index 5
        if (piece.typeIndex != 5) return;

        if (isEasyLikeMode)
        {
            // ★ Easy系（TSD_E / TSD_B）:
            //    Tが固定されたら、2列消えていれば成功、
            //    それ以外は即リスタート（失敗UIは出さない）
            if (linesCleared == 2)
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
            //    Tで2列消した場合のみクリア（失敗してもゲーム続行）
            if (linesCleared == 2)
            {
                HandleStageClear();
            }
        }
    }

    /// <summary>ステージクリア処理。</summary>
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

    /// <summary>Easy系モードで失敗したときにシーンを即リスタート。</summary>
    private void ForceRestartScene()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // ===== クリアUIのボタン用 =====

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
            Debug.LogWarning("TSpinDoubleJudge: nextStageSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageSceneName);
    }

    public void OnStageSelectButton()
    {
        if (string.IsNullOrEmpty(stageSelectSceneName))
        {
            Debug.LogWarning("TSpinDoubleJudge: stageSelectSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
