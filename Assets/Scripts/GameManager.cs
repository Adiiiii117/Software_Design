using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Scene references")]
    public Board board;
    public Spawner spawner;

    // フレームレートなど、ゲーム全体の初期設定を行う
    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    // シーン開始時に参照の最終チェックと依存関係の補完を行う
    private void Start()
    {
        if (!spawner.board) spawner.board = board;
    }
}
