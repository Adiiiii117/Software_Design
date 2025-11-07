using UnityEngine;
using System.Collections.Generic;

public class NextQueueUI : MonoBehaviour
{
    [Header("References")]
    public Spawner spawner;
    public Transform listRoot;
    public Tetromino[] previewPrefabs;

    [Header("Layout")]
    // itemOffset は「1つ下に並べるオフセット」。上を次にしたい場合もそのままでOK（例: (0, -1.1f, 0)）
    public Vector3 itemOffset = new Vector3(0, -1.1f, 0);
    public float itemScale = 0.5f;

    private readonly List<GameObject> previews = new List<GameObject>();

    private void OnEnable()
    {
        if (spawner != null)
            spawner.QueueChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (spawner != null)
            spawner.QueueChanged -= Refresh;
        Clear();
    }

    private void Clear()
    {
        foreach (var go in previews)
            Destroy(go);
        previews.Clear();
    }

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
            var parent = (listRoot != null) ? listRoot : transform;
            var go = Instantiate(prefab.gameObject, parent);

            // ← ここを変更：i * itemOffset にすることで「一番上(i=0)が次のミノ」
            go.transform.localPosition = i * itemOffset;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * itemScale;

            // 動きを止める
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            foreach (var col in go.GetComponentsInChildren<Collider2D>()) col.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>()) Destroy(rb);

            var ghost = go.GetComponentInChildren<GhostPiece>();
            if (ghost != null) Destroy(ghost.gameObject);

            previews.Add(go);
        }
    }
}
