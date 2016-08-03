using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour {
	public static SceneManager instance;

	void Awake() 
	{
		instance = this;
	}

	public List<Vessel> allVesselsInScene = new List<Vessel>();

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void RegisterVessel(Vessel v)
	{
		v.transform.parent = this.transform;
		allVesselsInScene.Add (v);
		BattleManager.instance.RegisterVessel (v);
	}

	public void UnregisterVessel(Vessel v)
	{
		allVesselsInScene.Remove (v);
		BattleManager.instance.UnregisterVessel (v);
	}

	string[] vesselPrefabs = new string[] 
	{
		"Prefabs/Ships/BattleShips/Sukhbaatar",
		"Prefabs/Ships/BattleShips/Bismarck", 
		"Prefabs/Ships/BattleShips/Tirpitz",
		"Prefabs/Ships/BattleShips/Iowa",
		"Prefabs/Ships/Carriers/Lexington",
		"Prefabs/Ships/Carriers/Saratoga",
		"Prefabs/Ships/Carriers/Essex",
		"Prefabs/Ships/Carriers/Zeppelin",
		"Prefabs/Ships/Carriers/Peter Strasser",
		"Prefabs/Ships/Destroyers/Le Fantasque",
		"Prefabs/Ships/HeavyCruisers/Kumano",
		"Prefabs/Ships/HeavyCruisers/Mogami",
		"Prefabs/Ships/HeavyCruisers/Quincy",
	};

	string GetVesselName(string prefabPath) 
	{
		return Resources.Load<Vessel>(prefabPath).name;
	}

	void SpawnVessels(List<string> sideA, List<string> sideB)
	{
		foreach (string vp in sideA)
		{
			GameObject vo = Resources.Load<GameObject>(vp);
			GameObject instance = (GameObject)Instantiate(vo);
			instance.name = instance.name.Replace("(Clone)", "");
			instance.GetComponent<Vessel>().side = 0;
			instance.transform.parent = this.transform;
		}
		foreach (string vp in sideB)
		{
			GameObject vo = Resources.Load<GameObject>(vp);
			GameObject instance = (GameObject)Instantiate(vo);
			instance.name = instance.name.Replace("(Clone)", ""); 
			instance.GetComponent<Vessel>().side = 1;
			instance.transform.parent = this.transform;
		}
	}

	List<string> sideA = new List<string>();
	List<string> sideB = new List<string>();
	bool battleStarted = false;
	void OnGUI()
	{
		GUIStyle centerStyle = GUI.skin.GetStyle("Label");
		centerStyle.alignment = TextAnchor.MiddleCenter;

		if (battleStarted == true) return;

		GUILayout.BeginArea(new Rect((Screen.width - 600f) * 0.5f, 0f, 600f, Screen.height));
		GUILayout.BeginVertical(GUILayout.Width(600f));
		{
			GUILayout.BeginHorizontal(GUILayout.Width(600f));
			{
				GUILayout.BeginVertical(GUILayout.Width(200f));
				{
					GUILayout.Label("Side A", centerStyle, GUILayout.ExpandWidth(true));
					for (int i = 0; i < sideA.Count; ++i)
					{
						bool remove = GUILayout.Button(GetVesselName(sideA[i]), GUILayout.Width(200f));
						if (remove)
						{
							sideA.RemoveAt(i);
							i--;
						}
					}
				}
				GUILayout.EndVertical();

				GUILayout.BeginVertical(GUILayout.Width(200f));
				{
					GUILayout.Label("Available Vessels", centerStyle, GUILayout.ExpandWidth(true));
					foreach (string vesselPrefab in vesselPrefabs)
					{
						GUILayout.BeginHorizontal();
						{
							bool addToSideA = GUILayout.Button("<", GUILayout.Width(50f));
							GUILayout.Label(GetVesselName(vesselPrefab), centerStyle, GUILayout.Width(100f));
							bool addToSideB = GUILayout.Button(">", GUILayout.Width(50f));

							if (addToSideA)
								sideA.Add(vesselPrefab);
							if (addToSideB)
								sideB.Add(vesselPrefab);
						}
						GUILayout.EndHorizontal();
					}

					bool battleStart = GUILayout.Button("Start Battle!", GUILayout.ExpandWidth(true));
					if (battleStart)
					{
						battleStarted = true;
						SpawnVessels(sideA, sideB);
						BattleManager.instance.Invoke("StartBattle", 1f);
					}
				}
				GUILayout.EndVertical();

				GUILayout.BeginVertical(GUILayout.Width(200f));
				{
					GUILayout.Label("Side B", centerStyle, GUILayout.ExpandWidth(true));
					for (int i = 0; i < sideB.Count; ++i)
					{
						bool remove = GUILayout.Button(GetVesselName(sideB[i]), GUILayout.Width(200f));
						if (remove)
						{
							sideB.RemoveAt(i);
							i--;
						}
					}
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

}
