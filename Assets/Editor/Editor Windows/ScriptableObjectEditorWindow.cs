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
        private Vector2 ScrollPosMain = new();
        private bool OrientationVertical = true; // Determines the orientation of the layout (true: vertical. false: horizontal)
                                                 // data:
        private static List<List<ScriptableObject>> groupedConfigs; // Stores ScriptableObjects grouped by their class types in nested lists
        private static List<Type> selectedTypes = new(); // Tracks which ScriptableObject types are currently selected for display
        private static List<Type> availableTypes = new(); // Holds all unique ScriptableObject types found in the project

        // classes:
        SOManagerTableGUI TableGUI; // Instance of the SOManagerTableGUI class for displaying ScriptableObject properties in a table format
        SOManagerResources ManagerResources; // Instance of the SOManagerResources class for loading icons and resources

        // Buttons Styles:
        GUIContent spaceButton;
        GUIContent orientationButton;
        GUIContent refreshButton;
        GUIContent filtersButton;
        
        // create config button styles:
        GUIContent AddConfigButton;
        GUILayoutOption[] AddConfigButtonOptions;
        GUIStyle buttonStyle;
        

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
            // Load the saved table orientation from EditorPrefs
            OrientationVertical = EditorPrefs.GetBool(TABLE_ORIENTATION_PREF, true);

            // Initialize the editor by discovering available types and organizing ScriptableObjects accordingly
            LoadAvailableTypes();
            GroupScriptableObjectsByType();

            // setup resources for icons:
            ManagerResources = new SOManagerResources();
            ManagerResources.LoadIcons();

            // setup styles 
            SetupButtonStyles();

            // setup the table GUI
            TableGUI = new SOManagerTableGUI();
        }

        /// <summary>
        /// Sets up the GUI buttons with their respective icons and tooltips.
        /// </summary>
        private void SetupButtonStyles()
        {
            // GUI content for space button
            if (SOManagerResources.spaceIcon != null)
            {
                spaceButton = new GUIContent(SOManagerResources.spaceIcon, "change space between parameters");
            }
            else
            {
                spaceButton = new GUIContent("space", "change space between parameters");
            }

            // GUI content for orientation button
            if (SOManagerResources.orientationIcon != null)
            {
                orientationButton = new GUIContent(SOManagerResources.orientationIcon, "change the table orientation");
            }
            else
            {
                orientationButton = new GUIContent("rotate", "change the table orientation");
            }

            // GUI content for refresh button
            if (SOManagerResources.refreshIcon != null)
            {
                refreshButton = new GUIContent(SOManagerResources.refreshIcon, "refresh");
            }
            else
            {
                refreshButton = new GUIContent("refresh", "refresh");
            }

            // GUI content for filters button
            if (SOManagerResources.filtersIcon != null)
            {
                filtersButton = new GUIContent(SOManagerResources.filtersIcon, "filters");
            }
            else
            {
                filtersButton = new GUIContent("filters", "filters");
            }

            // 'create config' button styles
            if (SOManagerResources.addConfigIcon != null)
            {
                AddConfigButton = new GUIContent(SOManagerResources.addConfigIcon, "create new config");
                AddConfigButtonOptions = new GUILayoutOption[] { GUILayout.Height(20), GUILayout.Width(20) };
            }
            else
            {
                AddConfigButton = new GUIContent("Add new", "create new config");
                AddConfigButtonOptions = new GUILayoutOption[] { GUILayout.Width(65) };
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // A button to set space between properties
            if (GUILayout.Button(spaceButton, GUILayout.Width(50)))
            {
                TableGUI.SetSpace();
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
                    if (SOManagerResources.addConfigIcon != null)
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
                        TableGUI.PutPropertiesForObject_V(configGroup);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical("box");
                        TableGUI.PutPropertiesForObject_H(configGroup);
                        EditorGUILayout.EndVertical();
                    }
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

        

        public static void LoadAvailableTypes()
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

        public static void GroupScriptableObjectsByType()
        {
            // Fetch all ScriptableObjects from a designated folder and organize them into groups based on their class types
            var configs = Resources.LoadAll<ScriptableObject>("").ToList();

            groupedConfigs = configs
                .GroupBy(c => c.GetType())
                .Where(g => selectedTypes.Count == 0 || selectedTypes.Contains(g.Key)) // Only include groups matching the current type filter
                .Select(g => g.ToList()) // Convert each group into a list of ScriptableObjects
                .ToList();
        }

        

        

        void OnLostFocus()
        {
            TableGUI.OnTableFocusLost();
        }

    }
}