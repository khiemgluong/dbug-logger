#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class DBugLoggerToolbar : EditorWindow
{
    [MenuItem("Tools/DBug Logger")]
    public static void ShowWindow()
    {
        DBugLoggerToolbar window = GetWindow<DBugLoggerToolbar>("DBug Logger");

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

    private const string PREFS_KEY = "DBugLogger_Channels";

    [SerializeField]
    Channel loggerChannels = new(ulong.MaxValue);

    Dictionary<string, bool> foldouts = new();
    private Vector2 scrollPos;

    private void OnEnable()
    {
        foreach (var nested in typeof(Channel).GetNestedTypes())
        {
            string foldoutKey = "DBugLogger_Foldout_" + nested.Name;
            foldouts[nested.Name] = EditorPrefs.GetBool(foldoutKey, true);
        }

        if (EditorPrefs.HasKey(PREFS_KEY))
        {
            string storedValue = EditorPrefs.GetString(PREFS_KEY);
            if (ulong.TryParse(storedValue, out ulong val))
            {
                loggerChannels = new Channel(val);
            }
        }

        DBug.SetChannels(loggerChannels);
    }

    private void OnGUI()
    {
        Channel currentChannels = loggerChannels;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select all"))
            currentChannels = DBug.kAllChannels;
        if (GUILayout.Button("Clear all"))
            currentChannels = new Channel(0);
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var nested in typeof(Channel).GetNestedTypes().OrderBy(t => t.Name))
        {
            DrawCategory(nested, ref currentChannels);
        }

        EditorGUILayout.EndScrollView();

        if (EditorGUI.EndChangeCheck())
        {
            loggerChannels = currentChannels;
            EditorPrefs.SetString(PREFS_KEY, loggerChannels.Value.ToString());
            DBug.SetChannels(currentChannels);
        }
    }

    private void DrawCategory(System.Type categoryType, ref Channel currentChannels)
    {
        string name = categoryType.Name;
        if (!foldouts.ContainsKey(name)) foldouts[name] = true;

        EditorGUI.BeginChangeCheck();
        bool foldout = EditorGUILayout.Foldout(foldouts[name], name, true);
        if (EditorGUI.EndChangeCheck())
        {
            foldouts[name] = foldout;
            EditorPrefs.SetBool("DBugLogger_Foldout_" + name, foldout);
        }

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