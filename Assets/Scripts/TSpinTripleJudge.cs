using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TSpinTripleJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    public GameObject clearUIRoot;
    public string stageSelectSceneName = "TechniqueSelect";
    public string nextStageSceneName = "";
    public bool stopTimeOnClear = true;

    [Header("Clear Animation")]
    public ClearFaridUI clearFaridUI;

    [Header("Clear Texts")]
    public Text clearMessageText;
    public Text timeText;

    public bool IsStageCleared { get; private set; } = false;

    bool isEasyLikeMode = false;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // Easy 系モードかどうか
        isEasyLikeMode = sceneName.Contains("TST_E") || sceneName.Contains("TST_B");

        if (clearUIRoot != null)
            clearUIRoot.SetActive(false);
    }

    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        if (piece.typeIndex != 5) return;

        if (isEasyLikeMode)
        {
            if (linesCleared == 3)
            {
                Debug.Log("[TST] SUCCESS (Easy): 3 lines cleared -> HandleStageClear()");
                SoundManager.Instance?.PlaySE(SeType.StageClear);
                HandleStageClear();
            }
            else
            {
                Debug.Log($"[TST] FAIL (Easy): linesCleared={linesCleared}, time={GetClearTimeSeconds():F2} sec -> ForceRestartScene()");
                SoundManager.Instance?.PlaySE(SeType.StageFail);
                ForceRestartScene();
            }
        }
        else
        {
            if (linesCleared == 3)
            {
                SoundManager.Instance?.PlaySE(SeType.StageClear);
                HandleStageClear();
            }
        }
    }

    void HandleStageClear()
    {
        Debug.Log("[TST] HandleStageClear START");

        IsStageCleared = true;

        var controlUI = FindObjectOfType<GameControlUI>();
        if (controlUI != null)
            controlUI.HideAllUI();

        if (GameTimer.Instance != null)
            GameTimer.Instance.StopTimer();

        float clearTime = GetClearTimeSeconds();
        int spriteIndex = GetSpriteIndexByTime(clearTime, isEasyLikeMode);

        UpdateClearTexts(clearTime, spriteIndex);

        if (clearFaridUI == null)
        {
            if (clearUIRoot != null)
                clearUIRoot.SetActive(true);

            if (stopTimeOnClear)
                Time.timeScale = 0f;

            Debug.Log("[TST] HandleStageClear: FaridUI=null -> panel only");
            return;
        }

        Debug.Log($"[TST] HandleStageClear: time={clearTime:F2}, easy={isEasyLikeMode}, spriteIndex={spriteIndex}");

        clearFaridUI.SetImageByIndex(spriteIndex);
        clearFaridUI.Play();

        if (stopTimeOnClear)
            Time.timeScale = 0f;

        Debug.Log("[TST] HandleStageClear END");
    }

    float GetClearTimeSeconds()
    {
        if (GameTimer.Instance == null)
            return 0f;

        return GameTimer.Instance.GetClearTime();
    }

    int GetSpriteIndexByTime(float seconds, bool easyMode)
    {
        if (easyMode)
        {
            if (seconds >= 60f) return 0;
            if (seconds > 30f)  return 1;
            if (seconds > 20f)  return 2;
            if (seconds > 10f)  return 3;
            return 4;
        }
        else
        {
            if (seconds >= 180f) return 0;
            if (seconds > 120f)  return 1;
            if (seconds > 60f)   return 2;
            if (seconds > 30f)   return 3;
            return 4;
        }
    }

    void UpdateClearTexts(float clearTime, int spriteIndex)
    {
        if (timeText != null)
            timeText.text = $"You took {clearTime:F2} seconds";

        if (clearMessageText != null)
            clearMessageText.text = GetTimeCommentByIndex(spriteIndex);
    }

    string GetTimeCommentByIndex(int index)
    {
        switch (index)
        {
            case 0:
                return "..... Well, unfortunately the reality is cruel";
            case 1:
                return "sigh... You better try hard...";
            case 2:
                return "Not bad, but you could more better";
            case 3:
                return "Wow, you pretty good at T-Spin!";
            default:
                return "You are God Tetris Player";
        }
    }

    void ForceRestartScene()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void OnRetryButton()
    {
        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        if (clearUIRoot != null)
            clearUIRoot.SetActive(false);
        if (clearFaridUI != null)
            clearFaridUI.gameObject.SetActive(false);

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();

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

        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

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

        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
