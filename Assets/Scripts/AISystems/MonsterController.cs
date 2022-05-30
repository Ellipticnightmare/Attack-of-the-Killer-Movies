using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : Jericho
{
    public enemy Enemy = enemy.Clown;
    public float specialEventTimerSet = 30;
    float specialEventTimer;
    Vector3 myPoint;
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
        isStalled = transform.position == myPoint && !MyNavMeshManager.agent.pathPending;
        if (!isStalled)
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
            }
            if (MyAIManager.EnemyState == aiManager.enemyState.Roam || MyAIManager.EnemyState == aiManager.enemyState.Chase)
            {
                if (!isHunting)
                {
                    if (receivedEnvironmentInput() && Enemy != enemy.Clown)
                    {
                        if (NavMesh.CalculatePath(transform.position, targPoint.position, NavMesh.AllAreas, MyNavMeshManager.path))
                        {
                            if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                            {
                                startHuntTrigger(targPoint.position);
                            }
                        }
                    }
                }
                else
                {
                    if (Enemy == enemy.Clown)
                    {
                        if (trackTimer >= 0)
                            trackTimer -= Time.deltaTime;
                        else
                            isHunting = false;
                        PlayerObject victim = targPlayer();
                        if (victim != null)
                        {
                            if (NavMesh.CalculatePath(transform.position, victim.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                            {
                                if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                                    startHuntTrigger(victim.transform.position);
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
                            if (MyAIManager.EnemyState == aiManager.enemyState.Chase)
                            {
                                if (receivedEnvironmentInput())
                                {
                                    if (NavMesh.CalculatePath(transform.position, targPoint.transform.position, NavMesh.AllAreas, MyNavMeshManager.path))
                                    {
                                        if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                                            startHuntTrigger(targPoint.transform.position);
                                        else
                                            isHunting = false;
                                    }
                                }
                                else
                                    isHunting = false;
                            }
                        }
                    }
                }
                if (!MyNavMeshManager.agent.pathPending)
                    Debug.Log(this.gameObject + " has pending path 1");{
                    if (MyNavMeshManager.agent.remainingDistance <= MyNavMeshManager.agent.stoppingDistance + 0.2f)
                    {
                        Debug.Log(this.gameObject + " is in range 2");
                        MyNavMeshManager.agent.velocity = new Vector3(0, 0, 0);
                        //if (!MyNavMeshManager.agent.hasPath || MyNavMeshManager.agent.velocity.sqrMagnitude == 0)
                        //{
                            Debug.Log(this.gameObject + " weird thing 3");
                            if (attackCooldownReal >= attackCooldown)
                            {
                                Debug.Log(this.gameObject + " attack cooldown 4");
                                if (isHunting)
                                {
                                    PlayerObject newTarg = targPlayer();
                                    if (Vector3.Distance(this.gameObject.transform.position,
                                            newTarg.transform.position) <= 1.5f)
                                    {
                                        attack(newTarg);
                                        Debug.Log(this.gameObject + " attacked");
                                        isHunting = false;
                                        MyAIManager.EnemyState = aiManager.enemyState.Roam;
                                    }
                                }
                                BuildNode();
                                attackCooldownReal = 0;
                            }
                        //}
                    }
                }
            }
        }
        else
        {
            if (!respawn)
            {
                stallTimer += Time.deltaTime;
                respawn = stallTimer > 4.0f;
                if (stallTimer > 4.0f)
                {
                    RespawnEnemyAtClosestPoint(myPoint);
                }
            }
        }
    }
    private void LateUpdate() => myPoint = transform.position;
    public override void BuildNode() => base.BuildNode();
    public override void attack(PlayerObject player) => base.attack(player);
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