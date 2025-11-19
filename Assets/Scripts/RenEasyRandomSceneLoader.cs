using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RenEasyRandomSceneLoader : MonoBehaviour
{
    [Header("Keyword to find scenes")]
    [Tooltip("この単語を含むシーンを自動で抽出する（例: REN_E）")]
    public string sceneKeyword = "REN_E";


    // sceneKeyword (REN_E)を含むシーンの中からランダムにロード
    public void LoadRandomEasyRenStage()
    {
        List<string> matchedScenes = new List<string>();

        int sceneCount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (sceneName.Contains(sceneKeyword))
                matchedScenes.Add(sceneName);
        }

        if (matchedScenes.Count == 0)
        {
            Debug.LogError($"RenEasyRandomSceneLoader: '{sceneKeyword}' を含むシーンが Build Settings から見つかりません。");
            return;
        }

        int index = Random.Range(0, matchedScenes.Count);
        string selectedScene = matchedScenes[index];

        Time.timeScale = 1f;  // 念のため戻す
        SceneManager.LoadScene(selectedScene);
    }
}
