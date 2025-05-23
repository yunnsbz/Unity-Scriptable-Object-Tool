using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ScriptableObjectManager
{

    public class ScriptableObjectEditorWindow : EditorWindow
    {
        //prefs:
        public const string BASIC_FILTERS_PREF = "basic_filters";
        public const string PROPERTY_SPACE_PREF = "property_space";
        public const string TABLE_ORIENTATION_PREF = "table_orientation";
        private const char SEPARATOR = '|'; // Filtrelerde kullanýlmayacak özel bir karakter

        public static string[] BasicFilters
        {
            get
            {
                string savedFilters = EditorPrefs.GetString(BASIC_FILTERS_PREF, "");
                return string.IsNullOrEmpty(savedFilters)
                    ? new string[0]
                    : savedFilters.Split(new[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            }
            set
            {
                string filtersString = string.Join(SEPARATOR.ToString(), value);
                EditorPrefs.SetString(BASIC_FILTERS_PREF, filtersString);
            }
        }

        // layout:
        private readonly float PropertyMinWidth = 40;
        private float PropertySpace = 5;
        private Vector2 ScrollPosMain = new();
        private readonly GUILayoutOption[] GUIL_StandartOptions = new GUILayoutOption[] { GUILayout.MinWidth(100), GUILayout.ExpandWidth(true) };
        private readonly GUILayoutOption[] GUIL_DefaultOptions = new GUILayoutOption[] { GUILayout.MinWidth(150), GUILayout.ExpandWidth(true) };
        private bool OrientationVertical = true; // Determines the orientation of the layout (true: vertical. false: horizontal)
                                                 // data:
        private List<List<ScriptableObject>> groupedConfigs; // Stores ScriptableObjects grouped by their class types in nested lists
        private List<Type> selectedTypes = new(); // Tracks which ScriptableObject types are currently selected for display
        private List<Type> availableTypes = new(); // Holds all unique ScriptableObject types found in the project

        // rename operation:
        bool isRenaming = false;
        bool FinishRenaming = false;
        ScriptableObject ObjectToRename = null;
        string renameText = "";

        // textures:
        private Texture2D spaceIcon;
        private Texture2D orientationIcon;
        private Texture2D ConfigOptionsIcon;
        private Texture2D addConfigIcon;
        private Texture2D refreshIcon;
        private Texture2D filtersIcon;
        private Texture2D checkIcon;

        // Buttons Styles:
        GUIContent spaceButton;
        GUIContent orientationButton;
        GUIContent refreshButton;
        GUIContent filtersButton;
        GUIContent checkButton;
        // create config button styles:
        GUIContent AddConfigButton;
        GUILayoutOption[] AddConfigButtonOptions;
        GUIStyle buttonStyle;
        // delete config button styles:
        GUIContent ConfigOptionsButton;
        GUILayoutOption[] ConfigOptionsButtonOptions;

        // label styles:
        GUIStyle centeredLabelStyle;

        // window:
        [MenuItem("Window/Game Config Editor")]
        public static void ShowWindow()
        {
            GetWindow<ScriptableObjectEditorWindow>("Game Config Editor");
        }

        private void OnEnable()
        {
            // Load the saved property space from EditorPrefs
            float propSpace = EditorPrefs.GetFloat(PROPERTY_SPACE_PREF, -2);
            if (propSpace == -2)
            {
                // If no space is set, use the default value
                PropertySpace = -1;
                EditorPrefs.SetFloat(PROPERTY_SPACE_PREF, PropertySpace);
            }
            else PropertySpace = propSpace;

            // Load the saved table orientation from EditorPrefs
            OrientationVertical = EditorPrefs.GetBool(TABLE_ORIENTATION_PREF, true);

            // Initialize the editor by discovering available types and organizing ScriptableObjects accordingly
            LoadAvailableTypes();
            GroupScriptableObjectsByType();

            // Load icons for UI buttons
            LoadIcons();

            // setup styles 
            SetupButtonStyles();
        }

        /// <summary>
        /// Sets up the GUI buttons with their respective icons and tooltips.
        /// </summary>
        private void SetupButtonStyles()
        {
            // GUI content for space button
            if (spaceIcon != null)
            {
                spaceButton = new GUIContent(spaceIcon, "change space between parameters");
            }
            else
            {
                spaceButton = new GUIContent("space", "change space between parameters");
            }

            // GUI content for orientation button
            if (orientationIcon != null)
            {
                orientationButton = new GUIContent(orientationIcon, "change the table orientation");
            }
            else
            {
                orientationButton = new GUIContent("rotate", "change the table orientation");
            }

            // GUI content for refresh button
            if (refreshIcon != null)
            {
                refreshButton = new GUIContent(refreshIcon, "refresh");
            }
            else
            {
                refreshButton = new GUIContent("refresh", "refresh");
            }

            // GUI content for filters button
            if (filtersIcon != null)
            {
                filtersButton = new GUIContent(filtersIcon, "filters");
            }
            else
            {
                filtersButton = new GUIContent("filters", "filters");
            }
            // GUI content for check button
            if (checkIcon != null)
            {
                checkButton = new GUIContent(checkIcon, "check");
            }
            else
            {
                checkButton = new GUIContent("ok", "check");
            }

            // 'create config' button styles
            if (addConfigIcon != null)
            {
                AddConfigButton = new GUIContent(addConfigIcon, "create new config");
                AddConfigButtonOptions = new GUILayoutOption[] { GUILayout.Height(20), GUILayout.Width(20) };
            }
            else
            {
                AddConfigButton = new GUIContent("Add new", "create new config");
                AddConfigButtonOptions = new GUILayoutOption[] { GUILayout.Width(65) };
            }

            // 'Config Options' button styles
            if (ConfigOptionsIcon != null)
            {
                ConfigOptionsButton = new GUIContent(ConfigOptionsIcon, "Options");
                ConfigOptionsButtonOptions = new GUILayoutOption[] { GUILayout.Height(20), GUILayout.Width(20) };
            }
            else
            {
                ConfigOptionsButton = new GUIContent("opt", "Options");
                ConfigOptionsButtonOptions = new GUILayoutOption[] { GUILayout.Width(30) };
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // A button to set space between properties
            if (GUILayout.Button(spaceButton, GUILayout.Width(50)))
            {
                SetSpace();
            }

            // A button to change the orientation of the table
            if (GUILayout.Button(orientationButton, GUILayout.Width(50)))
            {
                if (OrientationVertical)
                {
                    OrientationVertical = false;
                    EditorPrefs.SetBool(TABLE_ORIENTATION_PREF, OrientationVertical);
                }
                else
                {
                    OrientationVertical = true;
                    EditorPrefs.SetBool(TABLE_ORIENTATION_PREF, OrientationVertical);
                }
            }

            EditorGUILayout.Space();

            // A button to refresh the list of ScriptableObjects
            if (GUILayout.Button(refreshButton, GUILayout.Width(50)))
            {
                RefreshAll();
            }

            // Display a popup window at the mouse position for basic filters
            if (GUILayout.Button(filtersButton, GUILayout.Width(50)))
            {
                Vector2 mousePosition = Event.current.mousePosition;
                PopupWindow.Show(new Rect(mousePosition.x, mousePosition.y + 20, 0, 0), new ConfigTypeSelectionPopup(selectedTypes, GroupScriptableObjectsByType, availableTypes));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // if there is no SO loaded or selected from filters show a message
            if (selectedTypes == null || selectedTypes.Count == 0 || groupedConfigs == null || groupedConfigs.Count == 0)
            {
                EditorGUILayout.LabelField("[No Config Selected]", EditorStyles.boldLabel);
                return;
            }

            // show configs
            ScrollPosMain = EditorGUILayout.BeginScrollView(ScrollPosMain);
            if (groupedConfigs.Count != 0)
            {
                foreach (var configGroup in groupedConfigs)
                {
                    // Only display groups that match the selected types or if no specific types are filtered
                    if (selectedTypes.Count == 0 || !selectedTypes.Contains(configGroup[0].GetType()))
                        continue;

                    GUILayout.Space(20);

                    EditorGUILayout.BeginHorizontal();

                    // 'create config' button styles
                    if (addConfigIcon != null)
                    {
                        buttonStyle = new GUIStyle(GUI.skin.button);
                        buttonStyle.padding = new RectOffset(2, 2, 2, 2);
                        buttonStyle.imagePosition = ImagePosition.ImageOnly;
                    }

                    // create config button:
                    if (GUILayout.Button(AddConfigButton, buttonStyle, AddConfigButtonOptions))
                    {
                        AddNewSO(configGroup[0].GetType());
                    }

                    // show name of the ScriptableObject type
                    centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                    centeredLabelStyle.fontSize = 16;
                    GUILayout.Label(configGroup[0].GetType().Name, centeredLabelStyle);

                    EditorGUILayout.EndHorizontal();

                    // table:
                    if (OrientationVertical)
                    {
                        EditorGUILayout.BeginHorizontal();
                        PutPropertiesForObject_V(configGroup);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical("box");
                        PutPropertiesForObject_H(configGroup);
                        EditorGUILayout.EndVertical();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void SetSpace()
        {
            float propSpace = EditorPrefs.GetFloat(PROPERTY_SPACE_PREF, -2);
            if (propSpace == -2)
            {
                // If no space is set, use the default value
                PropertySpace = -1;
                EditorPrefs.SetFloat(PROPERTY_SPACE_PREF, PropertySpace);
            }
            else
            {
                // If space is already set, increase it by 1
                PropertySpace += 1;

                // If the space exceeds 5, reset it to 0
                if (PropertySpace > 5) PropertySpace = -1;

                // Save the new space value
                EditorPrefs.SetFloat(PROPERTY_SPACE_PREF, PropertySpace);

                Debug.Log("Property Space: " + PropertySpace);
            }
        }

        private void RefreshAll()
        {
            LoadAvailableTypes();
            GroupScriptableObjectsByType();
        }

        void AddNewSO(Type type)
        {
            // Create a new instance of the selected ScriptableObject type
            ScriptableObject newConfig = ScriptableObject.CreateInstance(type);

            // Save file panel that works within the project (Assets/)
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Config",
                type.Name + ".asset",
                "asset",
                "Please enter a file name to save the ScriptableObject.",
                "Assets/Resources/ScriptableObjects"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(newConfig, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            RefreshAll();
        }

        private void LoadIcons()
        {
            spaceIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/space.png");
            orientationIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/orientation.png");
            ConfigOptionsIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/options.png");
            addConfigIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/add file.png");
            refreshIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/refresh.png");
            filtersIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/filter.png");
            checkIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Editor Windows/Icons/check.png");

            if (spaceIcon == null)
            {
                Debug.LogError("space Icon not found in: Assets/Editor/Editor Windows/Icons/space.png");
            }
            if (orientationIcon == null)
            {
                Debug.LogError("orientation Icon not found in: Assets/Editor/Editor Windows/Icons/orientation.png");
            }
            if (ConfigOptionsIcon == null)
            {
                Debug.LogError("ConfigOptions Icon not found in: Assets/Editor/Editor Windows/Icons/options.png");
            }
            if (addConfigIcon == null)
            {
                Debug.LogError("addConfig Icon not found in: Assets/Editor/Editor Windows/Icons/add file.png");
            }
            if (refreshIcon == null)
            {
                Debug.LogError("refresh Icon not found in: Assets/Editor/Editor Windows/Icons/refresh.png");
            }
            if (filtersIcon == null)
            {
                Debug.LogError("filters Icon not found in: Assets/Editor/Editor Windows/Icons/filter.png");
            }
            if (checkIcon == null)
            {
                Debug.LogError("check Icon not found in: Assets/Editor/Editor Windows/Icons/check.png");
            }
        }

        private void LoadAvailableTypes()
        {
            selectedTypes.Clear();
            // Scan the "ScriptableObjects" folder to find all unique ScriptableObject types
            availableTypes = Resources.LoadAll<ScriptableObject>("ScriptableObjects").Select(t => t.GetType()).Distinct().ToList();
            foreach (var filter in BasicFilters)
            {
                if (string.IsNullOrEmpty(filter))
                    continue;
                // Add any additional types specified in the basic filters
                var type = availableTypes.FirstOrDefault(t => t.Name == filter);
                if (type != null)
                {
                    selectedTypes.Add(type);
                }
            }
        }

        private void GroupScriptableObjectsByType()
        {
            // Fetch all ScriptableObjects from a designated folder and organize them into groups based on their class types
            var configs = Resources.LoadAll<ScriptableObject>("").ToList();

            groupedConfigs = configs
                .GroupBy(c => c.GetType())
                .Where(g => selectedTypes.Count == 0 || selectedTypes.Contains(g.Key)) // Only include groups matching the current type filter
                .Select(g => g.ToList()) // Convert each group into a list of ScriptableObjects
                .ToList();
        }


        /// <summary>
        /// create a vertival table for the properties of the object
        /// </summary>
        private void PutPropertiesForObject_V<T>(List<T> Configs) where T : ScriptableObject
        {
            try
            {
                EditorGUILayout.BeginVertical("box", GUILayout.Width(170));
                try
                {
                    // An empty label to align property names properly in the UI
                    EditorGUILayout.LabelField("", GUILayout.MinWidth(PropertyMinWidth));

                    // Iterate through the properties of the first ScriptableObject to display their names
                    SerializedObject serializedObject = new(Configs[0]);
                    SerializedProperty property = serializedObject.GetIterator();

                    bool ShouldNext = property.NextVisible(true);
                    while (ShouldNext)
                    {
                        if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // Exclude internal Unity fields and arrays
                        {
                            // Show the name of each property as a label
                            EditorGUILayout.LabelField(property.name, GUIL_StandartOptions);
                            // if any property is bigger than expected this will calculate extra space needed (ex: array properties can expand)
                            if (property.isExpanded)
                                GUILayout.Space(CalculatePropertyHeight(Configs, property) - EditorGUIUtility.singleLineHeight);
                            GUILayout.Space(PropertySpace);
                        }
                        ShouldNext = property.NextVisible(false); // Move to the next property, skipping nested children
                    }
                }
                catch
                {
                    // If an error occurs, refresh the type list and regroup the objects
                    LoadAvailableTypes();
                    GroupScriptableObjectsByType();
                }
                EditorGUILayout.EndVertical();

                if (Configs.Count == 0)
                    return; // Exit if there are no configurations to display

                foreach (var Config in Configs)
                {
                    EditorGUILayout.BeginVertical("box");
                    try
                    {
                        // Retrieve and display the asset's file name without its extension
                        string filePath = AssetDatabase.GetAssetPath(Config);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        if (fileName == "") throw new System.Exception();

                        // Display the file name and a delete button for the asset
                        EditorGUILayout.BeginHorizontal();
                        GUIContent propertyContent = new GUIContent(fileName, fileName);

                        Rect elementRect = GUILayoutUtility.GetRect(120, 18);

                        if (isRenaming && ObjectToRename != null && ObjectToRename == Config)
                        {
                            GUI.SetNextControlName("RenameField");

                            // Display a text field for renaming the asset.if renameText is empty, show the file name until the user types something
                            renameText = EditorGUI.TextField(elementRect, renameText == "" ? fileName : renameText);

                            Event e = Event.current;

                            // textfield + confirm button (for mouse event it must also contain the button area)
                            elementRect.width += 25;
                            if (isRenaming && e.type == EventType.MouseDown && !elementRect.Contains(e.mousePosition))
                            {
                                isRenaming = false;
                                GUI.FocusControl(null);
                                e.Use();
                            }

                            if (FinishRenaming || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                            {
                                AssetDatabase.RenameAsset(filePath, renameText);
                                AssetDatabase.SaveAssets();
                                isRenaming = false;
                                FinishRenaming = false;
                                renameText = "";
                                ObjectToRename = null;
                                GUI.FocusControl(null);
                            }
                            else if (Event.current.keyCode == KeyCode.Escape)
                            {
                                renameText = "";
                                isRenaming = false;
                                ObjectToRename = null;
                                GUI.FocusControl(null);
                            }

                            EditorGUI.FocusTextInControl("RenameField");
                        }
                        else
                        {
                            EditorGUI.LabelField(elementRect, propertyContent, EditorStyles.miniBoldLabel);
                        }

                        OptionsButton(Config);
                        EditorGUILayout.EndHorizontal();

                        SerializedObject serializedObject = new(Config);
                        SerializedProperty property = serializedObject.GetIterator();

                        // Iterate through all properties to create editable fields based on their types
                        bool ShouldNext = property.NextVisible(true);
                        while (ShouldNext)
                        {
                            if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // Skip unwanted properties
                            {
                                EditorGUILayout.PropertyField(property, GUIContent.none, true, GUIL_DefaultOptions);
                                if (property.isExpanded)
                                {
                                    float extraSpace = CalculatePropertyHeight(Configs, property);
                                    extraSpace -= EditorGUI.GetPropertyHeight(property);
                                    GUILayout.Space(extraSpace);
                                }
                                GUILayout.Space(PropertySpace);
                                serializedObject.ApplyModifiedProperties();
                            }
                            ShouldNext = property.NextVisible(false); // Advance to the next property, excluding nested fields
                        }
                        property.Reset();
                    }
                    catch
                    {
                        // Handle errors by refreshing the type list and regrouping
                        LoadAvailableTypes();
                        GroupScriptableObjectsByType();
                    }
                    EditorGUILayout.EndVertical();
                }

            }
            catch
            {
                // Catch any top-level errors and reset the data
                LoadAvailableTypes();
                GroupScriptableObjectsByType();
            }
        }

        private void OptionsButton<T>(T Config) where T : ScriptableObject
        {

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            if (ConfigOptionsIcon != null)
            {
                buttonStyle.padding = new RectOffset(2, 2, 2, 2);
                buttonStyle.imagePosition = ImagePosition.ImageOnly;
            }
            if (isRenaming && ObjectToRename != null && ObjectToRename == Config)
            {
                if (GUILayout.Button(checkIcon, buttonStyle, ConfigOptionsButtonOptions))
                {
                    FinishRenaming = true;
                }
            }
            else
            {
                if (GUILayout.Button(ConfigOptionsButton, buttonStyle, ConfigOptionsButtonOptions))
                {
                    ShowOptionsMenu(Config);
                }
            }
            
        }

        private void ShowOptionsMenu<T>(T Config) where T : ScriptableObject
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Delete Config"), false, () => DeleteConfig(Config));
            menu.AddItem(new GUIContent("Rename Config"), false, () => { isRenaming = true; ObjectToRename = Config; renameText = ""; });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show In Project Folder"), false, () => ShowInProjectFolders(Config));
            menu.ShowAsContext();
        }

        /// <summary>
        /// create a Horizontal (parameters will be horizontal) table for the properties of the object
        /// </summary>
        private void PutPropertiesForObject_H<T>(List<T> Configs) where T : ScriptableObject
        {
            try
            {
                // property names on horizontal line:
                EditorGUILayout.BeginHorizontal();
                try
                {
                    // An empty label to align property names properly in the UI
                    EditorGUILayout.LabelField("", GUILayout.Width(152));

                    // Iterate through the properties of the first ScriptableObject to display their names
                    SerializedObject serializedObject = new(Configs[0]);
                    SerializedProperty property = serializedObject.GetIterator();

                    bool ShouldNext = property.NextVisible(true);
                    while (ShouldNext)
                    {
                        if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // Exclude internal Unity fields and arrays
                        {
                            // Show the name of each property as a label
                            EditorGUILayout.LabelField(property.name, GUIL_StandartOptions);
                        }
                        ShouldNext = property.NextVisible(false); // Move to the next property, skipping nested children
                    }
                }
                catch
                {
                    // If an error occurs, refresh the type list and regroup the objects
                    LoadAvailableTypes();
                    GroupScriptableObjectsByType();
                }
                EditorGUILayout.EndHorizontal();

                // Exit if there are no configurations to display
                if (Configs.Count == 0)
                    return;

                // Iterate through each ScriptableObject in the list
                foreach (var Config in Configs)
                {
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        // Retrieve and display the asset's file name without its extension
                        string filePath = AssetDatabase.GetAssetPath(Config);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        if (fileName == "") throw new System.Exception();

                        // Display the file name for the asset
                        GUIContent propertyContent = new GUIContent(fileName, fileName);
                        
                        
                        Rect elementRect = GUILayoutUtility.GetRect(120, 18, GUILayout.Width(120));
                        
                        if (isRenaming && ObjectToRename != null && ObjectToRename == Config)
                        {
                            GUI.SetNextControlName("RenameField");

                            // Display a text field for renaming the asset.if renameText is empty, show the file name until the user types something
                            renameText = EditorGUI.TextField(elementRect, renameText == "" ? fileName : renameText);

                            Event e = Event.current;

                            // textfield + confirm button (for mouse event it must also contain the button area)
                            elementRect.width = 145;
                            if (isRenaming && e.type == EventType.MouseDown && !elementRect.Contains(e.mousePosition))
                            {
                                isRenaming = false;
                                GUI.FocusControl(null);
                                e.Use();
                            }

                            if (FinishRenaming || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                            {
                                AssetDatabase.RenameAsset(filePath, renameText);
                                AssetDatabase.SaveAssets();
                                isRenaming = false;
                                FinishRenaming = false;
                                renameText = "";
                                ObjectToRename = null;
                                GUI.FocusControl(null);
                            }
                            else if (Event.current.keyCode == KeyCode.Escape)
                            {
                                renameText = "";
                                isRenaming = false;
                                ObjectToRename = null;
                                GUI.FocusControl(null);
                            }

                            EditorGUI.FocusTextInControl("RenameField");
                        }
                        else
                        {
                            EditorGUI.LabelField(elementRect, propertyContent,  EditorStyles.miniBoldLabel);
                        }
                        
                        // Display a delete button for the asset
                        OptionsButton(Config);
                        GUILayout.Space(3);

                        SerializedObject serializedObject = new(Config);
                        SerializedProperty property = serializedObject.GetIterator();

                        // Iterate through all properties to create editable fields based on their types
                        bool ShouldNext = property.NextVisible(true);
                        while (ShouldNext)
                        {
                            if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // Skip unwanted properties
                            {
                                EditorGUILayout.PropertyField(property, GUIContent.none, true, GUIL_DefaultOptions);
                                serializedObject.ApplyModifiedProperties();
                            }
                            ShouldNext = property.NextVisible(false); // Advance to the next property, excluding nested fields
                        }
                        property.Reset();
                    }
                    catch
                    {
                        // Handle errors by refreshing the type list and regrouping
                        LoadAvailableTypes();
                        GroupScriptableObjectsByType();
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(PropertySpace);
                }
            }
            catch
            {
                // Catch any top-level errors and reset the data
                LoadAvailableTypes();
                GroupScriptableObjectsByType();
            }
        }


        private void DeleteConfig<T>(T Config) where T : ScriptableObject
        {
            if (EditorUtility.DisplayDialog("Delete Config", "Are you sure you want to delete this config? \n\nYou cannot undo delete assets action.", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Config));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                GroupScriptableObjectsByType(); // Refresh the list after deletion
            }
        }

        private void ShowInProjectFolders<T>(T Config) where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Config);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        }

        public static float CalculatePropertyHeight<T>(List<T> configs, SerializedProperty property) where T : ScriptableObject
        {
            float maxHeight = 0f;

            foreach (var config in configs)
            {
                SerializedObject serializedObject = new(config);
                SerializedProperty targetProperty = serializedObject.FindProperty(property.propertyPath);

                if (targetProperty != null)
                {
                    float height = EditorGUI.GetPropertyHeight(targetProperty, true);
                    maxHeight = Mathf.Max(maxHeight, height);
                }
            }

            return maxHeight;
        }

        void OnLostFocus()
        {
            isRenaming = false;
            ObjectToRename = null;
            renameText = "";
            GUI.FocusControl(null);
        }

    }
}