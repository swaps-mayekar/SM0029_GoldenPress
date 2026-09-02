using System.Collections;
using UnityEngine;

namespace GoldenPress.UI
{
    public static class SimpleTween
    {
        public static IEnumerator ScalePunch(Transform target, float peak = 1.08f, float duration = 0.18f)
        {
            if (target == null)
            {
                yield break;
            }

            var original = target.localScale;
            var half = duration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(t / half);
                target.localScale = Vector3.Lerp(original, original * peak, p);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(t / half);
                target.localScale = Vector3.Lerp(original * peak, original, p);
                yield return null;
            }

            target.localScale = original;
        }

        public static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            float t = 0f;
            group.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            group.alpha = to;
        }
    }

    public sealed class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; }

        private bool _muted;

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                AudioListener.volume = _muted ? 0f : 1f;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void PlayUiTap()
        {
            // Placeholder hook: no clips shipped in this milestone.
        }

        public void PlaySuccess()
        {
        }

        public void PlayCoin()
        {
        }
    }
}
