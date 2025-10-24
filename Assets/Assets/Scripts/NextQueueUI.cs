using UnityEngine;
using System.Collections.Generic;

public class NextQueueUI : MonoBehaviour
{
    [Header("References")]
    public Spawner spawner;
    public Transform listRoot;
    public Tetromino[] previewPrefabs;

    [Header("Layout")]
    public Vector3 itemOffset = new Vector3(0, -1.1f, 0);
    public float itemScale = 0.5f;

    private readonly List<GameObject> previews = new List<GameObject>();

    // 有効化時にSpawnerのイベント購読と初期描画
    private void OnEnable()
    {
        if (spawner != null)
            spawner.QueueChanged += Refresh;
        Refresh();
    }

    // 無効化時にイベント購読解除と表示クリア
    private void OnDisable()
    {
        if (spawner != null)
            spawner.QueueChanged -= Refresh;
        Clear();
    }

    // 表示中のNextミノをすべて削除
    private void Clear()
    {
        foreach (var go in previews)
            Destroy(go);
        previews.Clear();
    }

    // Nextミノの一覧を再描画
    private void Refresh()
    {
        if (spawner == null || previewPrefabs == null || previewPrefabs.Length == 0)
            return;

        Clear();

        int[] next = spawner.GetUpcoming(5);
        for (int i = 0; i < next.Length; i++)
        {
            int idx = next[i];
            if (idx < 0 || idx >= previewPrefabs.Length) continue;

            Tetromino prefab = previewPrefabs[idx];
            var go = Instantiate(prefab.gameObject, listRoot != null ? listRoot : transform);

            int reversedIndex = next.Length - 1 - i;
            go.transform.localPosition = reversedIndex * itemOffset;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * itemScale;

            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;
            foreach (var col in go.GetComponentsInChildren<Collider2D>())
                col.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>())
                Destroy(rb);

            var ghost = go.GetComponentInChildren<GhostPiece>();
            if (ghost != null) Destroy(ghost.gameObject);

            previews.Add(go);
        }
    }
}
