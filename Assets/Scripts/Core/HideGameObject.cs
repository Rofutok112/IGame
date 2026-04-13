using UnityEngine;

namespace IGame.Core
{
    public class HideGameObject : MonoBehaviour
    {
        [Tooltip("GameObject to hide. If empty, this GameObject is used.")]
        public GameObject target;

        public void Hide()
        {
            GameObject resolvedTarget = target != null ? target : gameObject;
            resolvedTarget.SetActive(false);
        }
    }
}
