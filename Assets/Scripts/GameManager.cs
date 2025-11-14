using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Scene references (ゲームプレイ用)")]
    public Board board;
    public Spawner spawner;

    [Header("Scene Transition (共通)")]
    [Tooltip("フェード演出を入れたい場合だけ設定")]
    public FadeCanvas fadeCanvas;   // なければ空でOK

    // フレームレートなど、ゲーム全体の初期設定を行う
    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    // シーン開始時に参照の最終チェックと依存関係の補完を行う
    private void Start()
    {
        // ゲームプレイ用シーンに GameManager を置いた場合だけ有効にする
        if (spawner != null && board != null && spawner.board == null)
        {
            spawner.board = board;
        }
    }

    // ボタンの OnClick から直接呼ぶ用。
    // Inspector の OnClick で文字列引数にシーン名を渡す。
 
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameManager] シーン名が空です。OnClick の引数を設定してください。");
            return;
        }

        LoadSceneInternal(sceneName);
    }


    // Build Settings の「次のシーン」へ進みたいとき用（必要なら使う）
    public void LoadNextSceneByBuildIndex()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        string sceneName = SceneManager.GetSceneByBuildIndex(current + 1).name;
        LoadSceneInternal(sceneName);
    }

    
    //実際のロード処理（フェード有り／無しを共通化）
    private void LoadSceneInternal(string sceneName)
    {
        if (fadeCanvas != null)
        {
            // フェード演出を使う場合
            fadeCanvas.FadeOut(() =>
            {
                SceneManager.LoadScene(sceneName);
            });
        }
        else
        {
            // フェードなしで即ロード
            SceneManager.LoadScene(sceneName);
        }
    }
}