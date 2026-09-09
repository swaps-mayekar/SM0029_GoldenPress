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
}
