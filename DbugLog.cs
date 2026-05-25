#define UNITY_DIALOGS // Comment out to disable dialogs for fatal errors
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR && UNITY_DIALOGS
using UnityEditor;
#endif

public enum Priority
{
    // Default, simple output about game
    Info,
    // Warnings that things might not be as expected
    Warning,
    // Things have already failed, alert the dev
    Error,
    // Things will not recover, bring up pop up dialog
    FatalError,
}

public class DbugLog
{
    public static readonly Channel kAllChannels = new Channel(0xFFFFFFFF);

    private static DbugLog instance;
    private static DbugLog Instance
    {
        get
        {
            return instance ?? (instance = new DbugLog());
        }

    }

    private DbugLog()
    {
        m_Channels = kAllChannels;
    }

    ///////////////////////////
    // Members
    ///////////////////////////
    private Channel m_Channels;

    public delegate void OnLogFunc(Channel channel, Priority priority, string message);
    public static event OnLogFunc OnLog;

    ///////////////////////////
    // Channel Control
    ///////////////////////////

    public static void ResetChannels()
    {
        Instance.m_Channels = kAllChannels;
    }

    public static void AddChannel(Channel channelToAdd)
    {
        Instance.m_Channels |= channelToAdd;
    }

    public static void RemoveChannel(Channel channelToRemove)
    {
        Instance.m_Channels &= ~channelToRemove;
    }

    public static void ToggleChannel(Channel channelToToggle)
    {
        Instance.m_Channels ^= channelToToggle;
    }

    public static bool IsChannelActive(Channel channelToCheck)
    {
        return (Instance.m_Channels & channelToCheck).Value != 0;
    }

    public static void SetChannels(Channel channelsToSet)
    {
        Instance.m_Channels = channelsToSet;
    }

    // Logging functions

    /// <summary>
    /// Standard logging function, priority will default to info level
    /// </summary>
    /// <param name="logChannel"></param>
    /// <param name="message"></param>
    public static void Info(Channel logChannel, string message)
    {
        FinalLog(logChannel, Priority.Info, message);
    }
    public static void Warning(Channel logChannel, string message)
    {
        FinalLog(logChannel, Priority.Warning, message);
    }
    public static void Error(Channel logChannel, string message)
    {
        FinalLog(logChannel, Priority.Error, message);
    }

    public static void FatalError(Channel logChannel, string message)
    {
        FinalLog(logChannel, Priority.FatalError, message);
    }

    /// <summary>
    /// Standard logging function with specified priority
    /// </summary>
    /// <param name="logChannel"></param>
    /// <param name="priority"></param>
    /// <param name="message"></param>
    public static void Log(Channel logChannel, Priority priority, string message)
    {
        FinalLog(logChannel, priority, message);
    }

    /// <summary>
    /// Log with format args, priority will default to info level
    /// </summary>
    /// <param name="logChannel"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public static void Log(Channel logChannel, string message, params object[] args)
    {
        FinalLog(logChannel, Priority.Info, string.Format(message, args));
    }

    /// <summary>
    /// Log with format args and specified priority
    /// </summary>
    /// <param name="logChannel"></param>
    /// <param name="priority"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public static void Log(Channel logChannel, Priority priority, string message, params object[] args)
    {
        FinalLog(logChannel, priority, string.Format(message, args));
    }

