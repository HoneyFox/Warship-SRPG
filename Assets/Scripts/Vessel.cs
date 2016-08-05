using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EVesselType
{
	CV,
	CVL,
	BB,
	BC,
	CA,
	CL,
	DD,
	SS,
	AP,
	AIRCRAFT,
}

public class Vessel : MonoBehaviour 
{
	protected List<VesselPhase> ownPhases = new List<VesselPhase>();

	public EVesselType vesselType;
	public int side;

	// Vessel properties.
	public int maxHp;
	public int gunPower;
	public int gunRange;
	public int torpedoPower;
	public int torpedoRange;
	public int aswPower;
	public int aswRange;
	public int armor;
	public int speed;
	public int dodge;
	public int antiAir;
	public int sight;
	public int sonarSight;
	public int accuracy;
	public int luck;
	public int[] aircraftSlots;
	public bool[] aircraftLaunched;

	// In-combat properties.
	public int hp;
	public bool isAlive { get { return hp > 0; } }
	public int actualSight;
	public int actualSonarSight;
	public int actualSpeed;
	public Aircraft[] aircrafts;

	protected float nextMovePhaseTime = 0f;
	protected float nextActionPhaseTime = 0f;

	public float movePhasePeriod;
	public float actionPhasePeriod;

	// Use this for initialization
	protected virtual void Start () 
	{
		SceneManager.instance.RegisterVessel(this);
		nextMovePhaseTime = BattleManager.instance.currentBattleTime;
		nextActionPhaseTime = BattleManager.instance.currentBattleTime;
	}
	
	// Update is called once per frame
	protected virtual void Update () {
	
	}

	public virtual void OnDamaged(int damage)
	{
		if(damage != int.MaxValue)
		{
			if(hp - damage <= 0)
			{
				Debug.Log("<color=red>" + this.gameObject.name + " is destroyed.</color>");
			}
			else if(!(this is Aircraft) && hp >= maxHp * 0.5f && hp - damage < maxHp * 0.5f)
			{
				Debug.Log("<color=yellow>" + this.gameObject.name + " is damaged.</color>");
			}
		}
	
		hp = Mathf.Max(0, hp - damage);
		if (isAlive == false)
		{
			OnLeaveBattle();
		}
	}

	public virtual void OnLeaveBattle()
	{
		SceneManager.instance.UnregisterVessel(this);
		BattleManager.instance.sides[this.side].Remove(this);
		ownPhases.Clear();
	}

	public virtual void CheckPhase()
	{
		int movePhaseCount = 0;
		int actionPhaseCount = 0;
		for (int i = 0; i < ownPhases.Count; ++i)
		{
			VesselPhase vp = ownPhases[i];
			if (vp.phaseType == EPhaseType.MOVE || vp.phaseType == EPhaseType.RTB)
				movePhaseCount++;
			if (vp.phaseType == EPhaseType.ACTION)
				actionPhaseCount++;
		}

		if (isAlive)
		{
			if (movePhaseCount == 0)
			{
				CreatePhase(EPhaseType.MOVE, nextMovePhaseTime);
			}
			if (actionPhaseCount == 0)
			{
				CreatePhase(EPhaseType.ACTION, nextActionPhaseTime);
			}
		}
	}

	public void CreatePhase(EPhaseType type, float battleTime)
	{
		VesselPhase vp = new VesselPhase();
		vp.owner = this;
		vp.phaseType = type;
		vp.battleTime = battleTime;
		ownPhases.Add(vp);
		BattleManager.instance.AddPhase(vp);
	}

