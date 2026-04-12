using UnityEngine;
using UnityEngine.SceneManagement;

namespace IGame.Core
{
    public class SceneReloader : MonoBehaviour
    {
        public void ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
