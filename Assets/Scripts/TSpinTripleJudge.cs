using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// T-Spin Triple (TST) を決めたらステージクリアにする判定。
/// Tetromino.Lock() から OnPieceLocked が呼ばれる前提。
/// シーン名に TST_E / TST_N / TST_H を含めて使う想定。
/// </summary>
public class TSpinTripleJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    [Tooltip("成功時に表示するUIルート（クリアパネル）")]
    public GameObject clearUIRoot;

    [Tooltip("ステージ一覧（Technique Selectなど）のシーン名")]
    public string stageSelectSceneName = "TechniqueSelect";

    [Tooltip("次のステージのシーン名（なければ空でOK）")]
    public string nextStageSceneName = "";

    [Tooltip("クリア時に Time.timeScale = 0 にするか")]
    public bool stopTimeOnClear = true;

    /// <summary>ステージがクリア状態かどうか</summary>
    public bool IsStageCleared { get; private set; } = false;

    /// <summary>TST_E（初級）モードかどうか</summary>
    private bool isEasyMode = false;

    private void Start()
    {
        // シーン名に "TST_E" を含んでいたら初級モード
        string sceneName = SceneManager.GetActiveScene().name;
        isEasyMode = sceneName.Contains("TST_E");

        if (clearUIRoot != null) clearUIRoot.SetActive(false);
    }

    /// <summary>
    /// Tetromino がロックされたときに Tetromino 側から呼ばれる。
    /// </summary>
    /// <param name="piece">ロックされたミノ</param>
    /// <param name="linesCleared">このロックで消えたライン数</param>
    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        // このJudgeはTST用なので、T以外は無視（基本Tしか出ない想定）
        // tetrominoPrefabs が I,J,L,O,S,T,Z なら T は index 5
        if (piece.typeIndex != 5) return;

        if (isEasyMode)
        {
            // ★ 初級：Tが固定されたら、
            //    3列消えていれば成功、それ以外は即リスタート（失敗UIは出さない）
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
            // ★ Normal / Hard：Tで3列消した場合のみクリア
            if (linesCleared == 3)
            {
                HandleStageClear();
            }
        }
    }

    /// <summary>ステージクリア処理。</summary>
    private void HandleStageClear()
    {
        IsStageCleared = true;

        if (clearUIRoot != null)
            clearUIRoot.SetActive(true);

        if (stopTimeOnClear)
            Time.timeScale = 0f;
    }

    /// <summary>初級モードで失敗したときにシーンを即リスタート。</summary>
    private void ForceRestartScene()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // ===== クリアUIのボタン用 =====

    /// <summary>リトライボタン：現在のステージを再読み込み。</summary>
    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    /// <summary>次のステージへボタン。</summary>
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

    /// <summary>ステージ一覧へボタン。</summary>
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
