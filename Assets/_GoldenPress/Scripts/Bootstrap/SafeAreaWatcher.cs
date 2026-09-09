using UnityEngine;

namespace GoldenPress.Bootstrap
{
    /// <summary>
    /// Keeps a RectTransform fitted to Screen.safeArea. Place on SafeRoot.
    /// </summary>
    public sealed class SafeAreaWatcher : MonoBehaviour
    {
        private RectTransform _target;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _target = GetComponent<RectTransform>();
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
            if (_target == null)
            {
                return;
            }

            _lastSafeArea = Screen.safeArea;
            GoldenPress.UI.UiFactory.ApplySafeArea(_target);
        }
    }
}
