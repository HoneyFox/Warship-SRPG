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

	protected float nextPhaseTime = 0f;

	public float movePhasePeriod;
	public float actionPhasePeriod;

	// Use this for initialization
	protected virtual void Start () 
	{
		SceneManager.instance.RegisterVessel(this);
		nextPhaseTime = BattleManager.instance.currentBattleTime;
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
		if(ownPhases.Count == 0)
		{
			if(isAlive)
			{
				VesselPhase movePhase = new VesselPhase();
				movePhase.owner = this;
				movePhase.phaseType = EPhaseType.MOVE;
				movePhase.battleTime = nextPhaseTime;
				ownPhases.Add(movePhase);
				BattleManager.instance.AddPhase(movePhase);
			}
		}
	}

	public virtual void ExecutePhase(VesselPhase vp)
	{
		switch (vp.phaseType)
		{
			case EPhaseType.MOVE:
				//Debug.Log("@" + vp.battleTime.ToString("F1") + ": " + this.gameObject.name + " moved to new location.");
			
				nextPhaseTime += movePhasePeriod;
				
				VesselPhase actionPhase = new VesselPhase();
				actionPhase.owner = this;
				actionPhase.phaseType = EPhaseType.ACTION;
				actionPhase.battleTime = nextPhaseTime;
				ownPhases.Add(actionPhase);
				BattleManager.instance.AddPhase(actionPhase);
				break;
			case EPhaseType.ACTION:
				List<Vessel> targets = this.GetTargets(VesselActions.ETargetType.SURF | VesselActions.ETargetType.SUB);
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
					else
					{				
						bool aircraftAvailable = this.CheckAircraftAvailable();
						if (aircraftAvailable && maxAircraftLaunched > 0)
						{
							// The vessel can launch aircrafts.
							for (int i = 0; i < aircraftSlots.Length; ++i)
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
							nextPhaseTime += actionPhasePeriod;
						}
						else if (UnityEngine.Random.Range(0, gunPower + torpedoPower) < gunPower)
						{
							int damage = CombatEvaluator.DamageByGun(this, target, out isCritical);
							Debug.Log("@" + vp.battleTime.ToString("F1") + ": " 
						          + this.gameObject.name + " attacked " + target.gameObject.name
						          + " with guns: " + damage.ToString()
						          + (isCritical ? " Critical!" : ""));
							target.OnDamaged(Mathf.Max(0,damage));
							nextPhaseTime += actionPhasePeriod;
						}
						else
						{
							int damage = CombatEvaluator.DamageByTorpedo(this, target, out isCritical);
							Debug.Log("@" + vp.battleTime.ToString("F1") + ": " 
						          + this.gameObject.name + " attacked " + target.gameObject.name
						          + " with torpedos: " + damage.ToString()
						          + (isCritical ? " Critical!" : ""));
							target.OnDamaged(Mathf.Max(0,damage));
							nextPhaseTime += actionPhasePeriod;
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
						nextPhaseTime += actionPhasePeriod;
					}
					else 
					{
						nextPhaseTime += actionPhasePeriod;
					}
				}
				break;
		}
		ownPhases.Remove(vp);
		CheckPhase();
		BattleManager.instance.Invoke("ExecuteNextPhase", 0.25f);
	}
}
