using System.Collections.Generic;
using UnityEngine;

public class StageTetrominoBaker : MonoBehaviour
{
    [Header("References")]
    public Board board;                         // 盤面

    [Header("Options")]
    public bool roundChildPositionsToGrid = true; // 子ミノの位置を整数グリッドに揃えるか
    public bool bakeOnStart = true;              // Start時に自動で焼き込むか

    private void Reset()
    {
        // 可能なら自動でBoardを探す
        if (!board) board = FindObjectOfType<Board>();
    }

    private void Start()
    {
        if (!board)
        {
            Debug.LogError("StageTetrominoBaker: Board が設定されていません。");
            return;
        }

        if (bakeOnStart)
        {
            BakeChildrenTetrominoesIntoBoard();
        }
    }

    /// <summary>
    /// 子オブジェクトに置いた Tetromino プレハブを
    /// Board 上の静的ブロックとして焼き込む。
    /// </summary>
    public void BakeChildrenTetrominoesIntoBoard()
    {
        // このオブジェクト以下にある Tetromino を全部取ってくる
        var tempList = new List<Tetromino>(GetComponentsInChildren<Tetromino>());

        foreach (var t in tempList)
        {
            if (t == null) continue;

            // 見た目とロジックを合わせるため、位置を整数マスにスナップ
            if (roundChildPositionsToGrid)
            {
                var p = t.transform.position;
                t.transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f);
            }

            // Board に登録してブロックを実体化
            t.board = board;
            board.SetPiece(t);   // ← grid配列と blockContainer に登録:contentReference[oaicite:2]{index=2}

            // ゴーストなど余計なものを消す
            if (t.ghost) Destroy(t.ghost.gameObject);

            // Tetromino本体は不要になるので削除
            Destroy(t.gameObject);
        }

        // 万が一ラインが揃っていたらここで消える
        board.ClearLines();
    }
}
