using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    public class PhysicalButtonGroupDetector : MonoBehaviour
    {
        [Tooltip("All buttons that must be pressed at the same time.")]
        public PhysicalButton2D[] buttons;

        public UnityEvent onAllPressed;
        public UnityEvent onAnyReleased;

        public bool AreAllPressed { get; private set; }

        private void Update()
        {
            bool allPressedNow = AreAllButtonsPressed();

            if (allPressedNow == AreAllPressed)
                return;

            AreAllPressed = allPressedNow;

            if (AreAllPressed)
                onAllPressed?.Invoke();
            else
                onAnyReleased?.Invoke();
        }

        private bool AreAllButtonsPressed()
        {
            if (buttons == null || buttons.Length == 0)
                return false;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null || !buttons[i].IsPressed)
                    return false;
            }

            return true;
        }
    }
}
