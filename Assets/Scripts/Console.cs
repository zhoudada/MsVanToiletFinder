using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A console to display Unity's debug logs in-game.
/// </summary>
public class Console : MonoBehaviour
{
    [SerializeField]
    private Text uiText;

    struct Log
    {
        public string message;
        public string stackTrace;
        public LogType type;
    }

    List<string> messages = new List<string>();

    private int maxMessageNumber = 100;

    static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>()
    {
        { LogType.Assert, Color.white },
        { LogType.Error, Color.red },
        { LogType.Exception, Color.red },
        { LogType.Log, Color.white },
        { LogType.Warning, Color.yellow },
    };

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
    }

    void HandleLog(string message, string stackTrace, LogType type)
    {
        messages.Insert(0, message);
        int count = messages.Count;
        if (count > maxMessageNumber)
        {
            messages.RemoveAt(count - 1);
        }

        uiText.text = string.Join("\n", messages);
    }
}