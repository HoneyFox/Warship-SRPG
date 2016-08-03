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
				List<Vessel> targets = BattleManager.instance.sides[1 - side];
				if (targets.Count > 0)
				{
					int maxAircraftLaunched = 3 + (gunPower < 100 ? gunPower / 5 : (gunPower - 100) / 20 + 20);
					bool aircraftAvailable = false;
					for(int i = 0; i < aircraftSlots.Length; ++i)
					{			
						if(aircraftSlots[i] > 0 && aircraftLaunched[i] == false)
						{
							aircraftAvailable = true;
							break;
						}
					}
					if (aircraftAvailable && hp >= maxHp * 0.5)
					{
						// The vessel can launch aircrafts.
						for(int i = 0; i < aircraftSlots.Length; ++i)
						{
							if(aircraftSlots[i] > 0 && aircraftLaunched[i] == false)
							{
								Aircraft aircraft = aircrafts[i];
								Aircraft instance = Instantiate(aircraft, this.transform.position, this.transform.rotation) as Aircraft;
								instance.gameObject.name = this.gameObject.name + "'s " + aircraft.gameObject.name + "<" + (i+1).ToString() + ">";
								instance.maxHp = instance.hp = Mathf.Min(aircraftSlots[i], maxAircraftLaunched);
								instance.side = this.side;
								instance.carrier = this;
								instance.carrierSlot = i;
								instance.luck = this.luck;
								instance.accuracy = this.accuracy;
								instance.fuel = instance.maxFuel;
								aircraftSlots[i] -= instance.hp;
								aircraftLaunched[i] = true;
								Debug.Log("@" + vp.battleTime.ToString("F1") + ": " 
							          + this.gameObject.name + " launched " + aircraft.gameObject.name
							          + "x" + instance.hp.ToString()
							    );
								break;
							}
						}
					}
					else
					{
						Vessel target = targets[UnityEngine.Random.Range(0, targets.Count)];
						bool isCritical;

						if(UnityEngine.Random.Range(0, gunPower + torpedoPower) < gunPower)
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
		BattleManager.instance.Invoke("ExecuteNextPhase", 1f);
	}
}
