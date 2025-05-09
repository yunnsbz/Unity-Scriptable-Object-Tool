using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class ScriptableObjectEditorWindow : EditorWindow
{
    public const string BASIC_FILTERS_PREF = "basic_filters";
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
    private readonly float PropertySpace = 5;
    private Vector2 ScrollPosMain = new();
    private readonly GUILayoutOption[] GUIL_StandartOptions = new GUILayoutOption[] { GUILayout.MinWidth(100), GUILayout.ExpandWidth(true) };
    private readonly GUILayoutOption[] GUIL_DefaultOptions = new GUILayoutOption[] { GUILayout.MinWidth(150), GUILayout.ExpandWidth(true) };

    // data:
    private List<List<ScriptableObject>> groupedConfigs; // Stores ScriptableObjects grouped by their class types in nested lists
    private List<Type> selectedTypes = new(); // Tracks which ScriptableObject types are currently selected for display
    private List<Type> availableTypes = new(); // Holds all unique ScriptableObject types found in the project

    // window:
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
            RefreshAll();
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
                GUILayout.Label(configGroup[0].GetType().Name, EditorStyles.boldLabel); // Display the type name as a section header

                if(GUILayout.Button("Add New", GUILayout.Width(80)))
                {
                    AddNewSO(configGroup[0].GetType());
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal("box");

                PutPropertiesForObject(configGroup);

                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
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
        
        if (selectedTypes.Count == 0)
        {
            Debug.LogWarning("no saved filter found for Game Config Editor.");
            selectedTypes.AddRange(availableTypes);
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

    private void PutPropertiesForObject<T>(List<T> Configs) where T : ScriptableObject
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

            if(Configs.Count == 0)
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
                    EditorGUILayout.LabelField(fileName, EditorStyles.miniBoldLabel, GUILayout.MinWidth(PropertyMinWidth));
                    if(GUILayout.Button("del", GUILayout.MaxWidth(30)))
                    {
                        // Delete the selected ScriptableObject asset
                        DeleteConfig(Config);
                    }
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

// Custom Popup Window for Config Type Selection
public class ConfigTypeSelectionPopup : PopupWindowContent
{
    private readonly List<Type> SelectedTypes;
    private readonly List<Type> AvailableTypes;
    private readonly Action OnSelectionChanged;
    private Vector2 ScrollPos = new();

    public ConfigTypeSelectionPopup(List<Type> selectedTypes, Action onSelectionChanged, List<Type> availableTypes)
    {
        this.SelectedTypes = selectedTypes;
        this.OnSelectionChanged = onSelectionChanged;
        this.AvailableTypes = availableTypes;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(200, 500); // Define the dimensions of the popup window
    }

    public override void OnGUI(Rect rect)
    {
        GUILayout.Label("Select Config Types", EditorStyles.boldLabel);

        if (AvailableTypes.Count > 0)
        {
            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

            // Display each available type with a toggle to enable or disable it
            foreach (var type in AvailableTypes)
            {
                bool isSelected = SelectedTypes.Contains(type);
                bool toggle = EditorGUILayout.Toggle(type.Name, isSelected);

                if (toggle && !isSelected)
                {
                    SelectedTypes.Add(type); // Add the type to the selection list
                    OnSelectionChanged.Invoke();
                    ScriptableObjectEditorWindow.BasicFilters = SelectedTypes.Select(t => t.Name).ToArray();
                }
                else if (!toggle && isSelected)
                {
                    SelectedTypes.Remove(type); // Remove the type from the selection list
                    OnSelectionChanged.Invoke();
                    ScriptableObjectEditorWindow.BasicFilters = SelectedTypes.Select(t => t.Name).ToArray();
                }
            }
            // Close the scrollable area within the popup
            EditorGUILayout.EndScrollView();
        }
    }
}