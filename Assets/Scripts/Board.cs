using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    public Vector2Int size = new Vector2Int(10, 22);
    public Vector2Int visibleSize = new Vector2Int(10, 20);
    public Transform blockContainer;
    public Vector3 origin = Vector3.zero;

    [Header("Special Blocks")]
    [Tooltip("このタグが付いているブロックはラインが消えても残し、下にも動かさない（壁などに使用）")]
    public string fixedBlockTag = "FixedBlock";

    private Transform[,] grid;

    // ボード初期化（グリッド生成）
    private void Awake()
    {
        grid = new Transform[size.x, size.y];
    }

    // ワールド座標からグリッド座標に変換する
    public Vector2Int WorldToGrid(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x - origin.x);
        int y = Mathf.RoundToInt(position.y - origin.y);
        return new Vector2Int(x, y);
    }

    // グリッド座標からワールド座標に変換する
    public Vector3 GridToWorld(Vector2Int cell)
    {
        float x = cell.x + origin.x;
        float y = cell.y + origin.y;
        return new Vector3(x, y, 0f);
    }

    // ミノの現在位置が有効（範囲内かつ衝突なし）かどうかを判定する
    public bool IsValidPosition(Tetromino tetromino, Vector3 move)
    {
        foreach (Transform block in tetromino.Cells)
        {
            Vector3 newPos = block.position + move;
            Vector2Int cell = WorldToGrid(newPos);

            if (!IsInside(cell))
                return false;

            if (IsOccupied(cell))
                return false;
        }
        return true;
    }

    // 指定されたセルが盤面の範囲内にあるかを確認する
    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < size.x && cell.y >= 0 && cell.y < size.y;
    }

    // 指定されたセルにブロックがあるかどうかを確認する
    private bool IsOccupied(Vector2Int cell)
    {
        return grid[cell.x, cell.y] != null;
    }

    // ミノを盤面に固定する
    public void SetPiece(Tetromino tetromino)
    {
        foreach (Transform block in tetromino.Cells)
        {
            Vector2Int cell = WorldToGrid(block.position);
            if (cell.y >= size.y) continue;
            grid[cell.x, cell.y] = block;
            block.SetParent(blockContainer, true);
        }
    }

    // 全てのラインをチェックし、揃っている行を削除する
    public void ClearLines()
    {
        for (int y = 0; y < size.y; y++)
        {
            if (IsLineFull(y))
            {
                ClearLine(y);
                ShiftLinesDown(y + 1);
                y--;
            }
        }
    }

    // 指定された行がすべて埋まっているかを判定する
    private bool IsLineFull(int y)
    {
        for (int x = 0; x < size.x; x++)
        {
            if (grid[x, y] == null)
                return false;
        }
        return true;
    }

    // 指定された行を削除する
    // （fixedBlockTag が付いているブロックは消さず、そのまま残す）
    private void ClearLine(int y)
    {
        for (int x = 0; x < size.x; x++)
        {
            if (grid[x, y] != null)
            {
                // 固定ブロックなら消さない
                if (!string.IsNullOrEmpty(fixedBlockTag) &&
                    grid[x, y].CompareTag(fixedBlockTag))
                {
                    continue;
                }

                Destroy(grid[x, y].gameObject);
                grid[x, y] = null;
            }
        }
    }

    // 追加：消えたライン数を返すバージョン
    public int ClearLinesAndGetCount()
    {
        int cleared = 0;
        for (int y = 0; y < size.y; y++)
        {
            if (IsLineFull(y))
            {
                ClearLine(y);
                ShiftLinesDown(y + 1);
                y--;
                cleared++;
            }
        }
        return cleared;
    }

    // 指定行より上のすべての行を1段下に移動する
    // （fixedBlockTag が付いているブロックは動かさない）
    private void ShiftLinesDown(int startY)
    {
        for (int y = startY; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (grid[x, y] != null)
                {
                    // 固定ブロックはその場に残す
                    if (!string.IsNullOrEmpty(fixedBlockTag) &&
                        grid[x, y].CompareTag(fixedBlockTag))
                    {
                        continue;
                    }

                    grid[x, y - 1] = grid[x, y];
                    grid[x, y] = null;
                    grid[x, y - 1].position += Vector3.down;
                }
            }
        }
    }

    // 盤面全体をリセットする
    public void ClearBoard()
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }
    }

    public bool IsCellOccupiedOrOutOfBounds(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= size.x || cell.y < 0 || cell.y >= size.y)
            return true; // 盤外は「埋まっている」とみなす

        return grid[cell.x, cell.y] != null;
    }
}