    /// <summary>
    /// Assert that the passed in condition is true, otherwise log a fatal error
    /// </summary>
    /// <param name="condition">The condition to test</param>
    /// <param name="message">A user provided message that will be logged</param>
    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            FinalLog(Channel.System.Console, Priority.FatalError, string.Format("Assert Failed: {0}", message));
        }
    }

    /// <summary>
    /// This function controls where the final string goes
    /// </summary>
    /// <param name="logChannel"></param>
    /// <param name="priority"></param>
    /// <param name="message"></param>
    private static void FinalLog(Channel logChannel, Priority priority, string message)
    {
        if (IsChannelActive(logChannel))
        {
            // Dialog boxes can't support rich text mark up, do we won't colour the final string 
            string finalMessage = ContructFinalString(logChannel, priority, message, (priority != Priority.FatalError));

#if UNITY_EDITOR && UNITY_DIALOGS
            // Fatal errors will create a pop up when in the editor
            if (priority == Priority.FatalError)
            {
                bool ignore = EditorUtility.DisplayDialog("Fatal error", finalMessage, "Ignore", "Break");
                if (!ignore)
                {
                    Debug.Break();
                }
            }
#endif 
            // Call the correct unity logging function depending on the type of error 
            switch (priority)
            {
                case Priority.FatalError:
                case Priority.Error:
                    Debug.LogError(finalMessage);
                    break;

                case Priority.Warning:
                    Debug.LogWarning(finalMessage);
                    break;

                case Priority.Info:
                    Debug.Log(finalMessage);
                    break;
            }

            if (OnLog != null)
            {
                OnLog.Invoke(logChannel, priority, finalMessage);
            }
        }
    }

    /// <summary>
    /// Creates the final string with colouration based on channel and priority 
    /// </summary>
    /// <param name="logChannel"></param>
    /// <param name="priority"></param>
    /// <param name="message"></param>
    /// <param name="shouldColour"></param>
    /// <returns></returns>
    private static string ContructFinalString(Channel logChannel, Priority priority, string message, bool shouldColour)
    {
        string channelColour = null;
        string priorityColour = priorityToColour[priority];

        if (!channelToColour.TryGetValue(logChannel, out channelColour))
        {
            channelColour = "white";
        }

        string channelName = GetChannelName(logChannel);

        if (shouldColour)
        {
            return string.Format("<b><color={0}>[{1}] </color></b> <color={2}>{3}</color>", channelColour, channelName, priorityColour, message);
        }

        return string.Format("[{0}] {1}", channelName, message);
    }

    private static string GetChannelName(Channel logChannel)
    {
        foreach (var entry in channelToName)
        {
            if (entry.Key.Value == logChannel.Value)
                return entry.Value;
        }
        return "Unknown";
    }

    private static readonly Dictionary<Channel, string> channelToName = new()
    {
        { Channel.Characters.Combat, "Characters:Combat" },
        { Channel.Characters.Controls, "Characters:Controls" },
        { Channel.Characters.Targeting, "Characters:Targeting" },
        { Channel.Characters.NPC, "Characters:NPC" },
        { Channel.Environment.Prop, "Environment:Prop" },
        { Channel.Environment.Object, "Environment:Object" },
        { Channel.Environment.ObjItems, "Environment:ObjItems" },
        { Channel.Environment.Events, "Environment:Events" },
        { Channel.System.Serializers, "System:Serializers" },
        { Channel.System.Managers, "System:Managers" },
        { Channel.System.Utilities, "System:Utilities" },
        { Channel.System.Console, "System:Console" },
        { Channel.UserInterface.Overlay, "UI:Overlay" },
        { Channel.UserInterface.Terminal, "UI:Terminal" },
        { Channel.UserInterface.Scenes, "UI:Scenes" },
        { Channel.Editor.Characters, "Editor:Characters" },
        { Channel.Editor.Environment, "Editor:Environment" },
    };

    /// <summary>
    /// Map a channel to a colour, using Unity's rich text system
    /// </summary>
    private static readonly Dictionary<Channel, string> channelToColour = new()
    {
        { Channel.Characters.Combat, "lightblue" },
        { Channel.Characters.Controls, "lightblue" },
        { Channel.Characters.Targeting, "lightblue" },
        { Channel.Characters.NPC, "lightblue" },
        { Channel.Environment.Prop, "green" },
        { Channel.Environment.Object, "green" },
        { Channel.Environment.ObjItems, "green" },
        { Channel.Environment.Events, "green" },
        { Channel.System.Serializers, "yellow" },
        { Channel.System.Managers, "yellow" },
        { Channel.System.Utilities, "yellow" },
        { Channel.System.Console, "yellow" },
        { Channel.UserInterface.Overlay, "purple" },
        { Channel.UserInterface.Terminal, "purple" },
        { Channel.UserInterface.Scenes, "purple" },
        { Channel.Editor.Characters, "grey" },
        { Channel.Editor.Environment, "grey" },
    };

    /// <summary>
    /// Map a priority to a colour, using Unity's rich text system
    /// </summary>
    private static readonly Dictionary<Priority, string> priorityToColour = new()
    {
        { Priority.Info,"white" },
        { Priority.Warning,"orange" },
        { Priority.Error,"red" },
        { Priority.FatalError,"maroon" },
    };
}
