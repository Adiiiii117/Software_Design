using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Tetromino : MonoBehaviour
{
    [Header("Fall Speeds")]
    public float normalFallSpeed = 1f;   // 通常落下速度（マス/秒）
    public float fastDropSpeed = 12f;    // 下加速落下速度（マス/秒）

    [Header("Grounded Action Limits")]
    public int groundedMoveAllowance = 14;      // 接地時の移動許容回数
    public int groundedRotateAllowance = 15;    // 接地時の回転許容回数

    [Header("Inactivity Lock")]
    public float inactivitySeconds = 0.9f;      // 無操作ロック秒

    [Header("References")]
    public Board board;
    public Transform pivotOverride;              // 回転の中心（任意）
    public GhostPiece ghost;

    [Header("Meta")]
    public int typeIndex;                        // ミノ種類 (Spawnerの並び: I(0),J(1),L(2),O(3),S(4),T(5),Z(6))
    public bool spawnedFromHold;

    public Transform[] Cells { get; private set; } // ブロック4個

    // 内部状態
    private Transform _pivot;
    private bool locked = false;
    private bool fastDropping = false;
    private float accumulatedFall = 0f;

    private bool grounded = false;
    private bool groundedAllowanceInitialized = false;
    private int movesWhenGrounded;
    private int rotatesLeftWhenGrounded;

    private bool inactivityArmed = false;
    private float lastActionTime = -1f;

    // シーン別挙動
    private bool disableAutoFall = false;  // 自動落下を無効にするか
    private bool allowUpMove = false;      // ↑移動を許可するか
    private bool hardDropOnlyLock = false; // Normal: ハードドロップ時のみロック

    // SRS
    // 0:Up, 1:Right, 2:Down, 3:Left
    private int rotationIndex = 0;
    public bool lastMoveWasRotation { get; private set; } = false;

    private void Awake()
    {
        var list = new List<Transform>(4);
        foreach (Transform c in transform)
        {
            string n = c.name.ToLower();
            if (n.Contains("pivot"))
            {
                if (pivotOverride == null) pivotOverride = c;
                continue;
            }
            if (c.GetComponent<SpriteRenderer>() == null) continue;
            if (list.Count < 4) list.Add(c);
        }
        Cells = list.ToArray();

        _pivot = pivotOverride != null ? pivotOverride : transform;

        grounded = false;
        groundedAllowanceInitialized = false;
        movesWhenGrounded = groundedMoveAllowance;
        rotatesLeftWhenGrounded = groundedRotateAllowance;
    }

    private void Start()
    {
        if (board == null) board = FindObjectOfType<Board>();

        // 初期位置をマスにスナップ
        Vector3 p = transform.position;
        transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f);

        if (board == null || !board.IsValidPosition(this, Vector3.zero))
        {
            Debug.Log("Game Over (spawn invalid)");
            enabled = false;
            return;
        }

        // スポーン角度から rotationIndex を算出（Z軸 0/90/180/270 前提）
        rotationIndex = Mathf.RoundToInt((360f - transform.eulerAngles.z) / 90f) & 3;

        // シーン名に応じた挙動
        string sceneName = SceneManager.GetActiveScene().name;
        bool isNormalMode = sceneName.Contains("TSD_N") || sceneName.Contains("TST_N") || sceneName.Contains("REN_N");

        if (isNormalMode)
        {
            // Normal：落下なし・↑移動あり・Spaceのみロック
            disableAutoFall = true;
            allowUpMove = true;
            hardDropOnlyLock = true;
        }
        else if (sceneName.Contains("REN_E"))
        {
            // Easy(REN_E)：落下あり・↑移動なし
            disableAutoFall = false;
            allowUpMove = false;
            hardDropOnlyLock = false;
        }
        // それ以外（Hard等）はデフォルト（落下あり・↑移動なし）
    }

    private void Update()
    {
        if (locked) return;

        UpdateGroundedState();
        HandleInput();
        HandleFalling();
        TryAutoLockIfNeeded();
    }

    private void UpdateGroundedState()
    {
        bool touching = !board.IsValidPosition(this, Vector3.down);

        if (touching)
        {
            if (!grounded)
            {
                grounded = true;

                if (!groundedAllowanceInitialized)
                {
                    groundedAllowanceInitialized = true;
                    movesWhenGrounded = groundedMoveAllowance;
                    rotatesLeftWhenGrounded = groundedRotateAllowance;
                }

                if (!hardDropOnlyLock && !inactivityArmed)
                {
                    inactivityArmed = true;
                    lastActionTime = Time.time;
                }
            }
            else
            {
                grounded = true;
            }
        }
        else
        {
            grounded = false;
        }
    }

    private void HandleInput()
    {
        // Hold (C / LeftShift)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            var spawner = FindObjectOfType<Spawner>();
            if (spawner != null && spawner.RequestHold(this)) return;
        }

        // Move Left
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!grounded || movesWhenGrounded > 0 || hardDropOnlyLock)
            {
                if (TryMove(Vector3.left))
                {
                    if (grounded && !hardDropOnlyLock)
                    {
                        movesWhenGrounded--;
                        ArmInactivityTimerNow();
                        TryAutoLockIfNeeded();
                    }
                }
            }
        }

        // Move Right
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (!grounded || movesWhenGrounded > 0 || hardDropOnlyLock)
            {
                if (TryMove(Vector3.right))
                {
                    if (grounded && !hardDropOnlyLock)
                    {
                        movesWhenGrounded--;
                        ArmInactivityTimerNow();
                        TryAutoLockIfNeeded();
                    }
                }
            }
        }

        // Move Up (only when allowed)
        if (allowUpMove && Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (TryMove(Vector3.up))
            {
                if (!hardDropOnlyLock) ArmInactivityTimerNow();
            }
        }

        // Rotate CW / CCW (D / A)
        if (Input.GetKeyDown(KeyCode.D)) TryRotateAndRecord(+1);
        if (Input.GetKeyDown(KeyCode.A)) TryRotateAndRecord(-1);

        // Hard Drop (Space) → ONLY lock in "hardDropOnlyLock" mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (TryMove(Vector3.down)) { }
            Lock();
        }

        // Soft Drop (↓)
        if (Input.GetKeyDown(KeyCode.DownArrow)) fastDropping = true;
        if (Input.GetKeyUp(KeyCode.DownArrow)) fastDropping = false;
    }

    private void TryRotateAndRecord(int dir)
    {
        if (!grounded || rotatesLeftWhenGrounded > 0 || hardDropOnlyLock)
        {
            if (RotateSRS(dir))
            {
                if (grounded && !hardDropOnlyLock)
                {
                    rotatesLeftWhenGrounded--;
                    ArmInactivityTimerNow();
                    TryAutoLockIfNeeded();
                }
            }
        }
    }

    private void ArmInactivityTimerNow()
    {
        if (hardDropOnlyLock) return;
        inactivityArmed = true;
        lastActionTime = Time.time;
    }

    private void HandleFalling()
    {
        float baseSpeed = disableAutoFall ? 0f : normalFallSpeed;
        float speed = fastDropping ? fastDropSpeed : baseSpeed;

        accumulatedFall += speed * Time.deltaTime;

        while (accumulatedFall >= 1f)
        {
            accumulatedFall -= 1f;

            if (TryMove(Vector3.down)) continue;

            grounded = true;

            if (hardDropOnlyLock)
            {
                // Normalでは自動ロックしない（SpaceでのみLock）
                break;
            }

            if (IsAllowanceDepleted())
            {
                Lock();
                break;
            }

            CheckInactivityAutoLock();
            break;
        }

        if (!hardDropOnlyLock)
        {
            CheckInactivityAutoLock();
        }
    }

    private void TryAutoLockIfNeeded()
    {
        if (hardDropOnlyLock) return;

        if (IsAllowanceDepleted() && !board.IsValidPosition(this, Vector3.down))
        {
            Lock();
            return;
        }
        CheckInactivityAutoLock();
    }

    private void CheckInactivityAutoLock()
    {
        if (hardDropOnlyLock) return;
        if (!grounded) return;
        if (!inactivityArmed) return;
        if (lastActionTime < 0f) return;
        if (board.IsValidPosition(this, Vector3.down)) return;

        if (Time.time - lastActionTime >= inactivitySeconds)
        {
            Lock();
        }
    }

    private bool IsAllowanceDepleted()
    {
        if (hardDropOnlyLock) return false;
        return (movesWhenGrounded <= 0) || (rotatesLeftWhenGrounded <= 0);
    }

    private bool TryMove(Vector3 delta)
    {
        // 上移動は可視範囲を超えないよう制限
        if (delta == Vector3.up)
        {
            foreach (var cell in Cells)
            {
                Vector2Int gridPos = board.WorldToGrid(cell.position + delta);
                if (gridPos.y >= board.visibleSize.y) return false;
            }
        }

        if (!board.IsValidPosition(this, delta)) return false;

        transform.position += delta;
        return true;
    }

    // ===== SRS回転実装 =====

    // dir: +1=CW, -1=CCW
    private bool RotateSRS(int dir)
    {
        lastMoveWasRotation = false;

        int from = rotationIndex;
        int to = (rotationIndex + (dir > 0 ? 1 : 3)) & 3;

        float angle = -90f * dir;
        transform.RotateAround(_pivot.position, Vector3.forward, angle);

        Vector2Int[] kicks = GetSRSKicks(typeIndex, from, to, dir);

        foreach (var k in kicks)
        {
            Vector3 delta = new Vector3(k.x, k.y, 0f);
            if (board.IsValidPosition(this, delta))
            {
                transform.position += delta;
                rotationIndex = to;
                lastMoveWasRotation = true;
                return true;
            }
        }

        // 失敗：元に戻す
        transform.RotateAround(_pivot.position, Vector3.forward, -angle);
        return false;
    }

    private Vector2Int[] GetSRSKicks(int tIndex, int from, int to, int dir)
    {
        if (IsOPiece(tIndex))
        {
            return new[] { V(0, 0) };
        }
        if (IsIPiece(tIndex))
        {
            return GetI_Kicks(from, to, dir);
        }
        return GetJLSTZ_Kicks(from, to, dir);
    }

    private bool IsIPiece(int t) => t == 0; // I = 0（Spawnerの順に合わせる）
    private bool IsOPiece(int t) => t == 3; // O = 3

    // JLSTZ 共通（SRS正規）
    private Vector2Int[] GetJLSTZ_Kicks(int from, int to, int dir)
    {
        if (dir > 0) // CW
        {
            switch (from)
            {
                case 0: return new[] { V(0,0), V(-1,0), V(-1,+1), V(0,-2), V(-1,-2) }; // Up->Right
                case 1: return new[] { V(0,0), V(+1,0), V(+1,-1), V(0,+2), V(+1,+2) }; // Right->Down
                case 2: return new[] { V(0,0), V(+1,0), V(+1,+1), V(0,-2), V(+1,-2) }; // Down->Left
                case 3: return new[] { V(0,0), V(-1,0), V(-1,-1), V(0,+2), V(-1,+2) }; // Left->Up
            }
        }
        else // CCW
        {
            switch (from)
            {
                case 0: return new[] { V(0,0), V(+1,0), V(+1,+1), V(0,-2), V(+1,-2) }; // Up->Left
                case 3: return new[] { V(0,0), V(+1,0), V(+1,-1), V(0,+2), V(+1,+2) }; // Left->Down
                case 2: return new[] { V(0,0), V(-1,0), V(-1,+1), V(0,-2), V(-1,-2) }; // Down->Right
                case 1: return new[] { V(0,0), V(-1,0), V(-1,-1), V(0,+2), V(-1,+2) }; // Right->Up
            }
        }
        return new[] { V(0, 0) };
    }

    // I専用（SRS正規）
    private Vector2Int[] GetI_Kicks(int from, int to, int dir)
    {
        if (dir > 0) // CW
        {
            switch (from)
            {
                case 0: return new[] { V(0,0), V(-2,0), V(+1,0), V(-2,-1), V(+1,+2) }; // Up->Right
                case 1: return new[] { V(0,0), V(-1,0), V(+2,0), V(-1,+2), V(+2,-1) }; // Right->Down
                case 2: return new[] { V(0,0), V(+2,0), V(-1,0), V(+2,+1), V(-1,-2) }; // Down->Left
                case 3: return new[] { V(0,0), V(+1,0), V(-2,0), V(+1,-2), V(-2,+1) }; // Left->Up
            }
        }
        else // CCW
        {
            switch (from)
            {
                case 0: return new[] { V(0,0), V(-1,0), V(+2,0), V(-1,+2), V(+2,-1) }; // Up->Left
                case 3: return new[] { V(0,0), V(-2,0), V(+1,0), V(-2,-1), V(+1,+2) }; // Left->Down
                case 2: return new[] { V(0,0), V(+1,0), V(-2,0), V(+1,-2), V(-2,+1) }; // Down->Right
                case 1: return new[] { V(0,0), V(+2,0), V(-1,0), V(+2,+1), V(-1,-2) }; // Right->Up
            }
        }
        return new[] { V(0,0) };
    }

    private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

    // ===== ここまで SRS =====

    public bool ContainsCell(Transform t)
    {
        for (int i = 0; i < Cells.Length; i++)
            if (Cells[i] == t) return true;
        return false;
    }

    private void Lock()
    {
        if (locked) return;
        locked = true;

        // クリアUI表示時の不要ゴースト削除
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }

        // 子ブロックを独立させる（Board側で再アタッチ）
        foreach (var cell in Cells)
        {
            if (cell != null) cell.SetParent(null, true);
        }

        // 盤面に固定
        board.SetPiece(this);

        // ライン消去＆本数取得
        int linesCleared = board.ClearLinesAndGetCount();

        // ===== 各Judgeへ通知 =====
        var tsdJudge = FindObjectOfType<TSpinDoubleJudge>();
        if (tsdJudge != null)
        {
            tsdJudge.OnPieceLocked(this, linesCleared);
            if (tsdJudge.IsStageCleared)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
        }

        var tstJudge = FindObjectOfType<TSpinTripleJudge>();
        if (tstJudge != null)
        {
            tstJudge.OnPieceLocked(this, linesCleared);
            if (tstJudge.IsStageCleared)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
        }

        var renJudge = FindObjectOfType<RenJudge>();
        if (renJudge != null)
        {
            renJudge.OnPieceLocked(this, linesCleared);
            if (renJudge.IsStageCleared)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
        }

        // 次のミノを出す
        enabled = false;
        StartCoroutine(SpawnNextFrame());
    }

    private IEnumerator SpawnNextFrame()
    {
        yield return null;
        var spawner = FindObjectOfType<Spawner>();
        if (spawner != null) spawner.Spawn();
        Destroy(gameObject);
    }
}
