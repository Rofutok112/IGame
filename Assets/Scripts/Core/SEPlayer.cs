using UnityEngine;

namespace Core
{
    public class SEPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip audioClip;
        
        public void PlaySE()
        {
            audioSource.PlayOneShot(audioClip);
        }
    }
}