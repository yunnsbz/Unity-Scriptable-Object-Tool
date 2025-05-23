using System;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectManager
{
    /// <summary>
    /// Handles the editor preferences for the Scriptable Object Manager.
    /// </summary>
    internal class SOManagerPrefs : MonoBehaviour
    {
        public const string BASIC_FILTERS_PREF = "basic_filters";
        private const string PROPERTY_SPACE_PREF = "property_space";
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

        /// <summary>
        /// Saves the table orientation preference. Default to true (vertical) if not set
        /// </summary>
        /// <returns>true for vertical, false for horizontal</returns>
        internal static bool GetOrientation() 
        {
            return EditorPrefs.GetBool(TABLE_ORIENTATION_PREF, true);
        }
        internal static void SetOrientation(bool isVertical)
        {
            EditorPrefs.SetBool(TABLE_ORIENTATION_PREF, isVertical); 
        }

        /// <summary>
        /// Default to -2 if not set
        /// </summary>
        internal static float GetPropertySpace()
        {
            return EditorPrefs.GetFloat(PROPERTY_SPACE_PREF, -2f); 
        }
        internal static void SetPropertySpace(float space)
        {
            EditorPrefs.SetFloat(PROPERTY_SPACE_PREF, space);
        }
    }
}
