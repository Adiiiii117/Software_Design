using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tetromino : MonoBehaviour
{
    [Header("Fall Speeds")]
    public float normalFallSpeed = 1f; // 通常落下速度
    public float fastDropSpeed = 12f; // 下加速落下速度

    [Header("Grounded Action Limits")]
    public int groundedMoveAllowance = 14; // 地面接触時の移動許容回数
    public int groundedRotateAllowance = 15; // 地面接触時の回転許容回数 

    [Header("Inactivity Lock")]
    public float inactivitySeconds = 0.9f; // 無操作後にロックするまでの秒数

    [Header("References")]
    public Board board;
    public Transform pivotOverride; 
    public GhostPiece ghost;

    public int typeIndex; // ミノの種類インデックス
    public bool spawnedFromHold; // ホールドから生成されたか否か

    public Transform[] Cells { get; private set; } // ミノのブロックセル配列

    private Transform _pivot; // ミノの回転の基準点
    private bool locked; // ミノがロックされたか否か
    private bool fastDropping; // 下加速中か否か
    private float accumulatedFall; // 自動落下の累積値

    private bool grounded; // ミノが地面に接触しているか否か
    private bool groundedAllowanceInitialized; // 地面接触時の許容回数が初期化済みか否か
    private int movesWhenGrounded; // 地面接触時の残り移動許容回数
    private int rotatesLeftWhenGrounded; // 地面接触時の残り回転許容回数 

    private bool inactivityArmed = false;
    private float lastActionTime = -1f;

    // ミノ生成時の初期設定
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

    // 生成後の初期位置確認と有効性チェック
    private void Start()
    {
        if (board == null) board = FindObjectOfType<Board>();

        Vector3 p = transform.position;
        transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0f);

        if (board == null || !board.IsValidPosition(this, Vector3.zero))
        {
            Debug.Log("Game Over (spawn invalid)");
            enabled = false;
        }
    }

    // 毎フレームの処理（入力・落下・ロック判定など）
    private void Update()
    {
        if (locked) return;

        UpdateGroundedState();
        HandleInput();
        HandleFalling();
        TryAutoLockIfNeeded();
    }

    // ミノが地面または他のミノに接触しているかを判定
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

                if (!inactivityArmed)
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

    // プレイヤーの入力を処理
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            var spawner = FindObjectOfType<Spawner>();
            if (spawner != null && spawner.RequestHold(this)) return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (!grounded || movesWhenGrounded > 0)
            {
                if (TryMove(Vector3.left))
                {
                    if (grounded)
                    {
                        movesWhenGrounded--;
                        ArmInactivityTimerNow();
                        TryAutoLockIfNeeded();
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (!grounded || movesWhenGrounded > 0)
            {
                if (TryMove(Vector3.right))
                {
                    if (grounded)
                    {
                        movesWhenGrounded--;
                        ArmInactivityTimerNow();
                        TryAutoLockIfNeeded();
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow)) TryRotateAndRecord(+1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryRotateAndRecord(-1);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (TryMove(Vector3.down)) { }
            Lock();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow)) fastDropping = true;
        if (Input.GetKeyUp(KeyCode.DownArrow)) fastDropping = false;
    }

    // 回転処理と許容回数管理
    private void TryRotateAndRecord(int dir)
    {
        if (!grounded || rotatesLeftWhenGrounded > 0)
        {
            if (Rotate(dir))
            {
                if (grounded)
                {
                    rotatesLeftWhenGrounded--;
                    ArmInactivityTimerNow();
                    TryAutoLockIfNeeded();
                }
            }
        }
    }

    // 無操作タイマーを即時起動
    private void ArmInactivityTimerNow()
    {
        inactivityArmed = true;
        lastActionTime = Time.time;
    }

    // 自動落下処理
    private void HandleFalling()
    {
        float speed = fastDropping ? fastDropSpeed : normalFallSpeed;
        accumulatedFall += speed * Time.deltaTime;

        while (accumulatedFall >= 1f)
        {
            accumulatedFall -= 1f;

            if (TryMove(Vector3.down)) continue;

            grounded = true;

            if (IsAllowanceDepleted())
            {
                Lock();
                break;
            }

            CheckInactivityAutoLock();
            break;
        }

        CheckInactivityAutoLock();
    }

    // 自動ロックの実行条件を確認
    private void TryAutoLockIfNeeded()
    {
        if (IsAllowanceDepleted() && !board.IsValidPosition(this, Vector3.down))
        {
            Lock();
            return;
        }
        CheckInactivityAutoLock();
    }

    // 一定時間無操作でロックする処理
    private void CheckInactivityAutoLock()
    {
        if (!grounded) return;
        if (!inactivityArmed) return;
        if (lastActionTime < 0f) return;
        if (board.IsValidPosition(this, Vector3.down)) return;

        if (Time.time - lastActionTime >= inactivitySeconds)
        {
            Lock();
        }
    }

    // 地面接触中の操作許容回数が尽きたかを確認
    private bool IsAllowanceDepleted()
    {
        return (movesWhenGrounded <= 0) || (rotatesLeftWhenGrounded <= 0);
    }

    // ミノを指定方向に移動する
    private bool TryMove(Vector3 delta)
    {
        if (!board.IsValidPosition(this, delta)) return false;
        transform.position += delta;
        return true;
    }

    // ミノを回転させる
    private bool Rotate(int dir)
    {
        float angle = -90f * dir;
        transform.RotateAround(_pivot.position, Vector3.forward, angle);

        Vector3[] kicks =
        {
            Vector3.zero,
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.right*2, Vector3.left*2
        };

        foreach (var k in kicks)
        {
            if (board.IsValidPosition(this, k))
            {
                transform.position += k;
                return true;
            }
        }

        transform.RotateAround(_pivot.position, Vector3.forward, -angle);
        return false;
    }

    // 特定のブロックがこのミノに含まれているかを確認
    public bool ContainsCell(Transform t)
    {
        for (int i = 0; i < Cells.Length; i++)
            if (Cells[i] == t) return true;
        return false;
    }

    // ミノを盤面に固定する
    private void Lock()
    {
        if (locked) return;
        locked = true;

        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }

        foreach (var cell in Cells)
        {
            if (cell != null) cell.SetParent(null, true);
        }

        board.SetPiece(this);
        board.ClearLines();

        enabled = false;
        StartCoroutine(SpawnNextFrame());
    }

    // 次のミノを1フレーム後に生成する
    private IEnumerator SpawnNextFrame()
    {
        yield return null;
        var spawner = FindObjectOfType<Spawner>();
        if (spawner != null) spawner.Spawn();
        Destroy(gameObject);
    }
}
