using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using TMPro;

namespace CodeSculptLabs.UIFramework
{
    /// <summary>
    /// Class representing a category of UI elements.
    /// </summary>
    [System.Serializable]
    public class UICategory
    {
        public string name;
        public List<UIReference> references = new List<UIReference>();
    }

    /// <summary>
    /// Manages UI elements, providing methods to add, remove, and access them.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private List<UICategory> uiCategories = new List<UICategory>();

        #endregion

        #region Singleton Pattern

        private static UIManager instance;

        /// <summary>
        /// Singleton instance of the UIManager.
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<UIManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject(nameof(UIManager));
                        instance = obj.AddComponent<UIManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return instance;
            }
        }

        #endregion

        #region UI Reference Management

        private Dictionary<string, UIReference> uiReferenceByPath = new Dictionary<string, UIReference>();
        private Dictionary<string, UIReference> uiReferenceByInstanceID = new Dictionary<string, UIReference>();

        private void Awake()
        {
            InitializeDictionaries();
        }

        /// <summary>
        /// Initializes the dictionaries from the serialized uiCategories.
        /// </summary>
        private void InitializeDictionaries()
        {
            uiReferenceByPath.Clear();
            uiReferenceByInstanceID.Clear();

            foreach (var category in uiCategories)
            {
                foreach (var reference in category.references)
                {
                    // Ensure the GameObject reference is still valid
                    if (reference.uiElement != null)
                    {
                        uiReferenceByPath[reference.fullPath] = reference;
                        uiReferenceByInstanceID[reference.instanceID] = reference;
                    }
                    else
                    {
                        Debug.LogWarning($"UIManager: UI element '{reference.name}' is missing or has been destroyed.");
                    }
                }
            }
        }

        /// <summary>
        /// Adds a UI reference to the manager.
        /// </summary>
        /// <param name="uiElement">The UI element to add.</param>
        public void AddUIReference(GameObject uiElement)
        {
            if (uiElement == null)
            {
                Debug.LogError("UIManager: Attempted to add a null UI element.");
                return;
            }

            UIElementType type = DetermineUIElementType(uiElement);
            string categoryName = type.ToString();
            string fullPath = GetFullPath(uiElement.transform);
            string instanceID = uiElement.GetInstanceID().ToString();

            if (IsReferenceAlreadyAdded(fullPath, instanceID))
            {
                Debug.LogWarning($"UIManager: Duplicate UI element '{uiElement.name}' attempted to be added. Skipping.");
                return;
            }

            UICategory category = uiCategories.Find(cat => cat.name == categoryName);
            if (category == null)
            {
                category = new UICategory { name = categoryName };
                uiCategories.Add(category);
            }

            UIReference reference = new UIReference
            {
                name = uiElement.name,
                uiElement = uiElement,
                elementType = type,
                fullPath = fullPath,
                instanceID = instanceID
            };

            category.references.Add(reference);
            uiReferenceByPath[fullPath] = reference;
            uiReferenceByInstanceID[instanceID] = reference;
        }

        private bool IsReferenceAlreadyAdded(string fullPath, string instanceID)
        {
            return uiReferenceByPath.ContainsKey(fullPath) || uiReferenceByInstanceID.ContainsKey(instanceID);
        }

        private Dictionary<Transform, string> pathCache = new Dictionary<Transform, string>();

        private string GetFullPath(Transform transform)
        {
            if (transform == null)
            {
                Debug.LogError("UIManager: Null transform provided for GetFullPath.");
                return string.Empty;
            }

            if (pathCache.TryGetValue(transform, out string cachedPath))
            {
                return cachedPath;
            }

            string path = transform.name;
            Transform current = transform;

            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            pathCache[transform] = path;
            return path;
        }

        /// <summary>
        /// Removes a UI reference by full hierarchical path.
        /// </summary>
        /// <param name="fullPath">The full path of the UI element to remove.</param>
        public void RemoveUIReference(string fullPath)
        {
            if (uiReferenceByPath.TryGetValue(fullPath, out UIReference reference))
            {
                uiReferenceByPath.Remove(fullPath);
                uiReferenceByInstanceID.Remove(reference.instanceID);

                UICategory category = uiCategories.Find(cat => cat.name == reference.elementType.ToString());
                if (category != null)
                {
                    category.references.Remove(reference);
                }
            }
            else
            {
                Debug.LogWarning($"UIManager: No reference found with path '{fullPath}'.");
            }
        }

        /// <summary>
        /// Removes a UI reference by instance ID.
        /// </summary>
        /// <param name="instanceID">The instance ID of the UI element to remove.</param>
        public void RemoveUIReferenceByInstanceID(string instanceID)
        {
            if (uiReferenceByInstanceID.TryGetValue(instanceID, out UIReference reference))
            {
                uiReferenceByInstanceID.Remove(instanceID);
                uiReferenceByPath.Remove(reference.fullPath);

                UICategory category = uiCategories.Find(cat => cat.name == reference.elementType.ToString());
                if (category != null)
                {
                    category.references.Remove(reference);
                }
            }
            else
            {
                Debug.LogWarning($"UIManager: No reference found with instance ID '{instanceID}'.");
            }
        }

        /// <summary>
        /// Gets a UI reference by full hierarchical path.
        /// </summary>
        private GameObject GetUIReference(UIElementType elementType, string key)
        {
            if (uiReferenceByPath.TryGetValue(key, out UIReference reference))
            {
                return reference.uiElement;
            }

            Debug.LogWarning($"UIManager: No reference found with path '{key}'.");
            return null;
        }

        /// <summary>
        /// Gets a UI reference by instance ID.
        /// </summary>
        private GameObject GetUIReferenceByInstanceID(string instanceID)
        {
            if (uiReferenceByInstanceID.TryGetValue(instanceID, out UIReference reference))
            {
                return reference.uiElement;
            }

            Debug.LogWarning($"UIManager: No reference found with instance ID '{instanceID}'.");
            return null;
        }

        private UIElementType DetermineUIElementType(GameObject uiElement)
        {
            if (uiElement.GetComponent<Button>() != null) return UIElementType.Button;
            if (uiElement.GetComponent<Text>() != null) return UIElementType.Text;
            if (uiElement.GetComponent<TMP_Text>() != null) return UIElementType.TMP_Text;
            if (uiElement.GetComponent<Toggle>() != null) return UIElementType.Toggle;
            if (uiElement.GetComponent<InputField>() != null) return UIElementType.InputField;
            if (uiElement.GetComponent<TMP_InputField>() != null) return UIElementType.TMP_InputField;
            if (uiElement.GetComponent<Slider>() != null) return UIElementType.Slider;
            if (uiElement.GetComponent<Dropdown>() != null) return UIElementType.Dropdown;
            if (uiElement.GetComponent<TMP_Dropdown>() != null) return UIElementType.Dropdown; // Support for TMP_Dropdown
            if (uiElement.GetComponent<ScrollRect>() != null) return UIElementType.ScrollView;
            if (uiElement.GetComponent<Scrollbar>() != null) return UIElementType.ScrollView; // Support for Scrollbar

            if (uiElement.GetComponent<Image>() != null)
            {
                if (uiElement.name.EndsWith("_Panel", StringComparison.OrdinalIgnoreCase)) return UIElementType.Panel;
                return UIElementType.Image;
            }

            if (uiElement.GetComponent<RawImage>() != null) return UIElementType.RawImage;
            if (uiElement.GetComponent<Mask>() != null) return UIElementType.Mask;
            if (uiElement.GetComponent<Canvas>() != null) return UIElementType.Canvas;
            if (uiElement.GetComponent<CanvasGroup>() != null) return UIElementType.CanvasGroup;

            return UIElementType.Unknown;
        }

        /// <summary>
        /// Gets all UI categories.
        /// </summary>
        /// <returns>List of all UI categories.</returns>
        public List<UICategory> GetAllUICategories()
        {
            return uiCategories;
        }

        #endregion

        #region Generic UI Component Management

        private UIElementType GetElementTypeFromComponent<T>() where T : Component
        {
            if (typeof(T) == typeof(Button)) return UIElementType.Button;
            if (typeof(T) == typeof(Text)) return UIElementType.Text;
            if (typeof(T) == typeof(TMP_Text)) return UIElementType.TMP_Text;
            if (typeof(T) == typeof(Toggle)) return UIElementType.Toggle;
            if (typeof(T) == typeof(InputField)) return UIElementType.InputField;
            if (typeof(T) == typeof(TMP_InputField)) return UIElementType.TMP_InputField;
            if (typeof(T) == typeof(Slider)) return UIElementType.Slider;
            if (typeof(T) == typeof(Dropdown) || typeof(T) == typeof(TMP_Dropdown)) return UIElementType.Dropdown;
            if (typeof(T) == typeof(ScrollRect) || typeof(T) == typeof(Scrollbar)) return UIElementType.ScrollView;
            if (typeof(T) == typeof(Image)) return UIElementType.Image;
            if (typeof(T) == typeof(RawImage)) return UIElementType.RawImage;
            if (typeof(T) == typeof(Mask)) return UIElementType.Mask;
            if (typeof(T) == typeof(Canvas)) return UIElementType.Canvas;
            if (typeof(T) == typeof(CanvasGroup)) return UIElementType.CanvasGroup;
            return UIElementType.Unknown;
        }

        /// <summary>
        /// Gets a UI component of type T.
        /// </summary>
        public T GetUIComponent<T>(string key, bool isInstanceID = false) where T : Component
        {
            UIElementType elementType = GetElementTypeFromComponent<T>();
            if (elementType == UIElementType.Unknown)
            {
                Debug.LogWarning($"UIManager: Unknown component type '{typeof(T).Name}'.");
                return null;
            }

            GameObject uiElement = isInstanceID ? GetUIReferenceByInstanceID(key) : GetUIReference(elementType, key);

            if (uiElement == null)
            {
                Debug.LogWarning($"UIManager: UI element not found for key '{key}'.");
                return null;
            }

            T component = uiElement.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"UIManager: Component '{typeof(T).Name}' not found on UI element for key '{key}'.");
            }
            return component;
        }

        /// <summary>
        /// Sets a property on a UI component.
        /// </summary>
        public void SetUIComponentProperty<T>(string key, Action<T> propertySetter, bool isInstanceID = false) where T : Component
        {
            T component = GetUIComponent<T>(key, isInstanceID);
            if (component != null)
            {
                propertySetter(component);
            }
        }

        /// <summary>
        /// Sets a UnityAction listener on a UI component.
        /// </summary>
        public void SetUIComponentListener<T>(string key, UnityAction action, bool isInstanceID = false) where T : Component
        {
            T component = GetUIComponent<T>(key, isInstanceID);
            if (component == null) return;

            if (component is Button button)
            {
                button.onClick.AddListener(action);
            }
            else
            {
                Debug.LogWarning($"UIManager: Unsupported component type '{typeof(T).Name}' for UnityAction.");
            }
        }

        /// <summary>
        /// Sets a UnityAction<TEvent> listener on a UI component.
        /// </summary>
        public void SetUIComponentListener<T, TEvent>(string key, UnityAction<TEvent> action, bool isInstanceID = false) where T : Component
        {
            T component = GetUIComponent<T>(key, isInstanceID);
            if (component == null) return;

            switch (component)
            {
                case Toggle toggle when action is UnityAction<bool> boolAction:
                    toggle.onValueChanged.AddListener(boolAction);
                    break;
                case Slider slider when action is UnityAction<float> floatAction:
                    slider.onValueChanged.AddListener(floatAction);
                    break;
                case InputField inputField when action is UnityAction<string> stringAction:
                    inputField.onValueChanged.AddListener(stringAction);
                    break;
                case TMP_InputField tmpInputField when action is UnityAction<string> tmpStringAction:
                    tmpInputField.onValueChanged.AddListener(tmpStringAction);
                    break;
                case Dropdown dropdown when action is UnityAction<int> intAction:
                    dropdown.onValueChanged.AddListener(intAction);
                    break;
                case TMP_Dropdown tmpDropdown when action is UnityAction<int> tmpIntAction:
                    tmpDropdown.onValueChanged.AddListener(tmpIntAction);
                    break;
                case ScrollRect scrollRect when action is UnityAction<Vector2> vector2Action:
                    scrollRect.onValueChanged.AddListener(vector2Action);
                    break;
                default:
                    Debug.LogWarning($"UIManager: Unsupported component type '{typeof(T).Name}' or action type '{typeof(TEvent).Name}'.");
                    break;
            }
        }

        #endregion

        #region Panel Activation and Deactivation

        private GameObject lastActivePanel = null;
        private readonly List<GameObject> activePanels = new List<GameObject>();

        /// <summary>
        /// Gets a panel GameObject by key.
        /// </summary>
        public GameObject GetPanel(string key, bool isInstanceID = false)
        {
            GameObject uiElement = isInstanceID ? GetUIReferenceByInstanceID(key) : GetUIReference(UIElementType.Panel, key);

            if (uiElement == null)
            {
                Debug.LogWarning($"UIManager: Panel not found for key '{key}'.");
                return null;
            }

            if (uiElement.GetComponent<Image>() == null)
            {
                Debug.LogWarning($"UIManager: UI element '{key}' is not a Panel (missing Image component).");
                return null;
            }

            return uiElement;
        }

        /// <summary>
        /// Sets the active state of a panel.
        /// </summary>
        public void SetPanelActive(string key, bool isActive, bool isInstanceID = false, bool deactivateOthers = true, bool keepLastPanel = false)
        {
            GameObject panel = GetPanel(key, isInstanceID);
            if (panel == null) return;

            if (isActive)
            {
                HandlePanelActivation(panel, deactivateOthers, keepLastPanel);
            }
            else
            {
                panel.SetActive(false);
                UpdateActivePanelsList(panel, false);
            }
        }

        private void HandlePanelActivation(GameObject panel, bool deactivateOthers, bool keepLastPanel)
        {
            if (deactivateOthers)
            {
                foreach (var activePanel in new List<GameObject>(activePanels))
                {
                    if (activePanel != panel && (!keepLastPanel || activePanel != lastActivePanel))
                    {
                        activePanel.SetActive(false);
                        activePanels.Remove(activePanel);
                    }
                }
            }

            panel.SetActive(true);
            UpdateActivePanelsList(panel, true);
            lastActivePanel = panel;
        }

        private void UpdateActivePanelsList(GameObject panel, bool isActive)
        {
            if (isActive)
            {
                if (!activePanels.Contains(panel))
                {
                    activePanels.Add(panel);
                }
            }
            else
            {
                activePanels.Remove(panel);
                if (lastActivePanel == panel)
                {
                    lastActivePanel = null;
                }
            }
        }

        #endregion
    }
}
