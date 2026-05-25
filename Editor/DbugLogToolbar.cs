#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class DbugLogToolbar : EditorWindow
{
    [MenuItem("Tools/DbugLog Config")]
    public static void ShowWindow()
    {
        DbugLogToolbar window = GetWindow<DbugLogToolbar>("DbugLog Config");
        
        // Set fixed dimensions
        float windowWidth = 300f;
        float windowHeight = 540f; // Half of 1080

        // Calculate position: 1/3 from left, vertically centered
        // Using Screen.currentResolution as a reference for display size
        var resolution = Screen.currentResolution;
        float screenWidth = resolution.width > 0 ? resolution.width : 1920;
        float screenHeight = resolution.height > 0 ? resolution.height : 1080;

        float x = screenWidth / 3f;
        float y = (screenHeight - windowHeight) / 2f;

        window.position = new Rect(x, y, windowWidth, windowHeight);
    }

    [SerializeField]
    private Channel loggerChannels = new Channel(0xFFFFFFFF);

    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
    private Vector2 scrollPos;

    private void OnEnable()
    {
        foreach (var nested in typeof(Channel).GetNestedTypes())
        {
            foldouts[nested.Name] = true;
        }

        DBug.SetChannels(loggerChannels);
    }

    private void OnGUI()
    {
        Channel currentChannels = loggerChannels;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear all"))
            currentChannels = new Channel(0);
        if (GUILayout.Button("Select all"))
            currentChannels = DBug.kAllChannels;
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Click to toggle logging channels", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var nested in typeof(Channel).GetNestedTypes().OrderBy(t => t.Name))
        {
            DrawCategory(nested, ref currentChannels);
        }

        EditorGUILayout.EndScrollView();

        if (EditorGUI.EndChangeCheck())
{
            loggerChannels = currentChannels;
            if (EditorApplication.isPlaying)
            {
                DBug.SetChannels(currentChannels);
            }
        }
    }

    private void DrawCategory(System.Type categoryType, ref Channel currentChannels)
    {
        string name = categoryType.Name;
        if (!foldouts.ContainsKey(name)) foldouts[name] = true;

        foldouts[name] = EditorGUILayout.Foldout(foldouts[name], name, true);
        if (foldouts[name])
        {
            EditorGUI.indentLevel++;
            var fields = categoryType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(Channel)) continue;

                Channel channel = (Channel)field.GetValue(null);
                EditorGUILayout.BeginHorizontal();
                bool active = (currentChannels & channel).Value != 0;

                GUI.enabled = false;
                EditorGUILayout.Toggle(active, GUILayout.Width(20));
                GUI.enabled = true;

                GUILayout.Space(10);

                // Use the channel name to a String() rather than hardcode it
                string fullName = channel.ToString();
// Trim the enum channel values
                string label = fullName.Contains(".") ? fullName.Substring(fullName.LastIndexOf('.') + 1) : fullName;

                if (GUILayout.Button(label))
                {
                    if (active)
                        currentChannels &= ~channel;
                    else currentChannels |= channel;
                }

                // Add margin to the right so layout doesn't jump when scrollbar appears
                GUILayout.Space(5);

                EditorGUILayout.EndHorizontal();
}
            EditorGUI.indentLevel--;
        }
    }
}
#endif