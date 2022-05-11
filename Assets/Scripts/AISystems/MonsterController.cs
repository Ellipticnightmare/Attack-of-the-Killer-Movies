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
        MyAIManager.aggroRange = MyAIManager.aggroRange + EnemyManager.instance.difficultyCheck;
        MyAIManager.viewConeRadius = MyAIManager.viewConeRadius + (EnemyManager.instance.difficultyCheck * 1.25f * 1.25f);
        MyAIManager.viewConeAngle = MyAIManager.viewConeAngle + EnemyManager.instance.difficultyCheck;
    }

    // Update is called once per frame
    void Update()
    {
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
        MyNavMeshManager.agent.speed = MyAIManager.moveSpeed;
        MyNavMeshManager.agent.angularSpeed = MyAIManager.rotSpeed;
        switch (MyAIManager.EnemyState)
        {
            case aiManager.enemyState.Stunned:
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
            case aiManager.enemyState.Roam:
                MyAnimations.anim.SetInteger("state", 0);
                break;
            case aiManager.enemyState.Chase:
                MyAnimations.anim.SetInteger("state", 1);
                break;
            case aiManager.enemyState.Attack:
                MyAnimations.anim.SetInteger("state", 3);
                break;
            case aiManager.enemyState.Hunting:
                MyAnimations.anim.SetInteger("state", 0);
                break;
        }
        if(MyAIManager.EnemyState == aiManager.enemyState.Roam || MyAIManager.EnemyState == aiManager.enemyState.Chase || MyAIManager.EnemyState == aiManager.enemyState.Hunting)
        {
            if (!isTargetingBarricades)
            {
                if (!isHunting)
                {
                    if (canSeePlayer() && Enemy != enemy.Clown)
                    {
                        if(NavMesh.CalculatePath(transform.position, targPlayer.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                        {
                            if(MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            {
                                MyNavMeshManager.agent.SetDestination(targPlayer.transform.position);
                                isHunting = true;
                                MyAIManager.EnemyState = aiManager.enemyState.Chase;
                            }
                        }
                    }
                    if(hearSound() && Enemy != enemy.Clown)
                    {
                        if (NavMesh.CalculatePath(transform.position, targNoise.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                        {
                            if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            {
                                startHuntTrigger(targNoise.transform.position);
                                isHunting = true;
                                MyAIManager.EnemyState = aiManager.enemyState.Hunting;
                            }
                        }
                    }
                }
                else
                {
                    if (trackTimer >= 0)
                        trackTimer -= Time.deltaTime;
                    else
                    {
                        trackTimer = 2;
                        switch (MyAIManager.EnemyState)
                        {
                            case aiManager.enemyState.Chase:
                                if (canSeePlayer())
                                {
                                    if(NavMesh.CalculatePath(transform.position, targPlayer.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                                    {
                                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                                            MyNavMeshManager.agent.SetDestination(targPlayer.transform.position);
                                        else
                                        {
                                            pingBarricade(true);
                                            isTargetingBarricades = true;
                                            isHunting = false;
                                        }
                                    }
                                }
                                else
                                {
                                    pingBarricade(false);
                                    isHunting = false;
                                }
                                break;
                            case aiManager.enemyState.Hunting:
                                if (hearSound())
                                {
                                    if (NavMesh.CalculatePath(transform.position, targNoise.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                                    {
                                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                                            MyNavMeshManager.agent.SetDestination(targNoise.transform.position);
                                        else
                                        {
                                            pingBarricade(false);
                                            isTargetingBarricades = true;
                                            isHunting = false;
                                        }
                                    }
                                }
                                else
                                    isHunting = false;
                                break;
                        }
                    }
                }
            }
            if (!MyNavMeshManager.agent.pathPending)
            {
                if(MyNavMeshManager.agent.remainingDistance <= MyNavMeshManager.agent.stoppingDistance)
                {
                    MyNavMeshManager.agent.velocity = new Vector3(0,0,0);
                    if(!MyNavMeshManager.agent.hasPath || MyNavMeshManager.agent.velocity.sqrMagnitude == 0)
                    {
                        if(attackCooldownReal >= attackCooldown)
                        {
                            if (isHunting)
                                attack(targPlayer);
                            else if (isTargetingBarricades)
                                hitBarricade();
                            else
                                BuildNode();
                            attackCooldownReal = 0;
                        }
                    }
                }
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