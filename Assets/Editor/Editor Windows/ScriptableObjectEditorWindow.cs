using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class ScriptableObjectEditorWindow : EditorWindow
{

    private float PropertyMinWidth = 40;
    private Vector2 ScrollPos = new Vector2();

    private List<List<ScriptableObject>> groupedConfigs; // Her sýnýf türü için iç içe liste
    private List<Type> selectedTypes = new List<Type>(); // Seçilen ScriptableObject tipleri
    private List<Type> availableTypes = new List<Type>(); // Kullanýlabilir ScriptableObject tipleri

    [MenuItem("Window/game Config Editor")]
    public static void ShowWindow()
    {
        GetWindow<ScriptableObjectEditorWindow>("game Config Editor");
    }
    private void OnEnable()
    {
        LoadAvailableTypes(); // Kullanýlabilir ScriptableObject tiplerini yükle
        GroupScriptableObjectsByType(); // ScriptableObject'leri tiplerine göre grupla
    }

    private void OnGUI()
    {
        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
        // Baþlýk kýsmý ve ayarlar menüsü
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Game Configuration Editor", EditorStyles.boldLabel);

        // Refresh butonu
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            LoadAvailableTypes();
            GroupScriptableObjectsByType();
        }

        // Config tipi seçim menüsü
        if (GUILayout.Button("Filter", GUILayout.Width(80)))
        {
            Vector2 mousePosition = Event.current.mousePosition;
            // Toggle seçeneklerini göstermek için özel Popup aç
            PopupWindow.Show(new Rect(mousePosition.x, mousePosition.y + 20, 0, 0), new ConfigTypeSelectionPopup(selectedTypes, GroupScriptableObjectsByType, availableTypes));
        }
        EditorGUILayout.EndHorizontal();

        // Seçilen tipleri göster
        GUILayout.Label("Selected Config Types: " + (selectedTypes.Count > 0 ? string.Join(", ", selectedTypes.Select(t => t.Name)) : "None"), EditorStyles.miniBoldLabel);

        if (groupedConfigs == null || groupedConfigs.Count == 0)
        {
            GUILayout.Label("No Configs Loaded", EditorStyles.boldLabel);
        }

        if (groupedConfigs.Count != 0)
        {

            foreach (var configGroup in groupedConfigs)
            {
                // Eðer grup boþsa veya seçilen tiplerle eþleþmiyorsa devam et
                if (selectedTypes.Count == 0 || !selectedTypes.Contains(configGroup[0].GetType()))
                    continue;

                GUILayout.Label(configGroup[0].GetType().Name, EditorStyles.boldLabel); // Grup baþlýðý olarak tip ismi göster

                EditorGUILayout.BeginHorizontal("box");

                PutPropertiesForObject(configGroup);

                EditorGUILayout.EndHorizontal();

            }
        }
        //scrollView sonu
        EditorGUILayout.EndScrollView();
    }

    private void LoadAvailableTypes()
    {
        availableTypes = Resources.LoadAll<ScriptableObject>("ScriptableObjects").Select(t => t.GetType()).Distinct().ToList();
        selectedTypes.Clear();
        selectedTypes.AddRange(availableTypes);
    }

    private void GroupScriptableObjectsByType()
    {
        // ScriptableObject'leri belirli bir klasörden yükle (örneðin Resources) ve sýnýf türlerine göre gruplandýr
        var configs = Resources.LoadAll<ScriptableObject>("").ToList();

        groupedConfigs = configs
            .GroupBy(c => c.GetType())
            .Where(g => selectedTypes.Count == 0 || selectedTypes.Contains(g.Key)) // Seçilen tipleri filtrele
            .Select(g => g.ToList()) // Her tip için bir alt liste oluþtur
            .ToList();
    }

    private void PutPropertiesForObject<T>(List<T> Configs) where T : ScriptableObject
    {
        try
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("", GUILayout.MinWidth(PropertyMinWidth));// dosya isimleri satýrý için burasý boþ olmalý

                // SerializedObject ve SerializedProperty kullan
                SerializedObject serializedObject = new SerializedObject(Configs[0]);
                SerializedProperty property = serializedObject.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // m_Script'i atla
                    {
                        // Deðiþken ismini yazdýr
                        EditorGUILayout.LabelField(property.name, GUILayout.MinWidth(PropertyMinWidth));
                    }
                }
            }
            catch
            {
                LoadAvailableTypes();
                GroupScriptableObjectsByType();
            }
            EditorGUILayout.EndVertical();

            foreach (var Config in Configs)
            {
                EditorGUILayout.BeginVertical("box");
                try
                {
                    // Dosya adýný al
                    string filePath = AssetDatabase.GetAssetPath(Config);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    if (fileName == "") throw new System.Exception();
                    // Dosya adýný yazdýr
                    EditorGUILayout.LabelField(fileName, EditorStyles.miniBoldLabel, GUILayout.MinWidth(PropertyMinWidth));

                    SerializedObject serializedObject = new SerializedObject(Config);
                    SerializedProperty property = serializedObject.GetIterator();

                    // Tüm deðiþken isimlerini yazdýr ve uygun alanlarý oluþtur
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ArraySize && property.name != "m_Script" && property.name != "data") // m_Script'i atla
                        {
                            // Deðiþkenin türüne göre uygun alaný oluþtur
                            switch (property.propertyType)
                            {
                                case SerializedPropertyType.String:
                                    // Deðiþken adý ile ilgili bir kopya oluþtur
                                    string oldStringValue = property.stringValue;
                                    property.stringValue = EditorGUILayout.TextField(oldStringValue);
                                    break;
                                case SerializedPropertyType.Integer:
                                    int oldIntValue = property.intValue;
                                    property.intValue = EditorGUILayout.IntField(oldIntValue);
                                    break;
                                case SerializedPropertyType.Float:
                                    float oldFloatValue = property.floatValue;
                                    property.floatValue = EditorGUILayout.FloatField(oldFloatValue);
                                    break;
                                case SerializedPropertyType.Boolean:
                                    bool oldBoolValue = property.boolValue;
                                    property.boolValue = EditorGUILayout.Toggle(oldBoolValue);
                                    break;
                                case SerializedPropertyType.Enum:
                                    property.intValue = EditorGUILayout.Popup(property.enumValueIndex, property.enumNames);
                                    break;
                                default:
                                    EditorGUILayout.PropertyField(property, true);
                                    break;

                                    // Diðer türler için gerekli alanlarý buraya ekleyebilirsin
                            }
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                    property.Reset();
                    // Deðiþiklikleri kaydet

                }
                catch
                {
                    LoadAvailableTypes();
                    GroupScriptableObjectsByType();
                }
                EditorGUILayout.EndVertical();
            }
        }
        catch //(System.Exception e) 
        {
            LoadAvailableTypes();
            GroupScriptableObjectsByType();
        }
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
        return new Vector2(200, 500); // Popup boyutu
    }

    public override void OnGUI(Rect rect)
    {
        GUILayout.Label("Select Config Types", EditorStyles.boldLabel);

        if (availableTypes.Count > 0)
        {

            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

            // Her tipi bir Toggle ile gösteriyoruz
            foreach (var type in availableTypes)
            {
                bool isSelected = selectedTypes.Contains(type);
                bool toggle = EditorGUILayout.Toggle(type.Name, isSelected);

                if (toggle && !isSelected)
                {
                    selectedTypes.Add(type); // Tip seçildi
                    onSelectionChanged.Invoke();
                }
                else if (!toggle && isSelected)
                {
                    selectedTypes.Remove(type); // Tip çýkarýldý
                    onSelectionChanged.Invoke();
                }
            }

            // ScrollView sonu
            EditorGUILayout.EndScrollView();
        }
    }
}

