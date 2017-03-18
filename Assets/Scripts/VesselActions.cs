using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class VesselActions
{
	[Flags]
	public enum ETargetType 
	{ 
		SURF = 1,
		SUB = 2,
		AIR = 4,
		ALL = SURF | SUB | AIR,
	}

	[Flags]
	public enum EWeaponType
	{ 
		ANTI_AIR = 1,
		GUN = 2,
		BOMB = 4,
		TORPEDO = 8,
		AERIAL_TORPEDO = 16,
		ASW = 32,
		AERIAL_ASW = 64,
	}

	public static bool CheckAircraft(this Vessel vessel, Vessel target)
	{
		if (target.isAlive == false) return false;

		// Target is aircraft.
		if (target.vesselType == EVesselType.AIRCRAFT)
		{
			for (int i = 0; i < vessel.aircrafts.Length; ++i)
			{
				if (vessel.aircrafts[i].aircraftType == EAircraftType.FIGHTER
					&& vessel.aircrafts[i].kamikaze
					&& vessel.aircraftSlots[i] > 0
					&& vessel.aircraftLaunched[i] == false
				)
				{
					return true;
				}
			}
			return false;
		}
		else if (target.vesselType == EVesselType.SS)
		{
			for (int i = 0; i < vessel.aircrafts.Length; ++i)
			{
				if (vessel.aircrafts[i].aircraftType == EAircraftType.ASW_BOMBER
					&& vessel.aircrafts[i].kamikaze
					&& vessel.aircraftSlots[i] > 0
					&& vessel.aircraftLaunched[i] == false
				)
				{
					return true;
				}
			}
			return false;
		}
		else 
		{
			for (int i = 0; i < vessel.aircrafts.Length; ++i)
			{
				if ((vessel.aircrafts[i].gunPower > 0 || vessel.aircrafts[i].torpedoPower > 0)
					&& vessel.aircrafts[i].kamikaze
					&& vessel.aircraftSlots[i] > 0
					&& vessel.aircraftLaunched[i] == false
				)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static bool CheckWeapon(this Vessel vessel, Vessel target, bool considerAircrafts) 
	{
		if (target.isAlive == false) return false;

		// Target is aircraft.
		if (target.vesselType == EVesselType.AIRCRAFT)
		{
			// We don't allow ships to engage aircrafts actively in this game.
			// Exception is: vessels that carry kamikaze fighters. (anti-air missiles)
			if (vessel.vesselType != EVesselType.AIRCRAFT)
				return considerAircrafts && vessel.CheckAircraft(target);
			
			// If this aircraft is a fighter, never engage other aircrafts actively.
			if ((vessel as Aircraft).aircraftType != EAircraftType.FIGHTER) return false;
			
			return vessel.CheckRange(target, EWeaponType.ANTI_AIR);
		}
		else if (target.vesselType == EVesselType.SS)
		{
			// If this vessel has kamikaze asw bombers. (anti-sub missiles)
			if (vessel.vesselType != EVesselType.AIRCRAFT)
				if (considerAircrafts && vessel.CheckAircraft(target))
					return true;
			
			// Only vessels that have aswPower > 0 can attack subs.
			if (vessel.aswPower == 0) return false;
			if (vessel.vesselType == EVesselType.AIRCRAFT)
				return CheckRange(vessel, target, EWeaponType.AERIAL_ASW);
			else
				return CheckRange(vessel, target, EWeaponType.ASW);
		}
		else
		{ 
			// Aircraft to Surface.
			if (vessel.vesselType == EVesselType.AIRCRAFT)
			{
				bool canFire = false;
				canFire |= CheckRange(vessel, target, EWeaponType.BOMB);
				canFire |= CheckRange(vessel, target, EWeaponType.AERIAL_TORPEDO);
				return canFire;
			}
			else
			{
				bool canFire = false;
				canFire |= (considerAircrafts && vessel.CheckAircraft(target));
				canFire |= CheckRange(vessel, target, EWeaponType.GUN);
				canFire |= CheckRange(vessel, target, EWeaponType.TORPEDO);
				return canFire;
			}
		}
	}

	public static bool CheckRange(this Vessel vessel, Vessel target, EWeaponType weapon) 
	{
		float distance = Vector3.Distance(vessel.transform.position, target.transform.position);
		switch (weapon)
		{
			case EWeaponType.ANTI_AIR:
				return true;
			case EWeaponType.BOMB:
				return vessel.gunPower > 0;
			case EWeaponType.AERIAL_TORPEDO:
				return vessel.torpedoPower > 0;
			case EWeaponType.AERIAL_ASW:
				return vessel.aswPower > 0;
			case EWeaponType.GUN:
				return vessel.gunPower > 0 && distance < vessel.gunRange;
			case EWeaponType.TORPEDO:
				return vessel.torpedoPower > 0 && distance < vessel.torpedoRange;
			case EWeaponType.ASW:
				return vessel.aswPower > 0 && distance < vessel.aswRange;
			default:
				return false;
		}
	}

	public static List<Vessel> GetTargets(this Vessel vessel, ETargetType targetTypes, bool considerAircrafts)
	{
		bool findSurf = (targetTypes & ETargetType.SURF) > 0;
		bool findSub = (targetTypes & ETargetType.SUB) > 0;
		bool findAir = (targetTypes & ETargetType.AIR) > 0;
		List<Vessel> result = new List<Vessel>();
		if(findSurf || findSub)
		{
			foreach(Vessel v in BattleManager.instance.sides[1 - vessel.side])
			{
				if (v.isAlive == false) continue;
				if (v.vesselType == EVesselType.SS && findSub)
				{
					if(vessel.CheckWeapon(v, considerAircrafts) == true)
						result.Add(v);
				}
				else if (v.vesselType != EVesselType.SS && findSurf)
				{
					if (vessel.CheckWeapon(v, considerAircrafts) == true)
						result.Add(v);
				}
			}
		}
		if (findAir && BattleManager.instance.sidesAC.Count > (1 - vessel.side))
		{
			foreach (Aircraft a in BattleManager.instance.sidesAC[1 - vessel.side])
			{
				if (a.isAlive == false) continue;
				if (vessel.CheckWeapon(a, considerAircrafts) == true)
					result.Add(a);
			}
		}

		return result;
	}

	public static int GetMaxAircraftLaunched(this Vessel vessel)
	{
		switch (vessel.vesselType)
		{ 
			case EVesselType.CV:
			case EVesselType.CVL:
				return (vessel.hp >= vessel.maxHp * 0.5 ? 3 + vessel.gunPower / 5 : 0);
			default:
				return (vessel.hp >= vessel.maxHp * 0.5 ? 3 + vessel.gunPower / 20 : 0);
		}
	}

	public static bool CheckAircraftAvailable(this Vessel vessel)
	{
		for (int i = 0; i < vessel.aircraftSlots.Length; ++i)
		{
			if (vessel.aircraftSlots[i] > 0 && vessel.aircraftLaunched[i] == false)
			{
				return true;
			}
		}
		return false;
	}

	public static Aircraft LaunchAircraft(this Vessel vessel, int slotIndex)
	{
		int maxAircraftLaunched = vessel.GetMaxAircraftLaunched();
		if (vessel.aircraftSlots[slotIndex] > 0 && maxAircraftLaunched > 0)
		{
			Aircraft aircraft = vessel.aircrafts[slotIndex];
			Aircraft instance = UnityEngine.Object.Instantiate(aircraft, vessel.transform.position, vessel.transform.rotation) as Aircraft;
			instance.gameObject.name = vessel.gameObject.name + "'s " + aircraft.gameObject.name + "<" + (slotIndex + 1).ToString() + ">";
			instance.maxHp = instance.hp = Mathf.Min(vessel.aircraftSlots[slotIndex], maxAircraftLaunched);
			instance.side = vessel.side;
			instance.carrier = vessel;
			instance.carrierSlot = slotIndex;
			instance.luck = vessel.luck;
			instance.accuracy = vessel.accuracy;
			instance.fuel = instance.maxFuel;

			vessel.aircraftSlots[slotIndex] -= instance.hp;
			vessel.aircraftLaunched[slotIndex] = true;

			return instance;
		}
		else
			return null;
	}
}