using UnityEngine;

namespace CodeSculptLabs.UIFramework
{
    /// <summary>
    /// Represents a reference to a UI element in the scene.
    /// </summary>
    [System.Serializable]
    public class UIReference
    {
        public string name;
        public string fullPath;
        public string instanceID;
        public GameObject uiElement;
        public UIElementType elementType;
    }

    /// <summary>
    /// Enum representing different types of UI elements.
    /// </summary>
    public enum UIElementType
    {
        Panel,
        Button,
        Text,
        TMP_Text,
        Toggle,
        InputField,
        TMP_InputField,
        Slider,
        Dropdown,
        ScrollView,
        Image,
        RawImage,
        Mask,
        Canvas,
        CanvasGroup,
        Unknown
    }
}
