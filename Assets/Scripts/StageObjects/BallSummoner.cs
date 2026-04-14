using System;
using UnityEngine;

namespace StageObjects
{
    public class BallSummoner : MonoBehaviour
    {
        [SerializeField]
        private GameObject ballPrefab;
        
        public void SummonBall()
        {
            Instantiate(ballPrefab, transform);
        }
    }
}
