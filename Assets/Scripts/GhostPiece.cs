using UnityEngine;

public class GhostPiece : MonoBehaviour
{
    [Tooltip("着地点を表示したい実体ミノ（Spawnerが代入）")]
    public Tetromino target;

    [Tooltip("Board（Spawnerが代入）")]
    public Board board;

    [Tooltip("半透明色（Prefab側のSpriteRendererにも反映しておくと良い）")]
    public Color ghostColor = new Color(1f, 1f, 1f, 0.28f);

    [Tooltip("Ghost側のPivot（Prefab内のEmpty）。名前に \"Pivot\" を含めるとAwakeで自動検出")]
    public Transform pivotOverride;

    // 形コピーしたい場合に使う（各ゴーストPrefabが形済みなら必須ではない）
    private Transform[] cells = new Transform[4];

    private void Awake()
    {
        // ルート直下から Pivot と 4セルを拾う（セル名は何でもOK）
        int i = 0;
        foreach (Transform c in transform)
        {
            string low = c.name.ToLower();
            if (low.Contains("pivot")) { pivotOverride = c; continue; }

            if (i < 4)
            {
                cells[i++] = c;
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr) sr.color = ghostColor;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null || board == null) return;

        // 1) 形と回転を同期（※各GhostPrefabが形済みならこのブロックは何もしなくてもOK）
        CopyShapeAndRotation();

        // 2) Δ（差分）で最下位置までシミュレーションして配置
        SnapToLanding();
    }

    private void CopyShapeAndRotation()
    {
        // ターゲットの子4つの localPosition をコピー（形を常に一致させる）
        if (target.Cells != null && target.Cells.Length >= 4)
        {
            for (int i = 0; i < 4 && i < cells.Length; i++)
            {
                if (cells[i] != null && target.Cells[i] != null)
                    cells[i].localPosition = target.Cells[i].localPosition;
            }
        }

        // 回転も一致
        transform.rotation = target.transform.rotation;

        // Pivot位置も同期（両PrefabでPivotがある場合）
        if (target.pivotOverride != null && pivotOverride != null)
            pivotOverride.localPosition = target.pivotOverride.localPosition;
    }

    private void SnapToLanding()
    {
        Vector3 basePos = target.transform.position;

        // Board.IsValidPosition(piece, delta) を想定：現在位置からの差分で問い合わせる
        Vector3 offset = Vector3.zero;
        while (board.IsValidPosition(target, offset + Vector3.down))
        {
            offset += Vector3.down;
        }

        transform.position = basePos + offset;
    }
}
