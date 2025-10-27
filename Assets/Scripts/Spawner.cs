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

    // 7バッグ & プレビュー
    private readonly Queue<int> bagQueue = new Queue<int>();
    private System.Random rng;
    private readonly List<int> previewCache = new List<int>();

    // UI更新イベント（Next/Hold 共用）
    public event Action QueueChanged;

    // ホールド関連
    private int? heldIndex = null;   // 現在ホールド中のミノ（なければ null）
    private bool canHold = true;     // 1ミノにつき1回だけホールド可能

    // ==== 初期化 ====
    private void Awake()
    {
        RefillBag();
        RefillBag();
    }

    private void Start()
    {
        if (!ValidateSetup()) return;
        if (spawnOnStart) Spawn();
    }

    // ==== 通常スポーン（袋から） ====
    public Tetromino Spawn()
    {
        if (!ValidateSetup()) return null;

        // 新しいミノが出るたびに、再びホールド可能にする（1ミノ1回ルール）
        canHold = true;

        return SpawnFromBag();
    }

    // ==== ホールド要求（本家仕様：即時スワップ / 1ミノ1回） ====
    public bool RequestHold(Tetromino current)
    {
        if (!ValidateSetup()) return false;
        if (!canHold) return false;                 // このミノでは既にホールド済み
        if (current.spawnedFromHold) return false;  // ホールドから出たミノは再ホールド不可

        int curType = current.typeIndex;
        canHold = false; // このミノでのホールドは使い切り

        // 現在ミノとゴーストを破棄
        if (current.ghost != null) Destroy(current.ghost.gameObject);
        Destroy(current.gameObject);

        if (heldIndex == null)
        {
            // 初回ホールド：保存して袋から即スポーン
            heldIndex = curType;
            QueueChanged?.Invoke();     // Hold欄が変わるので通知
            SpawnFromBag();
        }
        else
        {
            // 2回目以降：ホールドと現在ミノを即時スワップ
            int swap = heldIndex.Value;
            heldIndex = curType;
            QueueChanged?.Invoke();     // Hold欄が入れ替わるので通知
            SpawnByIndex(swap, fromHold: true);
        }

        return true;
    }

    // ==== プレビュー（Next）取得 ====
    public int[] GetUpcoming(int count)
    {
        if (tetrominoPrefabs == null || tetrominoPrefabs.Length == 0) return Array.Empty<int>();
        while (previewCache.Count < count) RefillBag();
        int take = Mathf.Min(count, previewCache.Count);
        int[] outIdx = new int[take];
        for (int i = 0; i < take; i++) outIdx[i] = previewCache[i];
        return outIdx;
    }

    // ==== 現在のホールド取得 / ホールド可否 ====
    public int? GetHeldIndex() => heldIndex;
    public bool CanHoldNow() => canHold;

    // ==== 内部：袋からスポーン ====
    private Tetromino SpawnFromBag()
    {
        // バッグ残量が少なくなってきたら補充（先読み分も含める）
        int need = Mathf.Max(1, tetrominoPrefabs != null ? tetrominoPrefabs.Length : 0);
        if (bagQueue.Count <= need) RefillBag();

        if (bagQueue.Count == 0)
        {
            Debug.LogWarning("Spawner: bagQueue が空です。");
            return null;
        }

        int idx = bagQueue.Dequeue();

        // Nextプレビューを1つ進める
        if (previewCache.Count > 0) previewCache.RemoveAt(0);
        QueueChanged?.Invoke(); // Next/Hold UIに変更を通知

        return SpawnByIndex(idx, fromHold: false);
    }

    // ==== 内部：index 指定スポーン ====
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

        // ゴースト生成（あれば）
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

    // ==== 内部：7バッグ補充 ====
    private void RefillBag()
    {
        if (rng == null) rng = new System.Random();

        int n = (tetrominoPrefabs != null) ? tetrominoPrefabs.Length : 0;
        if (n == 0) return;

        int[] indices = new int[n];
        for (int i = 0; i < n; i++) indices[i] = i;

        // Fisher–Yates
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

    // ==== 内部：セットアップ検証 ====
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
