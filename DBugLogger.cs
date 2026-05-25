using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

#if UNITY_EDITOR && UNITY_DIALOGS
using UnityEditor;
#endif

public enum Priority
{
    Info,
    Warning,
    Error,
    FatalError,
}

public class DBug
{
    public static readonly Channel kAllChannels = new Channel(0xFFFFFFFF);

    static DBug instance;
    static DBug Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new DBug();
                Initialize();
            }
            return instance;
        }
    }

    DBug()
    {
        m_Channels = kAllChannels;
    }

    Channel m_Channels;

    public delegate void OnLogFunc(Channel channel, Priority priority, string message);
    public static event OnLogFunc OnLog;

    public static void ResetChannels() => Instance.m_Channels = kAllChannels;
    public static void AddChannel(Channel channelToAdd) => Instance.m_Channels |= channelToAdd;
    public static void RemoveChannel(Channel channelToRemove) => Instance.m_Channels &= ~channelToRemove;
    public static void ToggleChannel(Channel channelToToggle) => Instance.m_Channels ^= channelToToggle;
    public static bool IsChannelActive(Channel channelToCheck)
    {
        Initialize();
        return (Instance.m_Channels & channelToCheck).Value != 0;
    }
    public static void SetChannels(Channel channelsToSet) => Instance.m_Channels = channelsToSet;

    [Conditional("UNITY_EDITOR")]
    public static void Log(string message, Object context = null) => UnityEngine.Debug.Log(message, context);

    [Conditional("UNITY_EDITOR")]
    public static void Info(Channel logChannel, string message, Object context = null)
#if UNITY_EDITOR
        => FinalLog(logChannel, Priority.Info, message, context);
#else
        {}
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Warning(Channel logChannel, string message, Object context = null)
#if UNITY_EDITOR
        => FinalLog(logChannel, Priority.Warning, message, context);
#else
        {}
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Error(Channel logChannel, string message, Object context = null)
#if UNITY_EDITOR
        => FinalLog(logChannel, Priority.Error, message, context);
#else
        {}
#endif

    [Conditional("UNITY_EDITOR")]
    public static void FatalError(Channel logChannel, string message, Object context = null)
#if UNITY_EDITOR
        => FinalLog(logChannel, Priority.FatalError, message, context);
#else
        {}
#endif

    [Conditional("UNITY_EDITOR")]
    public static void Assert(bool condition, string message, Object context = null)
    {
#if UNITY_EDITOR
        if (!condition)
        {
            FinalLog(Channel.System.Console, Priority.FatalError, string.Format("Assert Failed: {0}", message), context);
        }
#endif
    }

#if UNITY_EDITOR
    static void FinalLog(Channel logChannel, Priority priority, string message, Object context = null)
    {
        if (IsChannelActive(logChannel))
        {
            string finalMessage = ContructFinalString(logChannel, priority, message, (priority != Priority.FatalError));

#if UNITY_DIALOGS
            if (priority == Priority.FatalError)
            {
                bool ignore = EditorUtility.DisplayDialog("Fatal error", finalMessage, "Ignore", "Break");
                if (!ignore) UnityEngine.Debug.Break();
            }
#endif
            switch (priority)
            {
                case Priority.FatalError:
                case Priority.Error:
                    UnityEngine.Debug.LogError(finalMessage, context);
                    break;
                case Priority.Warning:
                    UnityEngine.Debug.LogWarning(finalMessage, context);
                    break;
                case Priority.Info:
                    UnityEngine.Debug.Log(finalMessage, context);
                    break;
            }

            OnLog?.Invoke(logChannel, priority, finalMessage);
        }
    }

    static string ContructFinalString(Channel logChannel, Priority priority, string message, bool shouldColour)
    {
        if (!channelToColour.TryGetValue(logChannel, out string channelColour))
            channelColour = "white";

        string priorityColour = priorityToColour[priority];
        string channelName = GetChannelName(logChannel);

        if (shouldColour)
            return string.Format("<color={0}>{1}: </color> <color={2}>{3}</color>",
                            channelColour, channelName, priorityColour, message);

        return string.Format("{0}: {1}", channelName, message);
    }
#endif

    public static string GetChannelName(Channel logChannel)
    {
#if UNITY_EDITOR
        Initialize();
        if (channelToName.TryGetValue(logChannel, out string name))
            return name;
#endif
        return "Unknown";
    }

#if UNITY_EDITOR
    static readonly Dictionary<Channel, string> channelToName = new();
    static readonly Dictionary<Channel, string> channelToColour = new();
#endif
    static bool m_Initialized = false;

#if UNITY_EDITOR
    static readonly Dictionary<string, string> categoryToColour = new()
    {
        { "Characters", "red" },
        { "Data", "cyan" },
        { "Environment", "green" },
        { "Editor", "orange" },
        { "System", "yellow" },
        { "UserInterface", "purple" },
    };
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (m_Initialized) return;
        m_Initialized = true;

#if UNITY_EDITOR
        var channelType = typeof(Channel);
        var categories = channelType.GetNestedTypes().OrderBy(t => t.Name).ToList();
        int bitIndex = 0;

        foreach (var category in categories)
        {
            var fields = category.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(f => f.Name).ToList();
            categoryToColour.TryGetValue(category.Name, out string color);
            if (string.IsNullOrEmpty(color)) color = "white";

            foreach (var field in fields)
            {
                if (field.FieldType != channelType) continue;

                Channel channel = new Channel(1u << bitIndex++);
                field.SetValue(null, channel);

                channelToName[channel] = $"{category.Name}.{field.Name}";
                channelToColour[channel] = color;
            }
        }
#endif
    }

    static readonly Dictionary<Priority, string> priorityToColour = new()
    {
        { Priority.Info,"white" },
        { Priority.Warning,"orange" },
        { Priority.Error,"red" },
        { Priority.FatalError,"maroon" },
    };
}