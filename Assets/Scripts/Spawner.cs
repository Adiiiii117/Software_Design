using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs (I, J, L, O, S, T, Z の順で設定)")]
    public Tetromino[] tetrominoPrefabs;
    public GhostPiece[] ghostPrefabs;

    [Header("References")]
    public Board board;

    [Header("Spawn Settings")]
    public Vector2Int spawnCell = new Vector2Int(5, 20);
    public bool spawnOnStart = true;

    public enum SpawnMode
    {
        Normal,
        OnlyT,
        Sequence
    }

    [Header("Mode")]
    public SpawnMode spawnMode = SpawnMode.Normal;

    [Tooltip("tetrominoPrefabs の中で T が何番目か (I,J,L,O,S,T,Z なら 5)")]
    public int tIndex = 5;

    // 7種ランダムバッグ
    private readonly Queue<int> bagQueue = new Queue<int>();

    // 固定シーケンス（TSD_B などで使用）
    private readonly Queue<int> sequenceQueue = new Queue<int>();

    private System.Random rng;

    public event Action QueueChanged;
    public event Action OnHoldPieceReleased;

    private int? heldIndex = null;
    private bool canHold = true;
    private bool nextHoldSpawn = false; // 互換性のために残しているが、現在は使用していない

    // 初期化
    private void Awake()
    {
        rng = new System.Random();
        RefillBag();
        RefillBag();
    }

    private void Start()
    {
        if (!ValidateSetup()) return;

        ConfigureModeFromScene();

        if (spawnOnStart)
        {
            Spawn();
        }
    }

    /// <summary>
    /// シーン名からモードや固定シーケンスを設定する
    /// </summary>
    private void ConfigureModeFromScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // デフォルトは通常モード
        spawnMode = SpawnMode.Normal;

        // ★ 初級：TSD_E / TST_E → Tミノのみ
        if (sceneName.Contains("TSD_E") || sceneName.Contains("TST_E"))
        {
            spawnMode = SpawnMode.OnlyT;
        }

        // ★ Basic モード：TSD_B_??? → 固定シーケンス
        if (sceneName.Contains("TSD_B"))
        {
            spawnMode = SpawnMode.Sequence;

            // 例: TSD_B_OT → O → T
            if (sceneName.Contains("_IT"))
            {
                EnqueueSequence('I', 'T');
            }
            // 例: TSD_B_JT → J → T
            else if (sceneName.Contains("_JT"))
            {
                EnqueueSequence('J', 'T');
            }
            // 例: TSD_B_LT → L → T
            else if (sceneName.Contains("_LT"))
            {
                EnqueueSequence('L', 'T');
            }

            else if (sceneName.Contains("_OT"))
            {
                EnqueueSequence('O', 'T');
            }
            else if (sceneName.Contains("_ST"))
            {
                EnqueueSequence('S', 'T');
            }
            else if (sceneName.Contains("_TT"))
            {
                EnqueueSequence('T', 'T');
            }
            else if (sceneName.Contains("_ZT"))
            {
                EnqueueSequence('Z', 'T');
            }
            else
            {
                // デフォルト: T だけ1つ出す（必要に応じて拡張）
                EnqueueSequence('T');
            }
        }

        // 将来 TST_B_* などを追加したい場合も、
        // 同じように sceneName を見て sequenceQueue に積めばOK。
    }

    /// <summary>
    /// 固定シーケンスにミノを追加する（文字は I,J,L,O,S,T,Z を想定）
    /// </summary>
    private void EnqueueSequence(params char[] letters)
    {
        foreach (char c in letters)
        {
            int idx = GetIndexForLetter(c);
            if (idx >= 0 && idx < tetrominoPrefabs.Length)
            {
                sequenceQueue.Enqueue(idx);
            }
        }
    }

    /// <summary>
    /// ミノの種類文字から tetrominoPrefabs の index を返す
    /// </summary>
    private int GetIndexForLetter(char letter)
    {
        switch (letter)
        {
            case 'I': return 0;
            case 'J': return 1;
            case 'L': return 2;
            case 'O': return 3;
            case 'S': return 4;
            case 'T': return 5;
            case 'Z': return 6;
            default:
                return Mathf.Clamp(tIndex, 0, tetrominoPrefabs.Length - 1);
        }
    }

    /// <summary>
    /// 次のミノを生成する
    /// </summary>
    public Tetromino Spawn()
    {
        if (!ValidateSetup()) return null;

        canHold = true;

        int idx = GetNextSpawnIndex();
        if (idx < 0)
        {
            Debug.LogWarning("Spawner: 生成できるミノがありません。");
            return null;
        }

        QueueChanged?.Invoke();
        return SpawnByIndex(idx, fromHold: false);
    }

    /// <summary>
    /// 今後出てくるミノの index を count 個返す（Next表示用）
    /// </summary>
    public int[] GetUpcoming(int count)
    {
        if (!ValidateSetup() || count <= 0)
            return Array.Empty<int>();

        List<int> result = new List<int>(count);

        // 固定シーケンスを優先して表示（ただし消費はしない）
        if (sequenceQueue.Count > 0)
        {
            int[] seq = sequenceQueue.ToArray();
            for (int i = 0; i < seq.Length && result.Count < count; i++)
            {
                result.Add(seq[i]);
            }
        }

        // OnlyT モード
        if (spawnMode == SpawnMode.OnlyT && result.Count < count)
        {
            int idxT = Mathf.Clamp(tIndex, 0, tetrominoPrefabs.Length - 1);
            while (result.Count < count)
            {
                result.Add(idxT);
            }
            return result.ToArray();
        }

        // 残りはバッグの内容を参照（消費はしない）
        if (result.Count < count)
        {
            int[] bagArr = bagQueue.ToArray();
            for (int i = 0; i < bagArr.Length && result.Count < count; i++)
            {
                result.Add(bagArr[i]);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// ホールドを実行する
    /// </summary>
    public bool RequestHold(Tetromino current)
    {
        if (!ValidateSetup()) return false;
        if (current == null) return false;
        if (!canHold) return false;

        int curType = current.typeIndex;
        canHold = false;

        if (current.ghost != null)
            Destroy(current.ghost.gameObject);
        Destroy(current.gameObject);

        int spawnIdx = -1;
        bool fromHold = false;

        if (heldIndex == null)
        {
            // 初回ホールド：現在のミノを保存し、新しいミノを通常ルールで出す
            heldIndex = curType;
            spawnIdx = GetNextSpawnIndex();
        }
        else
        {
            // 2回目以降：ホールドしていたミノと入れ替え
            int tmp = heldIndex.Value;
            heldIndex = curType;
            spawnIdx = tmp;
            fromHold = true;
        }

        if (spawnIdx < 0)
        {
            Debug.LogWarning("Spawner: ホールド後に出すミノがありません。");
            return false;
        }

        Tetromino t = SpawnByIndex(spawnIdx, fromHold);
        if (fromHold)
        {
            OnHoldPieceReleased?.Invoke();
        }

        QueueChanged?.Invoke();
        return true;
    }

    /// <summary>現在ホールド中のミノ index を返す（なければ null）</summary>
    public int? GetHeldIndex() => heldIndex;

    /// <summary>現在ホールド可能かどうかを返す</summary>
    public bool CanHoldNow() => canHold && !nextHoldSpawn;

    /// <summary>
    /// ルールに従って「次に出すべきミノ index」を決めて返す
    /// </summary>
    private int GetNextSpawnIndex()
    {
        // 1) 固定シーケンスが残っていればそれを優先
        if (spawnMode == SpawnMode.Sequence && sequenceQueue.Count > 0)
        {
            return sequenceQueue.Dequeue();
        }

        // 2) OnlyT モード
        if (spawnMode == SpawnMode.OnlyT)
        {
            return Mathf.Clamp(tIndex, 0, tetrominoPrefabs.Length - 1);
        }

        // 3) 通常バッグから
        if (bagQueue.Count <= Mathf.Max(1, tetrominoPrefabs != null ? tetrominoPrefabs.Length : 0))
        {
            RefillBag();
        }

        if (bagQueue.Count == 0)
        {
            return -1;
        }

        return bagQueue.Dequeue();
    }

    /// <summary>
    /// 指定された index のミノを生成する
    /// </summary>
    private Tetromino SpawnByIndex(int idx, bool fromHold)
    {
        if (tetrominoPrefabs == null || idx < 0 || idx >= tetrominoPrefabs.Length)
        {
            Debug.LogError($"Spawner: 無効な index {idx}");
            return null;
        }

        Tetromino prefab = tetrominoPrefabs[idx];

        Vector3 spawnPos;
        try
        {
            spawnPos = board.GridToWorld(spawnCell);
        }
        catch
        {
            spawnPos = new Vector3(board.origin.x + spawnCell.x, board.origin.y + spawnCell.y, 0f);
        }

        Tetromino piece = Instantiate(prefab, spawnPos, Quaternion.identity);
        piece.board = board;
        piece.typeIndex = idx;
        piece.spawnedFromHold = fromHold;

        // ゴーストの生成
        if (ghostPrefabs != null &&
            ghostPrefabs.Length > idx &&
            ghostPrefabs[idx] != null)
        {
            GhostPiece ghost = Instantiate(ghostPrefabs[idx], piece.transform.position, Quaternion.identity);
            ghost.target = piece;
            ghost.board = board;
            piece.ghost = ghost;
        }

        return piece;
    }

    /// <summary>
    /// 7種ミノのランダムバッグを補充する
    /// </summary>
    private void RefillBag()
    {
        if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0)
            return;

        List<int> list = new List<int>(tetrominoPrefabs.Length);
        for (int i = 0; i < tetrominoPrefabs.Length; i++)
            list.Add(i);

        // Fisher–Yates シャッフル
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }

        foreach (int idx in list)
        {
            bagQueue.Enqueue(idx);
        }
    }

    /// <summary>
    /// プレハブなどの設定チェック
    /// </summary>
    private bool ValidateSetup()
    {
        if (board == null)
        {
            Debug.LogError("Spawner: board が未設定です。");
            return false;
        }
        if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0)
        {
            Debug.LogError("Spawner: tetrominoPrefabs が未設定です。");
            return false;
        }
        if (ghostPrefabs != null && ghostPrefabs.Length > 0 &&
            tetrominoPrefabs.Length != ghostPrefabs.Length)
        {
            Debug.LogError("Spawner: プレハブ数が一致していません。");
            return false;
        }
        return true;
    }
}
