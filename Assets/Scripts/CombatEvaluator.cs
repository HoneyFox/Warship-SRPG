using System;
using UnityEngine;

public static class CombatEvaluator
{
	public static void AntiAir(Vessel attacker, Aircraft defender)
	{
		float factor = Mathf.Sqrt(defender.actualSpeed / 200f) * Mathf.Sqrt(defender.dodge);
		factor /= (1f + Mathf.Sqrt(attacker.antiAir));
		factor = Mathf.Clamp(factor, 0.2f, 1f);
		defender.hp = Mathf.FloorToInt(defender.hp * factor);
	}

	public static int DamageByGun(Vessel attacker, Vessel defender, out bool isCritical)
	{
		isCritical = false;

		float distance = Vector3.Distance (attacker.transform.position, defender.transform.position);
		if (distance > attacker.gunRange)
			return -1;

		float accuracy = attacker.accuracy * 0.05f / (1f + Mathf.Log10(defender.dodge));
		accuracy *= (0.5f + Mathf.Sqrt(1 - distance / attacker.actualSight));
		if (distance > attacker.actualSight)
			accuracy *= (0.5f + attacker.luck * 0.01f);
		accuracy = Mathf.Clamp (accuracy, 0.05f, 0.95f);
		if (UnityEngine.Random.Range (0f, 1f) < accuracy) 
		{
			// Hit target.
			if(defender.armor * 0.5f > attacker.gunPower)
				return 0;

			float damage = attacker.gunPower;
			damage *= UnityEngine.Random.Range(0.8f, 1.2f);
			damage *= Mathf.Clamp(1f + (attacker.luck - defender.luck) * 0.005f, 0.8f, 1.2f);
			if(UnityEngine.Random.Range(0f, 1f) < (0.25f + attacker.luck * 0.005f))
			{
				// Critical damage.
				damage *= UnityEngine.Random.Range(2f, 4f);
				isCritical = true;
			}
			damage /= (1f + Mathf.Sqrt(defender.armor));

			if(defender.armor < attacker.gunPower * 0.2f)
			{
				if (UnityEngine.Random.Range(0f, 1f) < defender.luck * 0.02f)
				{
					damage = Mathf.Sqrt(damage);
					isCritical = false;
				}
			}
			return Mathf.CeilToInt(damage);
		}
		else
		{
			// Miss target.
			int damage = -1;
			float blast = Mathf.Sqrt(attacker.gunPower) * 0.5f;
			float nearHit = blast / (1f + Mathf.Log10(defender.dodge));
			if(UnityEngine.Random.Range(0f, 1f) < nearHit)
			{
				damage = Mathf.CeilToInt(blast / (1f + Mathf.Log10(defender.armor)));
				return damage;
			}
			return damage;
		}
	}

	public static void AirCombat(Aircraft attacker, Aircraft defender)
	{
		// Note that some bombers/torpedo bombers also have antiAir so they can shoot back in air combat.
		float attackerStrength = attacker.antiAir * attacker.hp * attacker.hp * UnityEngine.Random.Range(0.9f, 1.1f);
		float defenderStrength = defender.antiAir * defender.hp * defender.hp * UnityEngine.Random.Range(0.9f, 1.1f);

		float rndAS = attackerStrength * Mathf.Clamp(1f + (attacker.luck - defender.luck) * 0.005f, 0.8f, 1.2f);
		float rndDS = defenderStrength * Mathf.Clamp(1f - (attacker.luck - defender.luck) * 0.005f, 0.8f, 1.2f);

		if (rndAS > rndDS * 5f)
		{
			// Defender is overwhelmed.
			defender.hp = 0;
			attacker.hp = Mathf.FloorToInt(Mathf.Sqrt((rndAS - rndDS) / rndAS) * attacker.hp);
		}
		else if(rndAS < rndDS / 5f)
		{
			// Attacker is overwhelmed.
			attacker.hp = 0;
			defender.hp = Mathf.FloorToInt(Mathf.Sqrt((rndDS - rndAS) / rndDS) * defender.hp);
		}
		else
		{
			// Lanchester equation.
			if(rndDS < rndAS)
			{
				attacker.hp = Mathf.FloorToInt(Mathf.Sqrt((rndAS - rndDS) / rndAS) * attacker.hp);
				defender.hp = Mathf.FloorToInt(defender.hp * 0.2f);
			}
			else
			{
				attacker.hp = Mathf.FloorToInt(attacker.hp * 0.2f);
				defender.hp = Mathf.FloorToInt(Mathf.Sqrt((rndDS - rndAS) / rndDS) * defender.hp);
			}
		}
	}

	public static int DamageByBomb(Aircraft attacker, Vessel defender, out bool isCritical)
	{
		AntiAir(defender, attacker);

		// Increase the fire power.
		int origAttackerGunPower = attacker.gunPower;
		attacker.gunPower = Mathf.FloorToInt(attacker.gunPower * (8f + Mathf.Sqrt(attacker.hp)));
		int origDefenderDodge = defender.dodge;
		defender.dodge = Mathf.FloorToInt(defender.dodge * 0.75f);

		int dmg = DamageByGun (attacker, defender, out isCritical);
		int damage;
		if(dmg == -1)
			damage = -1;
		else
			damage = Mathf.FloorToInt(dmg * Mathf.Sqrt(attacker.hp));

		attacker.gunPower = origAttackerGunPower;
		defender.dodge = origDefenderDodge;

		return damage;
	}

