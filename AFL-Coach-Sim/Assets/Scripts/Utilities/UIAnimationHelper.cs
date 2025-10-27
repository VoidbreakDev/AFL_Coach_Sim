// Assets/Scripts/Utilities/UIAnimationHelper.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AFLManager.Utilities
{
    /// <summary>
    /// Simple UI animations for buttons, panels, and elements
    /// </summary>
    public static class UIAnimationHelper
    {
        /// <summary>
        /// Animate button press
        /// </summary>
        public static void AnimateButtonPress(Button button, System.Action onComplete = null)
        {
            if (button == null) return;
            
            var mono = button.GetComponent<MonoBehaviour>();
            if (mono) mono.StartCoroutine(ButtonPressAnimation(button.transform, onComplete));
        }
        
        private static IEnumerator ButtonPressAnimation(Transform transform, System.Action onComplete)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 pressedScale = originalScale * 0.9f;
            
            // Press down
            float duration = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
                yield return null;
            }
            
            // Pop back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Fade in a UI element
        /// </summary>
        public static void FadeIn(MonoBehaviour context, CanvasGroup canvasGroup, float duration = 0.3f, System.Action onComplete = null)
        {
            if (canvasGroup == null) return;
            context.StartCoroutine(FadeCoroutine(canvasGroup, 0f, 1f, duration, onComplete));
        }
        
        /// <summary>
        /// Fade out a UI element
        /// </summary>
        public static void FadeOut(MonoBehaviour context, CanvasGroup canvasGroup, float duration = 0.3f, System.Action onComplete = null)
        {
            if (canvasGroup == null) return;
            context.StartCoroutine(FadeCoroutine(canvasGroup, 1f, 0f, duration, onComplete));
        }
        
        private static IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float from, float to, float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            
            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Slide in a panel from side
        /// </summary>
        public static void SlideIn(MonoBehaviour context, RectTransform rectTransform, SlideDirection direction, float duration = 0.3f, System.Action onComplete = null)
        {
            if (rectTransform == null) return;
            
            Vector2 endPosition = rectTransform.anchoredPosition;
            Vector2 startPosition = GetOffscreenPosition(rectTransform, direction);
            
            context.StartCoroutine(SlideCoroutine(rectTransform, startPosition, endPosition, duration, onComplete));
        }
        
        /// <summary>
        /// Slide out a panel to side
        /// </summary>
        public static void SlideOut(MonoBehaviour context, RectTransform rectTransform, SlideDirection direction, float duration = 0.3f, System.Action onComplete = null)
        {
            if (rectTransform == null) return;
            
            Vector2 startPosition = rectTransform.anchoredPosition;
            Vector2 endPosition = GetOffscreenPosition(rectTransform, direction);
            
            context.StartCoroutine(SlideCoroutine(rectTransform, startPosition, endPosition, duration, onComplete));
        }
        
        private static IEnumerator SlideCoroutine(RectTransform rectTransform, Vector2 from, Vector2 to, float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            
            rectTransform.anchoredPosition = to;
            onComplete?.Invoke();
        }
        
        private static Vector2 GetOffscreenPosition(RectTransform rectTransform, SlideDirection direction)
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            switch (direction)
            {
                case SlideDirection.Left:
                    return new Vector2(-screenWidth, currentPos.y);
                case SlideDirection.Right:
                    return new Vector2(screenWidth, currentPos.y);
                case SlideDirection.Up:
                    return new Vector2(currentPos.x, screenHeight);
                case SlideDirection.Down:
                    return new Vector2(currentPos.x, -screenHeight);
                default:
                    return currentPos;
            }
        }
        
        /// <summary>
        /// Pulse animation for highlighting
        /// </summary>
        public static void Pulse(MonoBehaviour context, Transform transform, float scale = 1.1f, float duration = 0.5f)
        {
            context.StartCoroutine(PulseCoroutine(transform, scale, duration));
        }
        
        private static IEnumerator PulseCoroutine(Transform transform, float scale, float duration)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * scale;
            
            float halfDuration = duration / 2f;
            float elapsed = 0f;
            
            // Scale up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
    }
    
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }
}
