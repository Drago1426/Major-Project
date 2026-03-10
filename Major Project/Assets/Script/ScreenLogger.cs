using UnityEngine;
using System.Collections.Generic;

public class ScreenLogger : MonoBehaviour
{
    static List<string> logs = new List<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string log, string stack, LogType type)
    {
        logs.Add(log);
        if (logs.Count > 10) logs.RemoveAt(0);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, Screen.width, Screen.height),
            string.Join("\n", logs));
    }
}
