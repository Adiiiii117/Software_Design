using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Tetromino : MonoBehaviour
{
    [Header("Fall Speeds")]
    public float normalFallSpeed = 1f;   // 通常落下速度
    public float fastDropSpeed = 12f;    // 下加速落下速度

    [Header("Grounded Action Limits")]
    public int groundedMoveAllowance = 14;      // 地面接触時の移動許容回数
    public int groundedRotateAllowance = 15;    // 接地時の回転許容回数 

    [Header("Inactivity Lock")]
    public float inactivitySeconds = 0.9f;      // 無操作後にロックするまでの秒数

    [Header("References")]
    public Board board;
    public Transform pivotOverride;
    public GhostPiece ghost;

    public int typeIndex;              // ミノの種類インデックス (I,J,L,O,S,T,Z など)
    public bool spawnedFromHold;       // ホールドから生成されたか

    public Transform[] Cells { get; private set; } // ミノを構成する4つのブロック

    private Transform _pivot;           // 回転の基準点
    private bool locked;               // ロック済みかどうか
    private bool fastDropping;         // 下加速中かどうか
    private float accumulatedFall;     // 自動落下の累積値

    private bool grounded;                     // 地面接触中かどうか
    private bool groundedAllowanceInitialized; // 接地許容回数が初期化されたか
    private int movesWhenGrounded;             // 接地中の残り移動回数
    private int rotatesLeftWhenGrounded;       // 接地中の残り回転回数

    private bool inactivityArmed = false;      // 無操作ロックタイマー起動済みか
    private float lastActionTime = -1f;        // 最後の操作時刻

    // シーン別挙動用フラグ
    private bool disableAutoFall = false;      // 自動落下を無効にするか
    private bool allowUpMove = false;          // 上移動を許可するか

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
            return;
        }

        // シーン名に応じて自動落下や上移動の可否を切り替える
        // ・TSD_N …… 中級TSD（自動落下なし＋↑移動あり）
        // ・REN_E_* … Easy REN（自動落下なし＋↑移動あり）
        // ・REN_N …… Normal REN（自動落下なし＋↑移動あり）
        // ・REN_H …… Hard REN（通常落下・↑移動なし）
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("TSD_N") || sceneName.Contains("REN_E") || sceneName.Contains("REN_N"))
        {
            disableAutoFall = true;
            allowUpMove = true;
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
        // ホールド (C or LeftShift)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            var spawner = FindObjectOfType<Spawner>();
            if (spawner != null && spawner.RequestHold(this)) return;
        }

        // 左移動 (←)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
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

        // 右移動 (→)
        if (Input.GetKeyDown(KeyCode.RightArrow))
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

        // 上移動 (↑) - 対象シーンのみ有効（TSD_N / REN_E_* / REN_N）
        if (allowUpMove && Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (TryMove(Vector3.up))
            {
                ArmInactivityTimerNow();
            }
        }

        // 回転 (D: 右回転, A: 左回転) ※ここは元のキー設定のまま
        if (Input.GetKeyDown(KeyCode.D)) TryRotateAndRecord(+1);
        if (Input.GetKeyDown(KeyCode.A)) TryRotateAndRecord(-1);

        // ハードドロップ (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (TryMove(Vector3.down)) { }
            Lock();
        }

        // ソフトドロップ (↓)
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
        // REN_N / REN_E_* / TSD_N では自動落下を0にする（↓キー・Spaceは別扱い）
        float baseSpeed = disableAutoFall ? 0f : normalFallSpeed;
        float speed = fastDropping ? fastDropSpeed : baseSpeed;

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
        // ★ 上方向に動かすときだけ「見えている高さ」を超えないようにチェック
        if (delta == Vector3.up)
        {
            foreach (var cell in Cells)
            {
                // このブロックが動いた先のグリッド座標
                Vector2Int gridPos = board.WorldToGrid(cell.position + delta);

                // visibleSize.y 以上なら、枠より上にはみ出すので移動禁止
                if (gridPos.y >= board.visibleSize.y)
                {
                    return false;
                }
            }
        }

        // いつもの当たり判定（盤外・他ブロックとの衝突）
        if (!board.IsValidPosition(this, delta))
            return false;

        // ここまで来たら安全なので移動
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
            Vector3.right * 2, Vector3.left * 2
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

        // 子ブロックをいったん親から外す（Board.SetPieceで再アタッチされる）
        foreach (var cell in Cells)
        {
            if (cell != null) cell.SetParent(null, true);
        }

        // 盤面に固定
        board.SetPiece(this);

        // 何ライン消えたかを取得（Board に ClearLinesAndGetCount() がある前提）
        int linesCleared = board.ClearLinesAndGetCount();

        // ===== 各種 Judge へ通知 =====

        // T-Spin Double 用
        var tsdJudge = FindObjectOfType<TSpinDoubleJudge>();
        if (tsdJudge != null)
        {
            tsdJudge.OnPieceLocked(this, linesCleared);

            // クリアでステージが終了しているなら、次のミノは出さない
            if (tsdJudge.IsStageCleared)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }
        }

        // T-Spin Triple 用
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

        // REN 練習用
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

        // ===== 通常進行：次のミノを出す =====
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
