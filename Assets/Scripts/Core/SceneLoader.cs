using UnityEngine;
using UnityEngine.SceneManagement;

namespace IGame.Core
{
    public class SceneLoader : MonoBehaviour
    {
        [Tooltip("Scene name used by LoadConfiguredScene().")]
        public string sceneName;

        [Tooltip("Build index used by LoadConfiguredSceneByIndex().")]
        public int sceneBuildIndex = -1;

        public void LoadConfiguredScene()
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void LoadConfiguredSceneByIndex()
        {
            if (sceneBuildIndex >= 0)
            {
                SceneManager.LoadScene(sceneBuildIndex);
            }
        }

        public void LoadSceneByName(string targetSceneName)
        {
            if (!string.IsNullOrWhiteSpace(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }

        public void LoadSceneByIndex(int targetSceneBuildIndex)
        {
            if (targetSceneBuildIndex >= 0)
            {
                SceneManager.LoadScene(targetSceneBuildIndex);
            }
        }
    }
}
