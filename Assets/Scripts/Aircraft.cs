using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum EAircraftType
{
	BOMBER,
	TORPEDO_BOMBER,
	FIGHTER,
	ASW_BOMBER,
	SURVEILLANCE,
}

public class Aircraft : Vessel 
{
	public float rtbPhasePeriod;

	// For aircrafts, hp represents how many aircrafts are inside this slot.
	public EAircraftType aircraftType;
	public Vessel carrier;
	public int carrierSlot;

	public int maxFuel;
	public int fuel;

	public bool kamikaze;

	public override void OnLeaveBattle()
	{
		SceneManager.instance.UnregisterVessel(this);
		BattleManager.instance.sides[this.side].Remove(this);
		ownPhases.Clear();
		if (carrier != null && carrier.isAlive)
		{
			carrier.aircraftLaunched[carrierSlot] = false;
		}
		Destroy(this.gameObject);
	}

	public override void ExecutePhase(VesselPhase vp)
	{
		fuel--;
		switch (vp.phaseType)
		{
		case EPhaseType.MOVE:
			//Debug.Log("@" + vp.battleTime.ToString("F1") + ": " + this.gameObject.name + " moved to new location.");
			
			nextPhaseTime += movePhasePeriod;

			if(aircraftType == EAircraftType.SURVEILLANCE)
			{
				if(fuel > 0)
				{
					VesselPhase movePhase = new VesselPhase();
					movePhase.owner = this;
					movePhase.phaseType = EPhaseType.MOVE;
					movePhase.battleTime = nextPhaseTime;
					ownPhases.Add(movePhase);
					BattleManager.instance.AddPhase(movePhase);
				}
				else 
				{
					SetupRTBPhase();
				}
			}
			else 
			{
				VesselPhase actionPhase = new VesselPhase();
				actionPhase.owner = this;
				actionPhase.phaseType = EPhaseType.ACTION;
				actionPhase.battleTime = nextPhaseTime;
				ownPhases.Add(actionPhase);
				BattleManager.instance.AddPhase(actionPhase);
			}
			break;
		case EPhaseType.ACTION:
			List<Vessel> targets = this.GetTargets(VesselActions.ETargetType.AIR);
			if (this.aircraftType == EAircraftType.FIGHTER && targets.Count > 0)
			{
				Aircraft target = targets[UnityEngine.Random.Range(0, targets.Count)] as Aircraft;

				int attackerHp = this.hp;
				int defenderHp = target.hp;
				CombatEvaluator.AirCombat(this, target);
				if (kamikaze)
					hp = 0;
				Debug.Log("@" + vp.battleTime.ToString("F1") + ": " 
					+ this.gameObject.name
					+ "(" + attackerHp.ToString() + "->" + this.hp.ToString()
					+ ") engaged with " + target.gameObject.name 
			    	+ "(" + defenderHp.ToString() + "->" + target.hp.ToString() + ")"
				);
				
				// Notify the attacker/defender that their Hp might have changed.
				this.OnDamaged(0);
				target.OnDamaged(0);

				nextPhaseTime += actionPhasePeriod;
			}
			else
			{
				targets = this.GetTargets(VesselActions.ETargetType.SURF | VesselActions.ETargetType.SUB);
				if (targets.Count > 0)
				{
					Vessel target = targets[UnityEngine.Random.Range(0, targets.Count)];
					bool isCritical;

					bool canUseGun = this.CheckRange(target, VesselActions.EWeaponType.BOMB);
					bool canUseTorpedo = this.CheckRange(target, VesselActions.EWeaponType.AERIAL_TORPEDO);
					bool canUseASW = this.CheckRange(target, VesselActions.EWeaponType.AERIAL_ASW);
					if (canUseASW && target.vesselType == EVesselType.SS)
					{
						int origHp = hp;
						int damage = CombatEvaluator.DamageByAerialASW(this, target, out isCritical);
						if (kamikaze)
							hp = 0; 
						Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
								  + this.gameObject.name + "(" + origHp.ToString() + "->" + hp.ToString() + ") attacked " + target.gameObject.name
								  + " with depth-charges: " + damage.ToString()
								  + (isCritical ? " Critical!" : ""));

						this.OnDamaged(0);
						target.OnDamaged(Mathf.Max(0, damage));
						nextPhaseTime += actionPhasePeriod;
					}
					else if(UnityEngine.Random.Range(0, (canUseGun ? gunPower : 0) + (canUseTorpedo ? torpedoPower : 0)) < (canUseGun ? gunPower : 0))
					{
						int origHp = hp;
						int damage = CombatEvaluator.DamageByBomb(this, target, out isCritical);
						if (kamikaze)
							hp = 0;
						Debug.Log("@" + vp.battleTime.ToString("F1") + ": " 
						          + this.gameObject.name + "(" + origHp.ToString() + "->" + hp.ToString() + ") attacked " + target.gameObject.name
						          + " with bombs: " + damage.ToString()
						          + (isCritical ? " Critical!" : ""));
						
						this.OnDamaged(0);
						target.OnDamaged(Mathf.Max(0, damage));
						nextPhaseTime += actionPhasePeriod;
					}
					else
					{
						int origHp = hp;
						int damage = CombatEvaluator.DamageByAerialTorpedo(this, target, out isCritical);
						if (kamikaze)
							hp = 0;
						Debug.Log("@" + vp.battleTime.ToString("F1") + ": " 
						          + this.gameObject.name + "(" + origHp.ToString() + "->" + hp.ToString() + ") attacked " + target.gameObject.name
						          + " with torpedos: " + damage.ToString()
						          + (isCritical ? " Critical!" : ""));
						this.OnDamaged(0);
						target.OnDamaged(Mathf.Max(0, damage));
						nextPhaseTime += actionPhasePeriod;
					}
				}
				else
				{
					nextPhaseTime += actionPhasePeriod;
				}
			}

			SetupRTBPhase();
			break;		
		case EPhaseType.RTB:
			if(carrier != null && carrier.isAlive)
			{
				carrier.aircraftSlots[carrierSlot] += hp;
				Debug.Log ("@" + vp.battleTime.ToString("F1") + ": " + this.gameObject.name + "x" + hp.ToString() + " RTB.");
				OnDamaged(int.MaxValue);
			}
			else
			{
				Debug.Log ("@" + vp.battleTime.ToString("F1") + ": " + this.gameObject.name + "x" + hp.ToString()  + " run out of fuel and crashed.");
				OnDamaged(int.MaxValue);
			}
			break;
		}
		ownPhases.Remove(vp);
		CheckPhase();
		BattleManager.instance.Invoke("ExecuteNextPhase", 1f);
	}

	private void SetupRTBPhase()
	{
		if (kamikaze)
		{
			OnDamaged(int.MaxValue);
			return;
		};
		nextPhaseTime += rtbPhasePeriod;
		VesselPhase rtbPhase = new VesselPhase();
		rtbPhase.owner = this;
		rtbPhase.phaseType = EPhaseType.RTB;
		rtbPhase.battleTime = nextPhaseTime;
		ownPhases.Add(rtbPhase);
		BattleManager.instance.AddPhase(rtbPhase);
	}
}
	
	