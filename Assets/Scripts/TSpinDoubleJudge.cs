using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TSpinDoubleJudge : MonoBehaviour
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
        isEasyLikeMode = sceneName.Contains("TSD_E") || sceneName.Contains("TSD_B");

        if (clearUIRoot != null)
            clearUIRoot.SetActive(false);
    }

    // Tetromino がロックされたときに Tetromino 側から呼ぶ
    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        // Tミノ以外は無視（I,J,L,O,S,T,Z の順なら T は index 5）
        if (piece.typeIndex != 5) return;

        if (isEasyLikeMode)
        {
            if (linesCleared == 2)
            {
                Debug.Log("[TSD] SUCCESS (Easy): 2 lines cleared -> HandleStageClear()");
                
                SoundManager.Instance?.PlaySE(SeType.StageClear);
                HandleStageClear();
            }
            else
            {
                Debug.Log($"[TSD] FAIL (Easy): linesCleared={linesCleared}, time={GetClearTimeSeconds():F2} sec -> ForceRestartScene()");
                SoundManager.Instance?.PlaySE(SeType.StageFail);
                ForceRestartScene();
            }
        }
        else
        {
            // Normal / Hard: T で 2ライン消したときだけクリア
            if (linesCleared == 2)
            {
                
                SoundManager.Instance?.PlaySE(SeType.StageClear);
                HandleStageClear();
            }
        }
    }

    void HandleStageClear()
    {
        Debug.Log("[TSD] HandleStageClear START");

        IsStageCleared = true;

        var controlUI = FindObjectOfType<GameControlUI>();
        if (controlUI != null)
            controlUI.HideAllUI();

        if (GameTimer.Instance != null)
            GameTimer.Instance.StopTimer();

        float clearTime = GetClearTimeSeconds();
        int spriteIndex = GetSpriteIndexByTime(clearTime, isEasyLikeMode);

        // 先にテキストを更新
        UpdateClearTexts(clearTime, spriteIndex);

        if (clearFaridUI == null)
        {
            if (clearUIRoot != null)
                clearUIRoot.SetActive(true);

            if (stopTimeOnClear)
                Time.timeScale = 0f;

            Debug.Log("[TSD] HandleStageClear: FaridUI=null -> panel only");
            return;
        }

        Debug.Log($"[TSD] HandleStageClear: time={clearTime:F2}, easy={isEasyLikeMode}, spriteIndex={spriteIndex}");

        clearFaridUI.SetImageByIndex(spriteIndex);
        clearFaridUI.Play();

        if (stopTimeOnClear)
            Time.timeScale = 0f;

        Debug.Log("[TSD] HandleStageClear END");
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
            // Easy モード
            if (seconds >= 60f) return 0;   // 1分以上
            if (seconds > 30f)  return 1;   // 30〜60秒
            if (seconds > 20f)  return 2;   // 20〜30秒
            if (seconds > 10f)  return 3;   // 10〜20秒
            return 4;                       // 10秒以内
        }
        else
        {
            // Normal / Hard
            if (seconds >= 180f) return 0;  // 3分以上
            if (seconds > 120f)  return 1;  // 2〜3分
            if (seconds > 60f)   return 2;  // 1〜2分
            if (seconds > 30f)   return 3;  // 30〜60秒
            return 4;                       // 30秒以内
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
        // REN の雰囲気に合わせたコメント
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
            Debug.LogWarning("TSpinDoubleJudge: nextStageSceneName が設定されていません。");
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
            Debug.LogWarning("TSpinDoubleJudge: stageSelectSceneName が設定されていません。");
            return;
        }

        SoundManager.Instance?.PlaySE(SeType.ButtonClick);

        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
