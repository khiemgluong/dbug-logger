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
        GetWindow(typeof(DbugLogToolbar));
    }

    [SerializeField]
    private Channel loggerChannels = new Channel(0xFFFFFFFF);

    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

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

        foreach (var nested in typeof(Channel).GetNestedTypes().OrderBy(t => t.Name))
        {
            DrawCategory(nested, ref currentChannels);
        }

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
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif