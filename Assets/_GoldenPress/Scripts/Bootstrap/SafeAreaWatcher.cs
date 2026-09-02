using UnityEngine;

namespace GoldenPress.Bootstrap
{
    /// <summary>
    /// Ensures landscape-friendly safe-area updates when device insets change.
    /// </summary>
    public sealed class SafeAreaWatcher : MonoBehaviour
    {
        private RectTransform _target;
        private Rect _lastSafeArea;

        public void SetTarget(RectTransform target)
        {
            _target = target;
            Apply();
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            if (_lastSafeArea != Screen.safeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            GoldenPress.UI.UiFactory.ApplySafeArea(_target);
        }
    }
}
