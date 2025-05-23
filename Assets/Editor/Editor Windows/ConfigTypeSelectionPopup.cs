using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectManager
{

    // Custom Popup Window for Config Type Selection
    internal class ConfigTypeSelectionPopup : PopupWindowContent
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
            return new Vector2(200, 400); // Define the dimensions of the popup window
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

}
