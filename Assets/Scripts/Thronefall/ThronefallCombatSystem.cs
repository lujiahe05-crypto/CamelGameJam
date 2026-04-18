using System.Collections.Generic;
using UnityEngine;

public interface ICombatEntity
{
    int MaxHP { get; }
    int CurrentHP { get; }
    int ATK { get; }
    int DEF { get; }
    Vector3 Position { get; }
    bool IsAlive { get; }
    void TakeDamage(int damage);
    void Die();
}

public class ThronefallCombatSystem : MonoBehaviour
{
    List<ICombatEntity> allEntities = new List<ICombatEntity>();
    ThronefallBuilding mainBase;

    public void RegisterEntity(ICombatEntity entity)
    {
        if (!allEntities.Contains(entity))
            allEntities.Add(entity);
    }

    public void UnregisterEntity(ICombatEntity entity)
    {
        allEntities.Remove(entity);
    }

    public void SetMainBase(ThronefallBuilding building)
    {
        mainBase = building;
    }

    public ThronefallBuilding GetMainBase()
    {
        return mainBase;
    }

    public static int CalculateDamage(int attackerATK, int defenderDEF)
    {
        return Mathf.Max(0, attackerATK - defenderDEF);
    }

    public void ApplyDamage(ICombatEntity attacker, ICombatEntity defender)
    {
        if (attacker == null || defender == null || !defender.IsAlive) return;

        int damage = CalculateDamage(attacker.ATK, defender.DEF);
        if (damage > 0)
            defender.TakeDamage(damage);
    }

    public ThronefallEnemy FindNearestEnemy(Vector3 position, float maxRange)
    {
        ThronefallEnemy nearest = null;
        float nearestDist = maxRange * maxRange;

        for (int i = allEntities.Count - 1; i >= 0; i--)
        {
            var entity = allEntities[i];
            if (entity is ThronefallEnemy enemy && enemy.IsAlive)
            {
                float dist = (enemy.Position - position).sqrMagnitude;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }

    public ThronefallBuilding FindNearestBuilding(Vector3 position, float maxRange)
    {
        ThronefallBuilding nearest = null;
        float nearestDist = maxRange * maxRange;

        for (int i = allEntities.Count - 1; i >= 0; i--)
        {
            var entity = allEntities[i];
            if (entity is ThronefallBuilding building && building.IsAlive)
            {
                float dist = (building.Position - position).sqrMagnitude;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = building;
                }
            }
        }

        return nearest;
    }

    public ThronefallAlly FindNearestAlly(Vector3 position, float maxRange)
    {
        ThronefallAlly nearest = null;
        float nearestDist = maxRange * maxRange;

        for (int i = allEntities.Count - 1; i >= 0; i--)
        {
            var entity = allEntities[i];
            if (entity is ThronefallAlly ally && ally.IsAlive)
            {
                float dist = (ally.Position - position).sqrMagnitude;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = ally;
                }
            }
        }

        return nearest;
    }

    void LateUpdate()
    {
        // Clean up destroyed entities
        for (int i = allEntities.Count - 1; i >= 0; i--)
        {
            if (allEntities[i] == null || (allEntities[i] is MonoBehaviour mb && mb == null))
                allEntities.RemoveAt(i);
        }
    }
}
