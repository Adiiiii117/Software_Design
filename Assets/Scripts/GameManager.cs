using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Scene references (ゲームプレイ用)")]
    public Board board;
    public Spawner spawner;

    [Header("Scene Transition")]
    [Tooltip("引数なし版で使うデフォルトの遷移先シーン名")]
    public string defaultSceneName;

    // フレームレートなど、ゲーム全体の初期設定を行う
    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    // シーン開始時に参照の最終チェックと依存関係の補完を行う
    private void Start()
    {
        // ゲームプレイ用シーンでのみ有効にしたい処理
        if (spawner != null && board != null && spawner.board == null)
        {
            spawner.board = board;
        }
    }

   
    // Inspector の OnClick で「引数なし」のときに使う。
    // defaultSceneName に設定したシーンへ移動する。
    public void LoadDefaultScene()
    {
        if (string.IsNullOrEmpty(defaultSceneName))
        {
            Debug.LogWarning("[GameManager] defaultSceneName が設定されていません。Inspector で入力してください。");
            return;
        }

        SceneManager.LoadScene(defaultSceneName);
    }


    // Button の OnClick から、文字列引数でシーン名を指定して呼ぶ用。
    // ボタンごとに違うシーンに飛ばしたいときはこちらを使う。
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameManager] 引数の sceneName が空です。OnClick の引数にシーン名を入れてください。");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
