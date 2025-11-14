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

        UpdateClearMessage();
    }

    /// <summary>クリアUI上のテキストを、達成RENに応じて変える</summary>
    private void UpdateClearMessage()
    {
        if (resultLabel == null) return;

        // クリアタイム（タイマー未配置なら 0）
        float clearTime = (GameTimer.Instance != null) ? GameTimer.Instance.GetClearTime() : 0f;

        // 既存ロジックをベースに「ランク or クリア文言」を作る
        string head;
        if (isEasyMode)
        {
            if (maxRen >= targetRenForEasy)
            {
                // 目標達成
                head = $"CLEAR! {targetRenForEasy} REN 達成！";
            }
            else
            {
                // 未達（通常ここは表示されない想定だが安全に）
                int remain = Mathf.Max(0, targetRenForEasy - maxRen);
                head = $"{maxRen} REN（あと {remain} でクリア）";
            }
        }
        else
        {
            // Normal / Hard のランク表現（従来表示を踏襲）
            if (maxRen < 3)       head = $"{maxRen} REN";
            else if (maxRen == 3) head = $"Nice! {maxRen} REN!!";
            else if (maxRen == 5) head = $"Great! {maxRen} REN!!";
            else                  head = $"Excellent! {maxRen} REN!!!";
        }

        // ★ ここからが追加ポイント：必ず MAX REN と TIME を下に出す
        string msg = $"{head}\nMAX REN: {maxRen}\nTIME: {clearTime:F2}s";

        resultLabel.text = msg;
    }
}
