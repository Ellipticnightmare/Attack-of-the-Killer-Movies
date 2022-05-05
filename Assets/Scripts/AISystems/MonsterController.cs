using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : Jericho
{
    public enemy Enemy = enemy.Clown;
    [HideInInspector]
    public float clownTimer;
    // Start is called before the first frame update
    void Start()
    {
        trackTimer = 2;
        attackCooldown = 1;
        MyNavMeshManager.agent = this.GetComponent<NavMeshAgent>();
        MyAnimations.anim = this.GetComponent<Animator>();
        MyNavMeshManager.path = new NavMeshPath();
        BuildNode();
        UpdateWeightingList();
    }

    // Update is called once per frame
    void Update()
    {
        MyNavMeshManager.maximumWebSize = Mathf.Clamp(MyNavMeshManager.maximumWebSize, 7, 15);
        MyNavMeshManager.revisitThreshold = Mathf.Clamp(MyNavMeshManager.revisitThreshold, 69, 91);
        MyNavMeshManager.agent.speed = MyAIManager.moveSpeed;
        MyNavMeshManager.agent.angularSpeed = MyAIManager.rotSpeed;
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
                            startHuntTrigger();
                        }
                        else
                        {
                            MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
                        }
                    }
                }
                break;
            case aiManager.enemyState.Chase:
                if (MyAnimations.hasAnimations)
                    MyAnimations.anim.SetInteger("state", 1); //run anim
                if (trackTimer >= 0)
                {
                    trackTimer -= Time.deltaTime;
                    MyNavMeshManager.agent.SetDestination(targPlayer.transform.position);
                }
                else
                {
                    if (canSeePlayer())
                    {
                        if (NavMesh.CalculatePath(transform.position, targPlayer.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                        {
                            if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            {
                                MyNavMeshManager.agent.SetDestination(targPlayer.transform.position);
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
                                MyNavMeshManager.agent.SetDestination(targPlayer.transform.position);
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
                pingBarricade(false);
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
        if (!MyNavMeshManager.agent.pathPending)
        {
            if (MyNavMeshManager.agent.remainingDistance <= MyNavMeshManager.agent.stoppingDistance)
            {
                MyNavMeshManager.agent.velocity = new Vector3(0, 0, 0);
                if (!MyNavMeshManager.agent.hasPath || MyNavMeshManager.agent.velocity.magnitude == 0)
                {
                    if (attackCooldownReal >= attackCooldown)
                    {
                        switch (MyAIManager.EnemyState)
                        {
                            case aiManager.enemyState.Roam:
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
                    }
                }
            }
        }
    }
    public override void pingBarricade(bool fromPlayer) => base.pingBarricade(fromPlayer);
    public override void BuildNode() => base.BuildNode();
    public override void attack(PlayerObject player) => base.attack(player);
    public override void hitBarricade() => base.hitBarricade();
    public override void startHuntTrigger() => base.startHuntTrigger();
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