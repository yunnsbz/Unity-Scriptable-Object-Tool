using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectManager
{
    /// <summary>
    /// Class to manage the resources used in the SO Editor Window.
    /// </summary>
    public class SOManagerResources
    {
        // textures:
        internal static Texture2D spaceIcon;
        internal static Texture2D orientationIcon;
        internal static Texture2D ConfigOptionsIcon;
        internal static Texture2D addConfigIcon;
        internal static Texture2D refreshIcon;
        internal static Texture2D filtersIcon;
        internal static Texture2D checkIcon;

        internal void LoadIcons()
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
    }
}