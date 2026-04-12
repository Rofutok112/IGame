using System;
using UnityEngine;

namespace StageObjects
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BallSummoner : MonoBehaviour
    {
        private Rigidbody2D _rigidbody2D;

        private void Start()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _rigidbody2D.bodyType = RigidbodyType2D.Static;
        }
        
        public void SummonBall()
        {
            _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}
