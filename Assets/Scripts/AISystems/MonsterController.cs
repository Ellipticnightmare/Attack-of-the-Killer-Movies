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
        MyAIManager.aggroRange = MyAIManager.aggroRange + EnemyManager.instance.difficultyCheck;
        MyAIManager.viewConeRadius = MyAIManager.viewConeRadius + (EnemyManager.instance.difficultyCheck * 1.25f * 1.25f);
        MyAIManager.viewConeAngle = MyAIManager.viewConeAngle + EnemyManager.instance.difficultyCheck;
    }

    // Update is called once per frame
    void Update()
    {
        MyNavMeshManager.maximumWebSize = Mathf.Clamp(MyNavMeshManager.maximumWebSize, 7, 15);
        MyNavMeshManager.revisitThreshold = Mathf.Clamp(MyNavMeshManager.revisitThreshold, 69, 91);
        MyNavMeshManager.agent.speed = MyAIManager.moveSpeed * GameManager.instance.tMod;
        MyNavMeshManager.agent.angularSpeed = MyAIManager.rotSpeed * GameManager.instance.tMod;
        if (GameManager.instance.tMod > 0)
        {
            if (transform.position == myPoint && MyNavMeshManager.agent.pathPending)
                isStalled = true;
            if (isStalled && stallTimer <= 2)
                stallTimer += Time.deltaTime * 1;
            else if(stallTimer > 2)
                RespawnEnemyAtClosestPoint(transform.position);

            if (stallTimer < 2)
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
                if (MyAIManager.EnemyState == aiManager.enemyState.Roam)
                {
                    if (receivedEnvironmentInput() && targPoint != null)
                    {
                        if (NavMesh.CalculatePath(transform.position, targPoint.position, NavMesh.AllAreas, MyNavMeshManager.path))
                        {
                            if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete && Enemy != enemy.Clown)
                                startHuntTrigger(targPoint.position);
                        }
                    }
                }
                else if(MyAIManager.EnemyState == aiManager.enemyState.Chase)
                {
                    if (targPlayer == null)
                    {
                        if (canSeePlayer())
                        {
                            if (NavMesh.CalculatePath(transform.position, targPoint.position, NavMesh.AllAreas, MyNavMeshManager.path))
                            {
                                if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                                    startHuntTrigger(targPlayer.transform.position);
                            }
                        }
                    }
                    else
                    {
                        if (trackTimer >= 0)
                            trackTimer -= Time.deltaTime;
                        else
                        {
                            trackTimer = .75f;
                            if (receivedEnvironmentInput() && targPoint != null)
                            {
                                if (NavMesh.CalculatePath(transform.position, targPoint.position, NavMesh.AllAreas, MyNavMeshManager.path))
                                {
                                    if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                                        startHuntTrigger(targPoint.position);
                                }
                            }
                            else
                                MyAIManager.EnemyState = aiManager.enemyState.Roam;
                        }
                    }
                }
            }
            if (!MyNavMeshManager.agent.pathPending)
            {
                if (MyNavMeshManager.agent.remainingDistance <= MyNavMeshManager.agent.stoppingDistance)
                {
                    MyNavMeshManager.agent.velocity = new Vector3(0, 0, 0);
                    if (!MyNavMeshManager.agent.hasPath || MyNavMeshManager.agent.velocity.sqrMagnitude == 0)
                    {
                        if (MyAIManager.EnemyState == aiManager.enemyState.Chase)
                            Attack();
                        else
                            BuildNode();
                    }
                }
            }
        }
    }
    private void LateUpdate() => myPoint = transform.position;
    public override void BuildNode() => base.BuildNode();
    public void Attack() => attack(targPlayer);
    public override void FinishHit() => base.FinishHit();
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