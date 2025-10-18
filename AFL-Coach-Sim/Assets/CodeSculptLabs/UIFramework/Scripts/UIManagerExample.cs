using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeSculptLabs.UIFramework.Example
{
    /// <summary>
    /// Demonstrates the usage of UIManager with all UI element types.
    /// </summary>
    public class UIManagerFullExample : MonoBehaviour
    {
        #region Unity Methods

        private void Start()
        {
            SetAllListeners();
            InitializeUIElements();
        }

        #endregion

        #region Listener Setup Methods

        /// <summary>
        /// Sets up all listeners for the UI elements.
        /// </summary>
        private void SetAllListeners()
        {
            SetButtonListeners();
            SetToggleListeners();
            SetSliderListeners();
            SetInputFieldListeners();
            SetDropdownListeners();
            // Add more listener setups here if needed
        }

        private void SetButtonListeners()
        {
            UIManager.Instance.SetUIComponentListener<Button>(UI_Library.ExampleButton_Path, OnExampleButtonClick);
        }

        private void SetToggleListeners()
        {
            UIManager.Instance.SetUIComponentListener<Toggle, bool>(UI_Library.ExampleToggle_Path, OnExampleToggleValueChanged);
        }

        private void SetSliderListeners()
        {
            UIManager.Instance.SetUIComponentListener<Slider, float>(UI_Library.ExampleSlider_Path, OnExampleSliderValueChanged);
        }

        private void SetInputFieldListeners()
        {
            // For Unity's default InputField
            UIManager.Instance.SetUIComponentListener<InputField, string>(UI_Library.ExampleInputField_Path, OnExampleInputFieldValueChanged);

            // For TextMeshPro's TMP_InputField
            UIManager.Instance.SetUIComponentListener<TMP_InputField, string>(UI_Library.ExampleTMP_InputField_Path, OnExampleTMP_InputFieldValueChanged);

        }

        private void SetDropdownListeners()
        {
            UIManager.Instance.SetUIComponentListener<Dropdown, int>(UI_Library.ExampleDropdown_Path, OnExampleDropdownValueChanged);
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes UI elements that require setup.
        /// </summary>
        private void InitializeUIElements()
        {
            // Set initial text for Text components
            UIManager.Instance.SetUIComponentProperty<Text>(UI_Library.ExampleText_Path, text =>
            {
                text.text = "Initial Text";
            });
            UIManager.Instance.SetUIComponentProperty<TMP_Text>(UI_Library.ExampleTMP_Text_Path, tmpText =>
            {
                tmpText.text = "Initial TMP_Text";
            });

            // Set initial visibility for panels
            UIManager.Instance.SetPanelActive(UI_Library.Example_Panel_Path, false);
        }

        #endregion

        #region Callback Methods

        private void OnExampleButtonClick()
        {
            Debug.Log("Example Button Clicked.");

            // Change the button's image color to red
            UIManager.Instance.SetUIComponentProperty<Button>(UI_Library.ExampleButton_Path, button =>
            {
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.red;
                }
            });

            // Toggle the visibility of the Example Panel
            bool isPanelActive = UIManager.Instance.GetPanel(UI_Library.Example_Panel_Path)?.activeSelf ?? false;
            UIManager.Instance.SetPanelActive(UI_Library.Example_Panel_Path, !isPanelActive, deactivateOthers: false);
        }

        private void OnExampleToggleValueChanged(bool isOn)
        {
            Debug.Log($"Example Toggle Value Changed: {isOn}");

            // Update Text to show the toggle state
            UIManager.Instance.SetUIComponentProperty<Text>(UI_Library.ExampleText_Path, text =>
            {
                text.text = isOn ? "Toggle is ON" : "Toggle is OFF";
            });
        }

        private void OnExampleSliderValueChanged(float value)
        {
            Debug.Log($"Example Slider Value Changed: {value}");

            // Update TMP_Text to show the slider value
            UIManager.Instance.SetUIComponentProperty<TMP_Text>(UI_Library.ExampleTMP_Text_Path, tmpText =>
            {
                tmpText.text = $"Slider Value: {value:F2}";
            });

        }

        private void OnExampleInputFieldValueChanged(string input)
        {
            Debug.Log($"Example InputField Text Changed: {input}");

            // Update Text to show the input field text
            UIManager.Instance.SetUIComponentProperty<Text>(UI_Library.ExampleText_Path, text =>
            {
                text.text = $"InputField Input: {input}";
            });
        }

        private void OnExampleTMP_InputFieldValueChanged(string input)
        {
            Debug.Log($"Example TMP_InputField Text Changed: {input}");

            // Update TMP_Text to show the input field text
            UIManager.Instance.SetUIComponentProperty<TMP_Text>(UI_Library.ExampleTMP_Text_Path, tmpText =>
            {
                tmpText.text = $"TMP_InputField Input: {input}";
            });
        }

        private void OnExampleDropdownValueChanged(int selectedIndex)
        {
            Debug.Log($"Example Dropdown Value Changed: {selectedIndex}");

            // Update Text to show the selected dropdown option
            UIManager.Instance.SetUIComponentProperty<Text>(UI_Library.ExampleText_Path, text =>
            {
                text.text = $"Selected Option: {selectedIndex}";
            });
        }

        #endregion

        #region Additional Example Methods

        // Methods for other UI elements

        /// <summary>
        /// Example method to manipulate a ScrollView.
        /// </summary>
        public void ScrollToTop()
        {
            UIManager.Instance.SetUIComponentProperty<ScrollRect>(UI_Library.ExampleScrollView_Path, scrollRect =>
            {
                scrollRect.verticalNormalizedPosition = 1f; // Scroll to top
            });
        }

        /// <summary>
        /// Example method to change an Image's sprite.
        /// </summary>
        public void ChangeExampleImageSprite(Sprite newSprite)
        {
            UIManager.Instance.SetUIComponentProperty<Image>(UI_Library.ExampleImage_Path, image =>
            {
                image.sprite = newSprite;
            });
        }

        /// <summary>
        /// Example method to adjust the alpha of a CanvasGroup.
        /// </summary> 
        public void SetCanvasGroupAlpha(float alpha)
        {
            UIManager.Instance.SetUIComponentProperty<CanvasGroup>(UI_Library.ExampleCanvasGroup_Path, canvasGroup =>
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            });
        }

        #endregion
    }
}
