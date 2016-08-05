using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Flags]
public enum EPhaseType
{
	MOVE = 1,
	ACTION = 2,
	RTB = 4,
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
		if (this.battleTime == other.battleTime)
		{
			return ((int)this.phaseType).CompareTo((int)other.phaseType);
		}
		else
		{
			return this.battleTime.CompareTo(other.battleTime);
		}
	}
}