	public virtual void ExecutePhase(VesselPhase vp)
	{
		switch (vp.phaseType)
		{
			case EPhaseType.MOVE:
				//Debug.Log("@" + vp.battleTime.ToString("F1") + ": " + this.gameObject.name + " moved to new location.");
			
				nextMovePhaseTime += movePhasePeriod;
				break;
			case EPhaseType.ACTION:
				List<Vessel> targets = this.GetTargets(VesselActions.ETargetType.SURF | VesselActions.ETargetType.SUB, true);
				if (targets.Count > 0 && UnityEngine.Random.Range(0, this.maxHp * 0.6f) <= this.hp)
				{
					bool hasLaunchedAircraft = false;
					int maxAircraftLaunched = this.GetMaxAircraftLaunched();
					if (targets.Any((Vessel v) => v.vesselType == EVesselType.CV || v.vesselType == EVesselType.CVL) == false)
					{ 
						// There's no CV/CVL and we don't need to worry about air-strikes.
						Vessel target = targets[UnityEngine.Random.Range(0, targets.Count)];
						if (target.vesselType == EVesselType.SS)
						{
							for (int i = 0; i < aircraftSlots.Length; ++i)
							{
								if (aircrafts[i].aircraftType == EAircraftType.ASW_BOMBER)
								{
									Aircraft aircraft = this.LaunchAircraft(i);
									if (aircraft != null)
									{
										Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
											  + this.gameObject.name + " launched " + aircrafts[i].gameObject.name
											  + "x" + aircraft.hp.ToString()
										);
										hasLaunchedAircraft = true;
										break;
									}
								}
							}
						}
						if (hasLaunchedAircraft == false)
						{
							for (int i = 0; i < aircraftSlots.Length; ++i)
							{
								if (aircrafts[i].aircraftType == EAircraftType.BOMBER || aircrafts[i].aircraftType == EAircraftType.TORPEDO_BOMBER)
								{
									Aircraft aircraft = this.LaunchAircraft(i);
									if (aircraft != null)
									{
										Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
											  + this.gameObject.name + " launched " + aircrafts[i].gameObject.name
											  + "x" + aircraft.hp.ToString()
										);
										hasLaunchedAircraft = true;
										break;
									}
								}
							}
						}
					}
					if(hasLaunchedAircraft == false)
					{				
						bool aircraftAvailable = this.CheckAircraftAvailable();
						if (aircraftAvailable && maxAircraftLaunched > 0)
						{
							// The vessel can launch aircrafts.
							for (int i = 0; i < aircraftSlots.Length; ++i)
							{
								// Kamikaze aircrafts are only launched when there are existing targets.
								if (aircrafts[i].kamikaze)
								{
									bool canEngageAir = aircrafts[i].antiAir > 0;
									bool canEngageSurf = aircrafts[i].gunPower > 0 || aircrafts[i].torpedoPower > 0;
									bool canEngageSub = aircrafts[i].aswPower > 0;
									
									bool needsToLaunch = false;
									if (canEngageAir && this.GetTargets(VesselActions.ETargetType.AIR, true).Count > 0)
										needsToLaunch |= true;
									if (canEngageSurf && this.GetTargets(VesselActions.ETargetType.SURF, true).Count > 0)
										needsToLaunch |= true;
									if (canEngageSub && this.GetTargets(VesselActions.ETargetType.SUB, true).Count > 0)
										needsToLaunch |= true;
									if (needsToLaunch == false)
										continue;
								}

								Aircraft aircraft = this.LaunchAircraft(i);
								if (aircraft != null)
								{
									Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
										  + this.gameObject.name + " launched " + aircrafts[i].gameObject.name
										  + "x" + aircraft.hp.ToString()
									);
									hasLaunchedAircraft = true;
									break;
								}
							}
						}
					}

					if (hasLaunchedAircraft == true)
					{
						nextActionPhaseTime += actionPhasePeriod;
					}
					else
					{
						Vessel target = targets[UnityEngine.Random.Range(0, targets.Count)];
						bool isCritical;

						bool canUseGun = this.CheckRange(target, VesselActions.EWeaponType.GUN);
						bool canUseTorpedo = this.CheckRange(target, VesselActions.EWeaponType.TORPEDO);
						bool canUseASW = this.CheckRange(target, VesselActions.EWeaponType.ASW);

						if (canUseASW && target.vesselType == EVesselType.SS)
						{
							int origHp = hp;
							int damage = CombatEvaluator.DamageByASW(this, target, out isCritical);
							Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
								  + this.gameObject.name + " attacked " + target.gameObject.name
								  + " with depth-charges: " + damage.ToString()
								  + (isCritical ? " Critical!" : ""));

							this.OnDamaged(0);
							target.OnDamaged(Mathf.Max(0, damage));
							nextActionPhaseTime += actionPhasePeriod;
						}
						else if (UnityEngine.Random.Range(0, gunPower + torpedoPower) < gunPower)
						{
							int damage = CombatEvaluator.DamageByGun(this, target, out isCritical);
							Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
								  + this.gameObject.name + " attacked " + target.gameObject.name
								  + " with guns: " + damage.ToString()
								  + (isCritical ? " Critical!" : ""));
							target.OnDamaged(Mathf.Max(0, damage));
							nextActionPhaseTime += actionPhasePeriod;
						}
						else
						{
							int damage = CombatEvaluator.DamageByTorpedo(this, target, out isCritical);
							Debug.Log("@" + vp.battleTime.ToString("F1") + ": "
								  + this.gameObject.name + " attacked " + target.gameObject.name
								  + " with torpedos: " + damage.ToString()
								  + (isCritical ? " Critical!" : ""));
							target.OnDamaged(Mathf.Max(0, damage));
							nextActionPhaseTime += actionPhasePeriod;
						}
					}
				}
				else
				{
					// Damage control. Recover some hp this round.
					int hpRepaired = hp;
					hp = Mathf.Max(hp, Mathf.FloorToInt(Mathf.Lerp(hp, maxHp * 0.75f, 0.15f)));
					hpRepaired = hp - hpRepaired;
					if(hpRepaired > 0)
					{
						Debug.Log("@" + vp.battleTime.ToString("F1") + ": " + this.gameObject.name + " repaired: +" + hpRepaired.ToString());
						nextActionPhaseTime += actionPhasePeriod;
					}
					else 
					{
						nextActionPhaseTime += actionPhasePeriod;
					}
				}
				break;
		}
		ownPhases.Remove(vp);
		CheckPhase();
		BattleManager.instance.Invoke("ExecuteNextPhase", 0.25f);
	}
}
