using UnityEngine;

public class HoldPieceUI : MonoBehaviour
{
    [Header("References")]
    public Spawner spawner;                 // Spawnerを参照
    public Transform displayRoot;           // 表示位置の親Transform
    public Tetromino[] previewPrefabs;      // 表示に使うテトロミノのプレハブ群

    [Header("Layout")]
    public float itemScale = 0.5f;          // ミノの表示スケール
    public Vector3 itemOffset = Vector3.zero; // 表示位置の微調整

    private GameObject currentPreview;

    // 有効化時にSpawnerのイベント購読と初期描画
    private void OnEnable()
    {
        if (spawner != null)
        {
            spawner.QueueChanged += Refresh;
            spawner.OnHoldPieceReleased += Clear;
        }
        Refresh();
    }

    // 無効化時にイベント購読解除とUIのクリア
    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.QueueChanged -= Refresh;
            spawner.OnHoldPieceReleased -= Clear;
        }
        Clear();
    }

    // 現在のホールドUIを削除
    private void Clear()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    // ホールドしているミノの表示を更新
    private void Refresh()
    {
        if (spawner == null || previewPrefabs == null || previewPrefabs.Length == 0)
            return;

        Clear();

        int? held = spawner.GetHeldIndex();
        if (held == null) return;

        int idx = held.Value;
        if (idx < 0 || idx >= previewPrefabs.Length) return;

        Tetromino prefab = previewPrefabs[idx];
        currentPreview = Instantiate(prefab.gameObject, displayRoot != null ? displayRoot : transform);
        currentPreview.transform.localPosition = itemOffset;
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localScale = Vector3.one * itemScale;

        foreach (var mb in currentPreview.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = false;
        foreach (var col in currentPreview.GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        foreach (var rb in currentPreview.GetComponentsInChildren<Rigidbody2D>())
            Destroy(rb);

        var ghost = currentPreview.GetComponentInChildren<GhostPiece>();
        if (ghost != null) Destroy(ghost.gameObject);
    }
}
