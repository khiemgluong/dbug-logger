using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DebugLogToolbar : EditorWindow
{
    [MenuItem("Tools/DebugLog Config")]
    public static void ShowWindow()
    {
        GetWindow(typeof(DebugLogToolbar));
    }

    [SerializeField]
    private Channel loggerChannels = new Channel(0xFFFFFFFF);

    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    private void OnEnable()
    {
        // Initialize foldouts
        foldouts["Characters"] = true;
        foldouts["Environment"] = true;
        foldouts["System"] = true;
        foldouts["UserInterface"] = true;
        foldouts["Editor"] = true;

        DbugLog.SetChannels(loggerChannels);
    }

    private void OnGUI()
    {
        Channel currentChannels = loggerChannels;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear all"))
            currentChannels = new Channel(0);
        if (GUILayout.Button("Select all"))
            currentChannels = DbugLog.kAllChannels;
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Click to toggle logging channels", EditorStyles.boldLabel);

        DrawCategory("Characters", new (string, Channel)[] {
            ("Combat", Channel.Characters.Combat),
            ("Controls", Channel.Characters.Controls),
            ("Targeting", Channel.Characters.Targeting),
            ("NPC", Channel.Characters.NPC)
        }, ref currentChannels);

        DrawCategory("Environment", new (string, Channel)[] {
            ("Prop", Channel.Environment.Prop),
            ("Object", Channel.Environment.Object),
            ("ObjItems", Channel.Environment.ObjItems),
            ("Events", Channel.Environment.Events)
        }, ref currentChannels);

        DrawCategory("System", new (string, Channel)[] {
            ("Serializers", Channel.System.Serializers),
            ("Managers", Channel.System.Managers),
            ("Utilities", Channel.System.Utilities),
            ("Console", Channel.System.Console)
        }, ref currentChannels);

        DrawCategory("UserInterface", new (string, Channel)[] {
            ("Overlay", Channel.UserInterface.Overlay),
            ("Terminal", Channel.UserInterface.Terminal),
            ("Scenes", Channel.UserInterface.Scenes)
        }, ref currentChannels);

        DrawCategory("Editor", new (string, Channel)[] {
            ("Characters", Channel.Editor.Characters),
            ("Environment", Channel.Editor.Environment)
        }, ref currentChannels);

        if (EditorGUI.EndChangeCheck())
        {
            loggerChannels = currentChannels;
            if (EditorApplication.isPlaying)
            {
                DbugLog.SetChannels(currentChannels);
            }
        }
    }

    private void DrawCategory(string name, (string label, Channel channel)[] subcategories, ref Channel currentChannels)
    {
        foldouts[name] = EditorGUILayout.Foldout(foldouts[name], name, true);
        if (foldouts[name])
        {
            EditorGUI.indentLevel++;
            foreach (var sub in subcategories)
            {
                EditorGUILayout.BeginHorizontal();
                bool active = (currentChannels & sub.channel).Value != 0;

                GUI.enabled = false;
                EditorGUILayout.Toggle(active, GUILayout.Width(20));
                GUI.enabled = true;

                if (GUILayout.Button(sub.label))
                {
                    if (active)
                        currentChannels &= ~sub.channel;
                    else
                        currentChannels |= sub.channel;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
}
