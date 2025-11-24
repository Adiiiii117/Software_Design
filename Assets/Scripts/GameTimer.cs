using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [Header("Timer UI")]
    public Text timeLabel; // プレイ中に経過時間を表示するUI Text

    public static GameTimer Instance { get; private set; }

    private float elapsedTime = 0f;
    private bool isRunning = true;

    private void Awake()
    {
        // シングルトン化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ★ DontDestroyOnLoad を削除！
        //  → ステージごとに新しく生成されるようになる
    }

    private void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (timeLabel != null)
            timeLabel.text = $"TIME: {elapsedTime:F2}s";
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        UpdateLabel();
    }

    public float GetClearTime()
    {
        return elapsedTime;
    }
}
