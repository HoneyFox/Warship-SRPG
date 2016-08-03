using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum EPhaseType
{
	MOVE,
	ACTION,
	RTB,
}

[Serializable]
public class VesselPhase : IComparable<VesselPhase>
{
	public static Comparison<VesselPhase> sComparison = new Comparison<VesselPhase>(
		(VesselPhase a, VesselPhase b) => a.CompareTo(b)
	);

	public Vessel owner;
	public float battleTime;
	public EPhaseType phaseType;

	public int CompareTo(VesselPhase other)
	{
		return this.battleTime.CompareTo(other.battleTime);
	}
}
