using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RENJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    public GameObject clearUIRoot;
    public string stageSelectSceneName = "TechniqueSelect";
    public string nextStageSceneName = "";
    public bool stopTimeOnClear = true;

    [Header("Clear Animation")]
    public ClearFaridUI clearFaridUI;   // ClearFaridUI を入れる

    [Header("UI (REN In Progress)")]
    public Text renNowText;

    [Header("UI (Separate Messages)")]
    public Text renCountText;
    public Text clearMessageText;
    public Text timeText;

    public bool IsStageCleared { get; private set; } = false;

    int currentRen = 0;
    int maxRen = 0;

    bool isEasyMode = false;
    bool isNormalMode = false;
    bool isHardMode = false;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        isEasyMode   = sceneName.Contains("REN_E");
        isNormalMode = sceneName.Contains("REN_N");
        isHardMode   = sceneName.Contains("REN_H");

        if (clearUIRoot != null)
            clearUIRoot.SetActive(false);

        if (renNowText != null)
            renNowText.text = "";
    }

    // ミノがロックされたときに Tetromino 側から呼ぶ
    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        if (linesCleared > 0)
        {
            currentRen++;
            if (currentRen > maxRen)
                maxRen = currentRen;

            if (renNowText != null)
                renNowText.text = $"{currentRen} REN";
        }
        else
        {
            if (renNowText != null)
                renNowText.text = "";

            if (isEasyMode)
            {
                HandleStageClear();
                return;
            }

            currentRen = 0;
        }

        if (isNormalMode || isHardMode)
        {
            const int threshold = 3;
            if (maxRen >= threshold)
            {
                HandleStageClear();
            }
        }
    }

    // ステージクリア時の処理
    void HandleStageClear()
    {
        IsStageCleared = true;

        var controlUI = FindObjectOfType<GameControlUI>();
        if (controlUI != null)
            controlUI.HideAllUI();

        if (stopTimeOnClear)
            Time.timeScale = 0f;

        UpdateClearMessage();

        // REN数に応じて Farid の画像を選ぶ
        if (clearFaridUI != null)
        {
            int spriteIndex = 0;

            if (maxRen == 0)
                spriteIndex = 0;          // 完全失敗
            else if (maxRen <= 3)
                spriteIndex = 1;          // 低 REN
            else if (maxRen <= 5)
                spriteIndex = 2;          // 普通
            else if (maxRen <= 10)
                spriteIndex = 3;          // かなり良い
            else
                spriteIndex = 4;          // 神 REN

            clearFaridUI.SetImageByIndex(spriteIndex);
            clearFaridUI.Play();
        }
        else
        {
            if (clearUIRoot != null)
                clearUIRoot.SetActive(true);
        }
    }

    string GetRenComment(int ren)
    {
        if (ren == 0)
            return "..... Well, unfortunately the reality is cruel";
        if (ren <= 3)
            return "sigh... You better try hard...";
        if (ren <= 5)
            return "Not bad, but you could do more better";
        if (ren <= 10)
            return "Wow, you pretty good at REN";

        return "You are God Tetris Player";
    }

    void UpdateClearMessage()
    {
        if (renCountText != null)
            renCountText.text = $"You did {maxRen} REN";

        if (clearMessageText != null)
            clearMessageText.text = GetRenComment(maxRen);

        if (timeText != null)
            timeText.text = "";
    }

    // ボタン用
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
            Debug.LogWarning("RENJudge: nextStageSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageSceneName);
    }

    public void OnStageSelectButton()
    {
        if (string.IsNullOrEmpty(stageSelectSceneName))
        {
            Debug.LogWarning("RENJudge: stageSelectSceneName が設定されていません。");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
