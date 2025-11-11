using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RenJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    [Tooltip("成功時に表示するUIルート（クリアパネル）")]
    public GameObject clearUIRoot;

    [Tooltip("ステージ一覧（テクニック選択など）のシーン名")]
    public string stageSelectSceneName = "TechniqueSelect";

    [Tooltip("次のステージのシーン名（なければ空でOK）")]
    public string nextStageSceneName = "";

    [Tooltip("クリア時に Time.timeScale = 0 にするか")]
    public bool stopTimeOnClear = true;

    [Header("Result Text (任意)")]
    [Tooltip("クリア時に最大REN数などを表示するテキスト")]
    public Text resultLabel;

    public bool IsStageCleared { get; private set; } = false;

    // RENカウンタ
    private int currentRen = 0;
    private int maxRen = 0;

    // 難易度フラグ
    private bool isEasyMode = false;
    private bool isNormalMode = false;
    private bool isHardMode = false;

    // Easy用：シーン名から決まる目標REN（REN_E_3 なら 3）
    private int targetRenForEasy = 3;

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        isEasyMode   = sceneName.Contains("REN_E");
        isNormalMode = sceneName.Contains("REN_N");
        isHardMode   = sceneName.Contains("REN_H");

        if (isEasyMode)
        {
            // 例: REN_E_3 / REN_E_4 / REN_E_5
            int underscore = sceneName.LastIndexOf('_');
            if (underscore >= 0 && underscore + 1 < sceneName.Length)
            {
                string numPart = sceneName.Substring(underscore + 1);
                if (int.TryParse(numPart, out int n) && n > 0)
                {
                    targetRenForEasy = n;
                }
            }
        }

        if (clearUIRoot != null)
            clearUIRoot.SetActive(false);
    }

    /// <summary>
    /// ミノがロックされたときに Tetromino 側から呼ぶ
    /// </summary>
    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        // RENカウント：ラインが消えたかどうかだけを見る
        if (linesCleared > 0)
        {
            currentRen++;
            if (currentRen > maxRen) maxRen = currentRen;
        }
        else
        {
            currentRen = 0;
        }

        // クリア条件チェック
        if (isEasyMode)
        {
            if (maxRen >= targetRenForEasy)
            {
                HandleStageClear();
            }
        }
        else if (isNormalMode || isHardMode)
        {
            // とりあえず Normal / Hard とも 3REN でクリア
            const int threshold = 3;
            if (maxRen >= threshold)
            {
                HandleStageClear();
            }
        }
        else
        {
            // REN用ステージ以外では何もしない
        }
    }

    /// <summary>ステージクリア処理</summary>
    private void HandleStageClear()
    {
        IsStageCleared = true;

        if (clearUIRoot != null)
            clearUIRoot.SetActive(true);

        if (stopTimeOnClear)
            Time.timeScale = 0f;

        UpdateClearMessage();
    }

    /// <summary>クリアUI上のテキストを、達成RENに応じて変える</summary>
    private void UpdateClearMessage()
    {
        if (resultLabel == null) return;

        string msg;

        if (isEasyMode)
        {
            // 目標レン数との関係でメッセージを変える
            if (maxRen == targetRenForEasy)
            {
                msg = $"CLEAR! {maxRen} REN 達成！";
            }
            else if (maxRen > targetRenForEasy)
            {
                msg = $"すごい！ {maxRen} REN！(必要: {targetRenForEasy})";
            }
            else
            {
                msg = $"{maxRen} REN（あと {targetRenForEasy - maxRen} でクリア）";
            }
        }
        else
        {
            // Normal / Hard 共通のランク演出
            if (maxRen < 3)
            {
                msg = $"{maxRen} REN";
            }
            else if (maxRen == 3)
            {
                msg = $"Nice! {maxRen} REN!!";
            }
            else if (maxRen == 4)
            {
                msg = $"Great! {maxRen} REN!!";
            }
            else
            {
                msg = $"Excellent! {maxRen} REN!!!";
            }
        }

        resultLabel.text = msg;
    }

    // ===== クリアUIのボタン用 =====

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void OnNextStageButton()
    {
        if (string.IsNullOrEmpty(nextStageSceneName))
        {
            Debug.LogWarning("RenJudge: nextStageSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageSceneName);
    }

    public void OnStageSelectButton()
    {
        if (string.IsNullOrEmpty(stageSelectSceneName))
        {
            Debug.LogWarning("RenJudge: stageSelectSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
