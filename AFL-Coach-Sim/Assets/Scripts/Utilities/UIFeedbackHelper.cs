// Assets/Scripts/Utilities/UIFeedbackHelper.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AFLManager.Utilities
{
    /// <summary>
    /// Helper for UI feedback like toasts, confirmations, and notifications
    /// </summary>
    public class UIFeedbackHelper : MonoBehaviour
    {
        private static UIFeedbackHelper instance;
        
        [Header("Toast Settings")]
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private Transform toastContainer;
        [SerializeField] private float toastDuration = 3f;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private System.Action currentConfirmAction;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            
            if (confirmButton)
                confirmButton.onClick.AddListener(OnConfirm);
            
            if (cancelButton)
                cancelButton.onClick.AddListener(OnCancel);
            
            if (confirmationDialog)
                confirmationDialog.SetActive(false);
        }
        
        /// <summary>
        /// Show a toast message
        /// </summary>
        public static void ShowToast(string message, float duration = 3f)
        {
            if (instance != null)
            {
                instance.CreateToast(message, duration);
            }
            else
            {
                Debug.Log($"[Toast] {message}");
            }
        }
        
        /// <summary>
        /// Show a success message
        /// </summary>
        public static void ShowSuccess(string message)
        {
            ShowToast($"✓ {message}");
        }
        
        /// <summary>
        /// Show an error message
        /// </summary>
        public static void ShowError(string message)
        {
            ShowToast($"✗ {message}");
        }
        
        /// <summary>
        /// Show a confirmation dialog
        /// </summary>
        public static void ShowConfirmation(string message, System.Action onConfirm, System.Action onCancel = null)
        {
            if (instance != null)
            {
                instance.CreateConfirmation(message, onConfirm, onCancel);
            }
            else
            {
                Debug.LogWarning("UIFeedbackHelper not initialized");
                onConfirm?.Invoke();
            }
        }
        
        private void CreateToast(string message, float duration)
        {
            if (toastPrefab == null || toastContainer == null)
            {
                Debug.Log($"[Toast] {message}");
                return;
            }
            
            var toast = Instantiate(toastPrefab, toastContainer);
            var text = toast.GetComponentInChildren<TextMeshProUGUI>();
            if (text) text.text = message;
            
            StartCoroutine(FadeOutAndDestroy(toast, duration));
        }
        
        private IEnumerator FadeOutAndDestroy(GameObject toast, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            var canvasGroup = toast.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                float elapsed = 0f;
                float fadeTime = 0.5f;
                
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1f - (elapsed / fadeTime);
                    yield return null;
                }
            }
            
            Destroy(toast);
        }
        
        private void CreateConfirmation(string message, System.Action onConfirm, System.Action onCancel)
        {
            if (confirmationDialog == null)
            {
                Debug.LogWarning("Confirmation dialog not assigned");
                onConfirm?.Invoke();
                return;
            }
            
            currentConfirmAction = onConfirm;
            
            if (confirmationText)
                confirmationText.text = message;
            
            confirmationDialog.SetActive(true);
        }
        
        private void OnConfirm()
        {
            currentConfirmAction?.Invoke();
            currentConfirmAction = null;
            
            if (confirmationDialog)
                confirmationDialog.SetActive(false);
        }
        
        private void OnCancel()
        {
            currentConfirmAction = null;
            
            if (confirmationDialog)
                confirmationDialog.SetActive(false);
        }
    }
}
