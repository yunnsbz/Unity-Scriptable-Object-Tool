
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectManager
{
    /// <summary>
    /// Class to manage the GUI for displaying ScriptableObject properties in a table format.
    /// </summary>
    internal class SOManagerTableGUI
    {
        // layout values:
        private readonly float PropertyMinWidth = 40;
        private float PropertySpace = 5;
        private readonly GUILayoutOption[] GUIL_StandartOptions = new GUILayoutOption[] { GUILayout.MinWidth(100), GUILayout.ExpandWidth(true) };
        private readonly GUILayoutOption[] GUIL_DefaultOptions = new GUILayoutOption[] { GUILayout.MinWidth(150), GUILayout.ExpandWidth(true) };

        // rename operation:
        bool isRenaming = false;
        bool FinishRenaming = false;
        ScriptableObject ObjectToRename = null;
        string renameText = "";

        // config options button styles:
        GUIContent ConfigOptionsButton;
        GUILayoutOption[] ConfigOptionsButtonOptions;

        public SOManagerTableGUI()
        {
            // 'Config Options' button styles
            if (SOManagerResources.ConfigOptionsIcon != null)
            {
                ConfigOptionsButton = new GUIContent(SOManagerResources.ConfigOptionsIcon, "Options");
                ConfigOptionsButtonOptions = new GUILayoutOption[] { GUILayout.Height(20), GUILayout.Width(20) };
            }
            else
            {
                ConfigOptionsButton = new GUIContent("opt", "Options");
                ConfigOptionsButtonOptions = new GUILayoutOption[] { GUILayout.Width(30) };
            }

            // Load the saved property space from EditorPrefs
            float propSpace = SOManagerPrefs.GetPropertySpace();
            if (propSpace == -2)
            {
                // If no space is set, use the default value
                PropertySpace = -1;
                SOManagerPrefs.SetPropertySpace(PropertySpace);
            }
            else PropertySpace = propSpace;
        }

        /// <summary>
        /// create a vertival table for the properties of the object
        /// </summary>
        internal void PutPropertiesForObject_V<T>(List<T> Configs) where T : ScriptableObject
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
                    ScriptableObjectEditorWindow.LoadAvailableTypes();
                    ScriptableObjectEditorWindow.GroupScriptableObjectsByType();
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

                        LabelAndRenameFieldGUI(Config, filePath, fileName, propertyContent, elementRect);

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
                        ScriptableObjectEditorWindow.LoadAvailableTypes();
                        ScriptableObjectEditorWindow.GroupScriptableObjectsByType();
                    }
                    EditorGUILayout.EndVertical();
                }

            }
            catch
            {
                // Catch any top-level errors and reset the data
                ScriptableObjectEditorWindow.LoadAvailableTypes();
                ScriptableObjectEditorWindow.GroupScriptableObjectsByType();
            }
        }


        /// <summary>
        /// create a Horizontal (parameters will be horizontal) table for the properties of the object
        /// </summary>
        internal void PutPropertiesForObject_H<T>(List<T> Configs) where T : ScriptableObject
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
                    ScriptableObjectEditorWindow.LoadAvailableTypes();
                    ScriptableObjectEditorWindow.GroupScriptableObjectsByType();
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

                        LabelAndRenameFieldGUI(Config, filePath, fileName, propertyContent, elementRect);

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
                        ScriptableObjectEditorWindow.LoadAvailableTypes();
                        ScriptableObjectEditorWindow.GroupScriptableObjectsByType();
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(PropertySpace);
                }
            }
            catch
            {
                // Catch any top-level errors and reset the data
                ScriptableObjectEditorWindow.LoadAvailableTypes();
                ScriptableObjectEditorWindow.GroupScriptableObjectsByType();
            }
        }


        private void LabelAndRenameFieldGUI<T>(T Config, string filePath, string fileName, GUIContent propertyContent, Rect elementRect) where T : ScriptableObject
        {
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

                EditorGUI.FocusTextInControl("RenameField");
            }
            else
            {
                EditorGUI.LabelField(elementRect, propertyContent, EditorStyles.miniBoldLabel);
            }
        }

        private void ShowOptionsMenu<T>(T Config) where T : ScriptableObject
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Delete Config"), false, () => DeleteConfig(Config));
            menu.AddItem(new GUIContent("Rename Config"), false, () => { isRenaming = true; ObjectToRename = Config; renameText = ""; });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show In Project Window"), false, () => ShowInProjectFolders(Config));
            menu.ShowAsContext();
        }

        private void OptionsButton<T>(T Config) where T : ScriptableObject
        {

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

            if (SOManagerResources.ConfigOptionsIcon != null)
            {
                buttonStyle.padding = new RectOffset(2, 2, 2, 2);
                buttonStyle.margin = new RectOffset(0, 0, 0, 0);
                buttonStyle.imagePosition = ImagePosition.ImageOnly;
            }

            if (isRenaming && ObjectToRename != null && ObjectToRename == Config)
            {
                if (GUILayout.Button(SOManagerResources.checkIcon, buttonStyle, ConfigOptionsButtonOptions))
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

        private void DeleteConfig<T>(T Config) where T : ScriptableObject
        {
            if (EditorUtility.DisplayDialog("Delete Config", "Are you sure you want to delete this config? \n\nYou cannot undo delete assets action.", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Config));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ScriptableObjectEditorWindow.GroupScriptableObjectsByType(); // Refresh the list after deletion
            }
        }

        private void ShowInProjectFolders<T>(T Config) where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Config);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        }

        internal void SetSpace()
        {
            float propSpace = SOManagerPrefs.GetPropertySpace();
            if (propSpace == -2)
            {
                // If no space is set, use the default value
                PropertySpace = -1;
                SOManagerPrefs.SetPropertySpace(PropertySpace);
            }
            else
            {
                // If space is already set, increase it by 1
                PropertySpace += 1;

                // If the space exceeds 5, reset it to 0
                if (PropertySpace > 5) PropertySpace = -1;

                // Save the new space value
                SOManagerPrefs.SetPropertySpace(PropertySpace);

                Debug.Log("SO Manager: Property Space: " + PropertySpace);
            }
        }

        internal void OnTableFocusLost()
        {
            isRenaming = false;
            FinishRenaming = false;
            renameText = "";
            ObjectToRename = null;
            GUI.FocusControl(null);
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

    }
}
