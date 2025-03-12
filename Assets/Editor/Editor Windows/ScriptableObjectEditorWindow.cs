using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class ScriptableObjectEditorWindow : EditorWindow
{
    // layout:
    private float PropertyMinWidth = 40;
    private float PropertySpace = 4;
    private Vector2 ScrollPos = new Vector2();
    private GUILayoutOption[] GUIL_StandartOptions = new GUILayoutOption[] { GUILayout.MinWidth(100), GUILayout.ExpandWidth(true) };
    private GUILayoutOption[] GUIL_DefaultOptions = new GUILayoutOption[] { GUILayout.MinWidth(150), GUILayout.ExpandWidth(true) };

    // data:
    private List<List<ScriptableObject>> groupedConfigs; // Stores ScriptableObjects grouped by their class types in nested lists
    private List<Type> selectedTypes = new List<Type>(); // Tracks which ScriptableObject types are currently selected for display
    private List<Type> availableTypes = new List<Type>(); // Holds all unique ScriptableObject types found in the project

    [MenuItem("Window/Game Config Editor")]
    public static void ShowWindow()
    {
        GetWindow<ScriptableObjectEditorWindow>("Game Config Editor");
    }

    private void OnEnable()
    {
        // Initialize the editor by discovering available types and organizing ScriptableObjects accordingly
        LoadAvailableTypes();
        GroupScriptableObjectsByType();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Game Configuration Editor", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            LoadAvailableTypes();
            GroupScriptableObjectsByType();
        }

        // Display a popup window at the mouse position for type selection
        if (GUILayout.Button("Filter", GUILayout.Width(80)))
        {
            Vector2 mousePosition = Event.current.mousePosition;
            PopupWindow.Show(new Rect(mousePosition.x, mousePosition.y + 20, 0, 0), new ConfigTypeSelectionPopup(selectedTypes, GroupScriptableObjectsByType, availableTypes));
        }
        EditorGUILayout.EndHorizontal();

        // Show a summary of currently selected types or indicate none are selected
        GUILayout.Label("Selected Config Types: " + (selectedTypes.Count > 0 ? string.Join(", ", selectedTypes.Select(t => t.Name)) : "None"), EditorStyles.miniBoldLabel);
        if (groupedConfigs == null || groupedConfigs.Count == 0)
        {
            GUILayout.Label("No Configs Loaded", EditorStyles.boldLabel);
        }

        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
        if (groupedConfigs.Count != 0)
        {
            foreach (var configGroup in groupedConfigs)
            {
                // Only display groups that match the selected types or if no specific types are filtered
                if (selectedTypes.Count == 0 || !selectedTypes.Contains(configGroup[0].GetType()))
                    continue;

                GUILayout.Label(configGroup[0].GetType().Name, EditorStyles.boldLabel); // Display the type name as a section header

                EditorGUILayout.BeginHorizontal("box");

                PutPropertiesForObject(configGroup);

                EditorGUILayout.EndHorizontal();
            }
        }
        // Close the scrollable area
        EditorGUILayout.EndScrollView();
    }

    private void LoadAvailableTypes()
    {
        // Scan the "ScriptableObjects" folder to find all unique ScriptableObject types
        availableTypes = Resources.LoadAll<ScriptableObject>("ScriptableObjects").Select(t => t.GetType()).Distinct().ToList();
        selectedTypes.Intersect(availableTypes);
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

    private void PutPropertiesForObject<T>(List<T> Configs) where T : ScriptableObject
    {
        try
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                // An empty label to align property names properly in the UI
                EditorGUILayout.LabelField("", GUILayout.MinWidth(PropertyMinWidth));

                // Iterate through the properties of the first ScriptableObject to display their names
                SerializedObject serializedObject = new SerializedObject(Configs[0]);
                SerializedProperty property = serializedObject.GetIterator();

                bool ShouldNext = property.NextVisible(true);
                while (ShouldNext)
                {
                    if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // Exclude internal Unity fields and arrays
                    {
                        // Show the name of each property as a label
                        EditorGUILayout.LabelField(property.name, GUILayout.MinWidth(PropertyMinWidth));

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

            foreach (var Config in Configs)
            {
                EditorGUILayout.BeginVertical("box");
                try
                {
                    // Retrieve and display the asset's file name without its extension
                    string filePath = AssetDatabase.GetAssetPath(Config);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    if (fileName == "") throw new System.Exception();
                    EditorGUILayout.LabelField(fileName, EditorStyles.miniBoldLabel, GUILayout.MinWidth(PropertyMinWidth));

                    SerializedObject serializedObject = new SerializedObject(Config);
                    SerializedProperty property = serializedObject.GetIterator();

                    // Iterate through all properties to create editable fields based on their types
                    bool ShouldNext = property.NextVisible(true);
                    while (ShouldNext)
                    {
                        if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // Skip unwanted properties
                        {
                            // Generate an appropriate UI control based on the property's data type
                            switch (property.propertyType)
                            {
                                case SerializedPropertyType.String:
                                    string oldStringValue = property.stringValue;
                                    property.stringValue = EditorGUILayout.TextField(oldStringValue, GUIL_StandartOptions);
                                    break;
                                case SerializedPropertyType.Integer:
                                    int oldIntValue = property.intValue;
                                    property.intValue = EditorGUILayout.IntField(oldIntValue, GUIL_StandartOptions);
                                    break;
                                case SerializedPropertyType.Float:
                                    float oldFloatValue = property.floatValue;
                                    property.floatValue = EditorGUILayout.FloatField(oldFloatValue, GUIL_StandartOptions);
                                    break;
                                case SerializedPropertyType.Boolean:
                                    bool oldBoolValue = property.boolValue;
                                    property.boolValue = EditorGUILayout.Toggle(oldBoolValue, GUIL_StandartOptions);
                                    break;
                                case SerializedPropertyType.Enum:
                                    property.intValue = EditorGUILayout.Popup(property.enumValueIndex, property.enumNames, GUIL_StandartOptions);
                                    break;
                                default:
                                    EditorGUILayout.PropertyField(property, GUIContent.none, true, GUIL_DefaultOptions);
                                    if (property.isExpanded)
                                    {
                                        float extraSpace = CalculatePropertyHeight(Configs, property);
                                        extraSpace -= EditorGUI.GetPropertyHeight(property);
                                        GUILayout.Space(extraSpace);
                                    }
                                    break;
                                    // Additional cases can be added here to support more property types
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

    public static float CalculatePropertyHeight<T>(List<T> configs, SerializedProperty property) where T : ScriptableObject
    {
        float maxHeight = 0f;

        foreach (var config in configs)
        {
            SerializedObject serializedObject = new SerializedObject(config);
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

// Custom Popup Window for Config Type Selection
public class ConfigTypeSelectionPopup : PopupWindowContent
{
    private List<Type> selectedTypes;
    private List<Type> availableTypes;
    private Action onSelectionChanged;
    private Vector2 ScrollPos = new Vector2();

    public ConfigTypeSelectionPopup(List<Type> selectedTypes, Action onSelectionChanged, List<Type> availableTypes)
    {
        this.selectedTypes = selectedTypes;
        this.onSelectionChanged = onSelectionChanged;
        this.availableTypes = availableTypes;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(200, 500); // Define the dimensions of the popup window
    }

    public override void OnGUI(Rect rect)
    {
        GUILayout.Label("Select Config Types", EditorStyles.boldLabel);

        if (availableTypes.Count > 0)
        {
            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

            // Display each available type with a toggle to enable or disable it
            foreach (var type in availableTypes)
            {
                bool isSelected = selectedTypes.Contains(type);
                bool toggle = EditorGUILayout.Toggle(type.Name, isSelected);

                if (toggle && !isSelected)
                {
                    selectedTypes.Add(type); // Add the type to the selection list
                    onSelectionChanged.Invoke();
                }
                else if (!toggle && isSelected)
                {
                    selectedTypes.Remove(type); // Remove the type from the selection list
                    onSelectionChanged.Invoke();
                }
            }

            // Close the scrollable area within the popup
            EditorGUILayout.EndScrollView();
        }
    }
}