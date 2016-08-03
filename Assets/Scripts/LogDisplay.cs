using System;
using System.Collections.Generic;
using UnityEngine;

public class LogDisplay : MonoBehaviour
{
	private List<string> logs = new List<string>();
	private Vector2 _scrollPos;

	void Start()
	{
		Application.RegisterLogCallback(LogHandler);
	}
	
	private void LogHandler(string condition, string stackTrace, LogType logType)
	{
		logs.Add(condition);
	}
	
	void OnGUI()
	{
		GUIStyle leftStyle = GUI.skin.GetStyle("Label");
		leftStyle.alignment = TextAnchor.MiddleLeft;
		_scrollPos = GUI.BeginScrollView(new Rect(0,0, Screen.width, Screen.height), _scrollPos, new Rect(0, 0, Screen.width - 20, logs.Count * 20));
		for(int i = 0; i < logs.Count; ++i)
			GUI.Label(new Rect(0, i * 20, Screen.width - 20, 20), logs[i], leftStyle);
		GUI.EndScrollView();
	}
}

