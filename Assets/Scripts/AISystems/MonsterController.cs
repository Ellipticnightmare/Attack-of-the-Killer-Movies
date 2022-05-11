using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : Jericho
{
    public enemy Enemy = enemy.Clown;
    [HideInInspector]
    public float clownTimer;
    public float specialEventTimerSet = 30;
    float specialEventTimer;
    // Start is called before the first frame update
    void Start()
    {
        MyAIManager.hotOrCold = 10;
        MyAIManager.patienceIndex = 15;
        specialEventTimer = specialEventTimerSet;
        trackTimer = 2;
        attackCooldown = 1;
        MyNavMeshManager.agent = this.GetComponent<NavMeshAgent>();
        MyAnimations.anim = this.GetComponent<Animator>();
        MyNavMeshManager.path = new NavMeshPath();
        BuildNode();
        UpdateWeightingList();
        Personality = (personality)Random.Range(0, 4);
        MyAIManager.aggroRange = MyAIManager.aggroRange + EnemyManager.instance.difficultyCheck;
        MyAIManager.viewConeRadius = MyAIManager.viewConeRadius + (EnemyManager.instance.difficultyCheck * 1.25f * 1.25f);
        MyAIManager.viewConeAngle = MyAIManager.viewConeAngle + EnemyManager.instance.difficultyCheck;
    }

    // Update is called once per frame
    void Update()
    {
        realMovSpeed = MyAIManager.moveSpeed;
        switch (Personality)
        {
            case personality.Aggressive:
                if (EnemyManager.instance.NodeFromWorldPoint(transform.position).corridor)
                    realMovSpeed = MyAIManager.moveSpeed * 1.5f;
                break;
            case personality.Cocky:
                if (EnemyManager.instance.NodeFromWorldPoint(transform.position).openArea)
                    realMovSpeed = MyAIManager.moveSpeed * 1.5f;
                break;
        }
        if (specialEventTimer >= 0 && specialEventTimerSet > 0)
            specialEventTimer--;
        else
        {
            if (specialEventTimerSet > 0)
            {
                specialEventTimer = specialEventTimerSet;
                EnemyManager.instance.triggerUniqueSpawn(this);
            }
        }
        MyNavMeshManager.maximumWebSize = Mathf.Clamp(MyNavMeshManager.maximumWebSize, 7, 15);
        MyNavMeshManager.revisitThreshold = Mathf.Clamp(MyNavMeshManager.revisitThreshold, 69, 91);
        if (attackCooldownReal < attackCooldown)
            attackCooldownReal += Time.deltaTime;
        switch (MyAIManager.EnemyState)
        {
            case aiManager.enemyState.Roam:
                if (MyAnimations.hasAnimations)
                    MyAnimations.anim.SetInteger("state", 0); //walk anim
                if (canSeePlayer() && Enemy != enemy.Clown)
                {
                    if (NavMesh.CalculatePath(transform.position, targPlayer.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                    {
                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                        {
                            startHuntTrigger(targPlayer.transform.position);
                        }
                        else
                        {
                            MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
                        }
                    }
                }
                if (hearSound() && Enemy != enemy.Clown)
                {
                    if (NavMesh.CalculatePath(transform.position, targNoise.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                    {
                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            startHuntTrigger(targNoise.transform.position);
                        else
                            MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
                    }
                }
                break;
            case aiManager.enemyState.Hunting:
                if (MyAnimations.hasAnimations)
                    MyAnimations.anim.SetInteger("state", 0); //walk anim
                if (canSeePlayer())
                {
                    if (NavMesh.CalculatePath(transform.position, targPlayer.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                    {
                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                        {
                            startHuntTrigger(targPlayer.transform.position);
                        }
                        else
                        {
                            MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
                        }
                    }
                }
                if (hearSound())
                {
                    if (NavMesh.CalculatePath(transform.position, targNoise.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                    {
                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            startHuntTrigger(targNoise.transform.position);
                        else
                            MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
                    }
                }
                break;
            case aiManager.enemyState.Chase:
                if (MyAnimations.hasAnimations)
                    MyAnimations.anim.SetInteger("state", 1); //run anim
                if (trackTimer >= 0)
                {
                    trackTimer -= Time.deltaTime;
                    FindPath(transform.position, targPlayer.transform.position);
                }
                else
                {
                    if (canSeePlayer())
                    {
                        if (NavMesh.CalculatePath(transform.position, targPlayer.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                        {
                            if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            {
                                FindPath(transform.position, targPlayer.transform.position);
                            }
                            else
                            {
                                MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
                            }
                        }
                    }
                    else
                    {
                        if (Enemy == enemy.Clown)
                        {
                            if (clownTimer >= 0)
                            {
                                clownTimer--;
                                FindPath(transform.position, targPlayer.transform.position);
                            }
                            else
                            {
                                MyAIManager.EnemyState = aiManager.enemyState.Roam;
                                BuildNode();
                            }
                        }
                        else
                        {
                            pingBarricade(false);
                            MyAIManager.EnemyState = aiManager.enemyState.Roam;
                        }
                    }
                    trackTimer = 2;
                }
                break;
            case aiManager.enemyState.targetBarricades:
                if (Enemy != enemy.Blob && Enemy != enemy.Dracula)
                    pingBarricade(false);
                else
                    MyAIManager.EnemyState = aiManager.enemyState.Roam;
                break;
            case aiManager.enemyState.Stunned:
                if (MyAnimations.hasAnimations)
                    MyAnimations.anim.SetInteger("state", 2); //Dizzy/stun anim
                MyNavMeshManager.agent.speed = 0;
                MyNavMeshManager.agent.angularSpeed = 0;
                if (stunTimer >= 0)
                    stunTimer -= Time.deltaTime;
                else
                {
                    BuildNode();
                    MyAIManager.EnemyState = aiManager.enemyState.Roam;
                }
                break;
            case aiManager.enemyState.Attack:
                if (MyAnimations.hasAnimations)
                    MyAnimations.anim.SetInteger("state", 3); //Attack anim
                break;
        }
        if (hasFinishedPath)
        {
            if (attackCooldownReal >= attackCooldown)
            {
                switch (MyAIManager.EnemyState)
                {
                    case aiManager.enemyState.Roam:
                        BuildNode();
                        break;
                    case aiManager.enemyState.Hunting:
                        MyAIManager.EnemyState = aiManager.enemyState.Roam;
                        BuildNode();
                        break;
                    case aiManager.enemyState.Chase:
                        attack(targPlayer);
                        break;
                    case aiManager.enemyState.targetBarricades:
                        hitBarricade();
                        break;
                    case aiManager.enemyState.Attack:
                        attack(targPlayer);
                        break;
                }
                attackCooldownReal = 0;
                hasFinishedPath = false;
            }
        }
    }
    public override void pingBarricade(bool fromPlayer) => base.pingBarricade(fromPlayer);
    public override void BuildNode() => base.BuildNode();
    public override void attack(PlayerObject player) => base.attack(player);
    public override void hitBarricade() => base.hitBarricade();
    public override void startHuntTrigger(Vector3 position) => base.startHuntTrigger(position);
    public override void VanishEnemy() => base.VanishEnemy();
    public override void ReturnEnemy() => base.ReturnEnemy();
    public override void FinishHit() => base.FinishHit();
    public override void StartStun(float stunTime) => base.StartStun(stunTime);
    public enum enemy
    {
        Blob,
        Dracula,
        Frankenstein,
        Wolfman,
        Mummy,
        Grey,
        SwampMonster,
        Clown
    };
}
[System.Serializable]
public struct webNodePoint
{
    public Vector3 nodePoint;
    public int weighting;
}