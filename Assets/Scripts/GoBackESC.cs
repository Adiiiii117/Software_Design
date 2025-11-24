using UnityEngine;
using UnityEngine.SceneManagement;

public class GoBackESC : MonoBehaviour
{
    [Header("ESC で戻るシーン名")]
    [SerializeField] private string targetSceneName = "";

    private void Update()
    {
        // ESC判定（旧 InputSystem で動作）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log("[EscToScene] ESC pressed → Load: " + targetSceneName);
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning("[EscToScene] targetSceneName が設定されていません！");
            }
        }
    }
}
