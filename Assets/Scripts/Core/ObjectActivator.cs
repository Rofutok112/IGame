using System;
using UnityEngine;

namespace Core
{
    public class ObjectActivator : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void ActivateObject()
        {
            gameObject.SetActive(true);
        }
    }
}