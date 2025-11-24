using UnityEngine;
using UnityEngine.SceneManagement;

public class GoBackESC : MonoBehaviour
{
    [Header("戻り先のシーン名")]
    [SerializeField] private string backSceneName = "TechniqueSelect";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[EscBack] ESC pressed. Loading scene: " + backSceneName);
            SceneManager.LoadScene(backSceneName);
        }
    }
}
