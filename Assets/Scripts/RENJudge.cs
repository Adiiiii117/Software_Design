using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RENJudge : MonoBehaviour
{
    [Header("UI / Scene Settings")]
    public GameObject clearUIRoot;
    public string stageSelectSceneName = "REN_StageSelect";
    public string nextStageSceneName = "";
    public bool stopTimeOnClear = true;

    [Header("Clear Animation")]
    public ClearFaridUI clearFaridUI;

    [Header("UI (In-Game REN)")]
    public Text renNowText;

    [Header("UI (Result)")]
    public Text renCountText;
    public Text clearMessageText;
    public Text timeText;

    [Header("Requirements")]
    public int normalRequiredRen = 3;
    public int hardRequiredRen = 5;

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

        if (clearUIRoot != null) clearUIRoot.SetActive(false);
        if (renNowText != null) renNowText.text = "";
    }

    public void OnPieceLocked(Tetromino piece, int linesCleared)
    {
        if (IsStageCleared) return;

        if (linesCleared > 0)
        {
            currentRen++;
            if (currentRen > maxRen) maxRen = currentRen;
            if (renNowText != null) renNowText.text = $"{currentRen} REN";
            return;
        }

        if (renNowText != null) renNowText.text = "";

        bool cleared = false;

        if (isEasyMode) cleared = true;
        else if (isNormalMode) cleared = (maxRen >= normalRequiredRen);
        else if (isHardMode) cleared = (maxRen >= hardRequiredRen);

        currentRen = 0;

        if (cleared) HandleStageClear();
    }

    void HandleStageClear()
    {
        IsStageCleared = true;

        var controlUI = FindObjectOfType<GameControlUI>();
        if (controlUI != null) controlUI.HideAllUI();

        UpdateClearMessage();

        int spriteIndex = GetSpriteIndexByRen(maxRen);

        if (clearFaridUI != null)
        {
            clearFaridUI.SetImageByIndex(spriteIndex);
            clearFaridUI.Play();
        }

        if (clearUIRoot != null) clearUIRoot.SetActive(true);

        if (stopTimeOnClear) Time.timeScale = 0f;
    }

    int GetSpriteIndexByRen(int ren)
    {
        if (isEasyMode)
        {
            if (ren == 0) return 0;
            if (ren <= 3) return 1;
            if (ren <= 5) return 2;
            if (ren <= 10) return 3;
            return 4;
        }
        else
        {
            if (ren == 0) return 0;
            if (ren == 1) return 1;
            if (ren == 2) return 2;
            if (ren <= 4) return 3;
            return 4;
        }
    }

    string GetRenComment(int ren)
    {
        if (ren == 0) return "..... Well, unfortunately the reality is cruel";
        if (ren <= 3) return "sigh... You better try hard...";
        if (ren <= 5) return "Not bad, but you could do more better";
        if (ren <= 10) return "Wow, you pretty good at REN";
        return "You are God Tetris Player";
    }

    void UpdateClearMessage()
    {
        if (renCountText != null) renCountText.text = $"You did {maxRen} REN";

        if (clearMessageText != null)
        {
            if (isNormalMode || isHardMode)
                clearMessageText.text = "You are now pro at REN!";
            else
                clearMessageText.text = GetRenComment(maxRen);
        }

        if (timeText != null) timeText.text = "";
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnNextStageButton()
    {
        if (string.IsNullOrEmpty(nextStageSceneName)) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextStageSceneName);
    }

    public void OnStageSelectButton()
    {
        if (string.IsNullOrEmpty(stageSelectSceneName)) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
