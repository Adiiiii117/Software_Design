using System;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly Queue<int> bagQueue = new Queue<int>();
    private System.Random rng;
    private readonly List<int> previewCache = new List<int>();

    public event Action QueueChanged;
    public event Action OnHoldPieceReleased;

    private int? heldIndex = null;
    private bool canHold = true;
    private bool nextHoldSpawn = false;

    // シーン開始時の初期化処理
    private void Awake()
    {
        RefillBag();
        RefillBag();
    }

    // シーン開始時のセットアップ確認と最初のミノ生成
    private void Start()
    {
        if (!ValidateSetup()) return;
        if (spawnOnStart) Spawn();
    }

    // 次のミノを生成する
    public Tetromino Spawn()
    {
        if (!ValidateSetup()) return null;
        canHold = true;

        if (nextHoldSpawn && heldIndex.HasValue)
        {
            int idx = heldIndex.Value;
            nextHoldSpawn = false;
            heldIndex = null;

            var t = SpawnByIndex(idx, fromHold: true);
            OnHoldPieceReleased?.Invoke();
            return t;
        }

        return SpawnFromBag();
    }

    // ホールドを実行する
    public bool RequestHold(Tetromino current)
    {
        if (!ValidateSetup()) return false;
        if (nextHoldSpawn) return false;
        if (current.spawnedFromHold) return false;
        if (!canHold) return false;

        int curType = current.typeIndex;
        canHold = false;

        if (current.ghost != null) Destroy(current.ghost.gameObject);
        Destroy(current.gameObject);

        if (heldIndex == null)
        {
            heldIndex = curType;
            nextHoldSpawn = true;
            SpawnFromBag();
        }
        else
        {
            int swapped = heldIndex.Value;
            heldIndex = curType;
            nextHoldSpawn = true;
            SpawnFromBag();
        }

        return true;
    }

    // 指定数分の次のミノの種類を取得する
    public int[] GetUpcoming(int count)
    {
        if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0) return Array.Empty<int>();
        while (previewCache.Count < count) RefillBag();
        int take = Mathf.Min(count, previewCache.Count);
        int[] outIdx = new int[take];
        for (int i = 0; i < take; i++) outIdx[i] = previewCache[i];
        return outIdx;
    }

    // 現在ホールドされているミノのindexを返す
    public int? GetHeldIndex() => heldIndex;

    // 現在ホールド可能かどうかを返す
    public bool CanHoldNow() => canHold && !nextHoldSpawn;

    // バッグから次のミノを生成する
    private Tetromino SpawnFromBag()
    {
        if (bagQueue.Count <= Mathf.Max(1, tetrominoPrefabs != null ? tetrominoPrefabs.Length : 0))
            RefillBag();

        if (bagQueue.Count == 0)
        {
            Debug.LogWarning("Spawner: bagQueue が空です。");
            return null;
        }

        int idx = bagQueue.Dequeue();

        if (previewCache.Count > 0) previewCache.RemoveAt(0);
        QueueChanged?.Invoke();

        return SpawnByIndex(idx, fromHold: false);
    }

    // 指定されたindexのミノを生成する
    private Tetromino SpawnByIndex(int idx, bool fromHold)
    {
        if (tetrominoPrefabs == null || idx < 0 || idx >= tetrominoPrefabs.Length)
        {
            Debug.LogError($"Spawner: 無効な index {idx}");
            return null;
        }

        Tetromino prefab = tetrominoPrefabs[idx];
        Vector3 spawnPos;

        try { spawnPos = board.GridToWorld(spawnCell); }
        catch { spawnPos = new Vector3(board.origin.x + spawnCell.x, board.origin.y + spawnCell.y, 0f); }

        Tetromino piece = Instantiate(prefab, spawnPos, Quaternion.identity);
        piece.board = board;
        piece.typeIndex = idx;
        piece.spawnedFromHold = fromHold;

        if (ghostPrefabs != null &&
            idx >= 0 && idx < ghostPrefabs.Length &&
            ghostPrefabs[idx] != null)
        {
            GhostPiece ghost = Instantiate(ghostPrefabs[idx], spawnPos, Quaternion.identity);
            ghost.target = piece;
            ghost.board = board;
            piece.ghost = ghost;
        }

        return piece;
    }

    // バッグをリセットして全てのミノをランダムに詰め直す
    private void RefillBag()
    {
        if (rng == null) rng = new System.Random();

        int n = (tetrominoPrefabs != null) ? tetrominoPrefabs.Length : 0;
        if (n == 0) return;

        int[] indices = new int[n];
        for (int i = 0; i < n; i++) indices[i] = i;

        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        foreach (var i in indices)
        {
            bagQueue.Enqueue(i);
            previewCache.Add(i);
        }

        QueueChanged?.Invoke();
    }

    // 必要な参照が設定されているかを確認する
    private bool ValidateSetup()
    {
        if (board == null)
        {
            Debug.LogError("Spawner: Board が未割当です。");
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