	public static int DamageByTorpedo(Vessel attacker, Vessel defender, out bool isCritical)
	{
		isCritical = false;
		
		float distance = Vector3.Distance (attacker.transform.position, defender.transform.position);
		if (distance > attacker.torpedoRange)
			return -1;
		
		float accuracy = attacker.accuracy * 0.05f / (1f + Mathf.Log10(defender.dodge)) / Mathf.Sqrt(defender.actualSpeed / 20f);
		accuracy *= (0.5f + Mathf.Sqrt(1 - distance / attacker.actualSight));
		if (distance > attacker.actualSight)
			accuracy *= (0.5f + attacker.luck * 0.01f);
		accuracy = Mathf.Clamp (accuracy, 0.05f, 0.95f);
		if (UnityEngine.Random.Range (0f, 1f) < accuracy)
		{
			// Hit target.
			if(defender.armor * 0.25f > attacker.torpedoPower)
				return 0;
			
			float damage = attacker.torpedoPower;
			damage *= UnityEngine.Random.Range(0.3f, 1.7f);
			damage *= Mathf.Clamp(1f + (attacker.luck - defender.luck) * 0.005f, 0.6f, 1.4f);
			if(UnityEngine.Random.Range(0f, 1f) < (0.25f + attacker.luck * 0.005f))
			{
				// Critical damage.
				damage *= UnityEngine.Random.Range(1f, 2f);
				isCritical = true;
			}
			damage /= (1f + Mathf.Sqrt(defender.armor * 0.2f));

			if(defender.armor < attacker.torpedoPower * 0.2f)
			{
				if (UnityEngine.Random.Range(0f, 1f) < defender.luck * 0.02f)
				{
					damage *= 0.5f;
					isCritical = false;
				}
			}
			return Mathf.CeilToInt(damage);
		}
		else
		{
			// Miss target.
			return -1;
		}
	}

	public static int DamageByAerialTorpedo(Aircraft attacker, Vessel defender, out bool isCritical)
	{
		// Reduce anti-air for torpedo bombers.
		int origDefenderAntiAir = defender.antiAir;
		defender.antiAir = Mathf.FloorToInt(defender.antiAir * 0.75f);

		AntiAir(defender, attacker);

		defender.antiAir = origDefenderAntiAir;

		// Multiple torpedo bombers can increase accuracy.
		int origAttackerAccuracy = attacker.accuracy;
		attacker.accuracy = Mathf.FloorToInt(attacker.accuracy * (1f + Mathf.Log10(attacker.hp)));
		int origAttackerTorpedoPower = attacker.torpedoPower;
		attacker.torpedoPower = Mathf.FloorToInt(attacker.torpedoPower * (8f + Mathf.Sqrt(attacker.hp)));

		int dmg = DamageByTorpedo (attacker, defender, out isCritical);
		int damage;
		if (dmg == -1)
			damage = -1;
		else
			damage = Mathf.FloorToInt(dmg * Mathf.Sqrt(attacker.hp));

		attacker.accuracy = origAttackerAccuracy;
		attacker.torpedoPower = origAttackerTorpedoPower;

		return damage;
	}

	public static int DamageByASW(Vessel attacker, Vessel defender, out bool isCritical)
	{
		isCritical = false;
		
		float distance = Vector3.Distance (attacker.transform.position, defender.transform.position);
		if (distance > attacker.aswRange)
			return -1;
		
		float accuracy = attacker.accuracy * 0.05f / (1f + Mathf.Log10(defender.dodge)) / Mathf.Sqrt(defender.actualSpeed / 10f);
		accuracy *= (0.5f + Mathf.Sqrt(1 - distance / attacker.actualSonarSight));
		if (distance > attacker.actualSonarSight)
			accuracy *= (0.1f + attacker.luck * 0.005f);
		accuracy = Mathf.Clamp (accuracy, 0.05f, 0.95f);
		if (UnityEngine.Random.Range (0f, 1f) < accuracy)
		{
			// Hit target.
			if(defender.armor * 0.5f > attacker.aswPower)
				return 0;
			
			float damage = attacker.aswPower;
			damage *= UnityEngine.Random.Range(0.4f, 1.2f);
			damage *= Mathf.Clamp(1f + (attacker.luck - defender.luck) * 0.005f, 0.6f, 1.4f);
			if(UnityEngine.Random.Range(0f, 1f) < (0.25f + attacker.luck * 0.005f))
			{
				// Critical damage.
				damage *= UnityEngine.Random.Range(2f, 4f);
				isCritical = true;
			}
			damage /= (1f + Mathf.Sqrt(defender.armor * 0.5f));

			return Mathf.CeilToInt(damage);
		}
		else
		{
			// Miss target.
			return -1;
		}
	}

	public static int DamageByAerialASW(Aircraft attacker, Vessel defender, out bool isCritical)
	{
		AntiAir(defender, attacker);

		// Increase accuracy when multiple aircrafts are dropping asw together in a certain area.
		int origAttackerAccuracy = attacker.accuracy;
		attacker.accuracy = Mathf.FloorToInt(attacker.accuracy * (1f + Mathf.Log10(attacker.hp)));
		int origAttackerASWPower = attacker.aswPower;
		attacker.aswPower = Mathf.FloorToInt(attacker.aswPower * (8f + Mathf.Sqrt(attacker.hp)));

		int dmg = DamageByASW (attacker, defender, out isCritical);
		int damage;
		if (dmg == -1)
			damage = -1;
		else
			damage = Mathf.FloorToInt(dmg * (1f + Mathf.Log10(attacker.hp)));

		attacker.accuracy = origAttackerAccuracy;
		attacker.aswPower = origAttackerASWPower;

		return damage;
	}
}

