using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{	public static BattleManager instance;
	
	void Awake() 
	{
		instance = this;
	}

	public float currentBattleTime;

	public List<List<Vessel>> sides = new List<List<Vessel>>();
	public List<List<Aircraft>> sidesAC = new List<List<Aircraft>>();

	[SerializeField]
	private List<VesselPhase> allVesselPhases = new List<VesselPhase>();

	// Use this for initialization
	void Start ()
	{

	}
	
	// Update is called once per frame
	void Update ()
	{

	}

	public void StartBattle()
	{
		currentBattleTime = 0f;
		ExecuteNextPhase ();
	}

	public void RegisterVessel(Vessel v)
	{
		if (v is Aircraft) 
		{
			if (v.side >= sidesAC.Count) 
			{
				for(int i = sidesAC.Count; i <= v.side; ++i)
					sidesAC.Add(new List<Aircraft>());
			}
			sidesAC[v.side].Add(v as Aircraft);
		}
		else
		{
			if (v.side >= sides.Count) 
			{
				for(int i = sides.Count; i <= v.side; ++i)
					sides.Add(new List<Vessel>());
			}
			sides[v.side].Add(v);
		}
	}

	public void UnregisterVessel(Vessel v)
	{
		if (v is Aircraft)
		{
			if (v.side < sidesAC.Count) 
			{
				sidesAC[v.side].Remove(v as Aircraft);	
			}
		}
		else
		{
			if (v.side < sides.Count) 
			{
				sides[v.side].Remove(v);	
			}
		}
	}

	public void AddPhase(VesselPhase vp)
	{
		allVesselPhases.Add(vp);
		allVesselPhases.Sort(VesselPhase.sComparison);
	}

	public bool CheckSides()
	{
		for(int i = 0; i < sides.Count; ++i)
			if(sides[i].Count == 0 && (sidesAC.Count <= i || sidesAC[i].Count == 0))
		{
			// One side has no vessels left.
			return false;
		}
		return true;
	}

	public void ExecuteNextPhase()
	{
		if (CheckSides () == false) return;

		RequestVesselPhases();
		if(allVesselPhases.Count > 0)
		{
			while(allVesselPhases[0].owner.isAlive == false)
			{
				allVesselPhases.RemoveAt(0);
			}
			if(allVesselPhases.Count > 0)
			{
				VesselPhase vp = allVesselPhases[0];
				currentBattleTime = vp.battleTime;
				vp.owner.ExecutePhase(vp);
				allVesselPhases.Remove(vp);
			}
			else
			{
				// Battle ends because no vessel is alive.
			}
		}
		else
		{
			// Battle ends because no vessel is alive.
		}
	}

	public void RequestVesselPhases()
	{
		foreach(Vessel v in SceneManager.instance.allVesselsInScene)
		{
			v.CheckPhase();
		}
	}
}
