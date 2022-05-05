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
<<<<<<< Updated upstream
    public virtual void BuildNode() /*gets run at start, when enemy exits Roam state,
                      and when enemy reaches their destination in Roam state*/
    {
        bool isValidNewWebPoint = true;
        Vector3 newPoint = Vector3.zero;
        while (newPoint == Vector3.zero)
        {
            newPoint = RandomNavmeshLocation(walkRadius);
        }
        if (nodeWeb.Count > 1 && nodeWeb.Count <= 7)
        {
            for (int i = 0; i < nodeWeb.Count; i++)
            {
                var x = nodeWeb[i];
                x.weighting = Mathf.Clamp(x.weighting - Random.Range(1, 4), 0, 100);
                nodeWeb[i] = x;
                if (Vector3.Distance(newPoint, nodeWeb[i].nodePoint) <= nodeMinDistance)
                    isValidNewWebPoint = false;
            }
        }
        else if (nodeWeb.Count >= 8)
        {
            nodeWeb.Sort((x, y) => y.weighting.CompareTo(x.weighting));
            nodeWeb.Remove(nodeWeb[nodeWeb.Count - 1]);
            isValidNewWebPoint = false;
        }
        if (isValidNewWebPoint)
        {
            webNodePoint newNode = new webNodePoint();
            int genWeighting = calculateWeighting(newNode.nodePoint);
            newNode.nodePoint = newPoint;
            newNode.weighting = (int)Mathf.Clamp(genWeighting, 0, 100);
            nodeWeb.Add(newNode);
            nextNode = newNode;
        }
        else
        {
            if (avoidDest != Vector3.zero)
            {
                foreach (var node in nodeWeb)
                {
                    if (node.nodePoint == avoidDest && node.weighting <= revisitThreshold)
                        avoidDest = Vector3.zero;
                }
            }
            int wantRandom = Random.Range(0, 20);
            if (wantRandom + roamingDesire <= 11)
            {
                nodeWeb.Sort((x, y) => y.weighting.CompareTo(x.weighting));
                if (avoidDest != nodeWeb[0].nodePoint)
                    nextNode = nodeWeb[0];
                else if (nodeWeb.Count > 1)
                    nextNode = nodeWeb[1];
                var x = nodeWeb[0];
                x.weighting = Mathf.Clamp(x.weighting - 10, 0, 100);
                nodeWeb[0] = x;
            }
            else
                nextNode = nodeWeb[Random.Range(0, nodeWeb.Count)];
        }
        curNode = nextNode;
        if (NavMesh.CalculatePath(transform.position, curNode.nodePoint, NavMesh.AllAreas, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
                agent.SetDestination(curNode.nodePoint);
            else
            {
                EnemyState = enemyState.targetBarricades;
            }
        }
    }
    void updateNodeWeight() //gets run when enemy spots player for the first time, exiting the Roam state
    {
        float distance = nodeWeightDistance;
        webNodePoint x = new webNodePoint();
        int newI = 0;
        for (int i = 0; i < nodeWeb.Count; i++)
        {
            if (Vector3.Distance(nodeWeb[i].nodePoint, targPlayer.transform.position) < distance)
            {
                x = nodeWeb[i];
                newI = i;
                distance = Vector3.Distance(nodeWeb[i].nodePoint, targPlayer.transform.position);
            }
        }
        x.weighting = 100;
        avoidDest = x.nodePoint;
        nodeWeb[newI] = x;
    }
    void pingBarricade(bool fromPlayer)
    {
        float hitRange = fromPlayer ? 50 : 5;
        Vector3 startPos = fromPlayer ? targPlayer.transform.position : transform.position;
        int index = -1;
        Collider[] Barricades;
        Barricades = Physics.OverlapSphere(startPos, hitRange);
        if (Barricades.Length > 0)
        {
            List<Barrier> destrucTargs = new List<Barrier>();
            foreach (var item in Barricades)
            {
                if (item.GetComponent<Barrier>() != null)
                    destrucTargs.Add(item.GetComponent<Barrier>());
            }
            float distancetoTarg = Mathf.Infinity;
            if (destrucTargs.Count > 0)
            {
                for (int i = 0; i < destrucTargs.Count; i++)
                {
                    float distCheck = Vector3.Distance(destrucTargs[i].transform.position, startPos);
                    if (distCheck < distancetoTarg)
                    {
                        distancetoTarg = distCheck;
                        targBar = destrucTargs[i];
                        index = i;
                    }
                }
                if (targBar != null)
                    agent.SetDestination(targBar.transform.position);
                else
                    Debug.Log(destrucTargs[index]);
            }
            else
                BuildNode();
        }
        else
            BuildNode();
    }
    public virtual void attack(Transform player) //For damage logic and unique effects
    {
        EnemyState = enemyState.Attack;
    }
    public virtual void hitBarricade() //Doesn't get run by Dracula or Blob
    {
        EnemyState = enemyState.Attack;
        targBar.Open();
    }
    public virtual void startHuntTrigger() //Can be manually triggered by Clown
    {
        agent.SetDestination(targPlayer.transform.position);
        updateNodeWeight();
        EnemyState = enemyState.Chase;
    }
    public void FinishHit() //Ends the attack animation
    {
        EnemyState = enemyState.Roam;
        targBar = null;
        BuildNode();
    }
    public void StartStun(float stunTime)
    {
        stunTimer = stunTime;
        EnemyState = enemyState.Stunned;
    }
    public virtual void VanishEnemy()
    {
        EnemyState = enemyState.Vanish;
        anim.CrossFade("VanishAnim", 0.2f); //Animation MUST be named this
    }
    public virtual void ReturnEnemy()
    {
        anim.CrossFade("ReturnAnim", 0.2f); //Animation MUST be named this
        StartCoroutine(waitForEndOfAnimation());
    }

    IEnumerator waitForEndOfAnimation()
    {
        while(EnemyState != enemyState.Roam)
        {
            yield return new WaitForEndOfFrame();
        }
        EnemyState = enemyState.Roam;
    }

    public Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
            finalPosition = hit.position;
        else
            finalPosition = Vector3.zero;
        return finalPosition;
    }
    bool canSeePlayer()
    {
        bool output = false;
        List<PlayerObject> visibleTargets = new List<PlayerObject>();
        visibleTargets.Clear();
        Vector3 fromPosition = this.transform.position;
        Collider[] targetsInViewRadius = Physics.OverlapSphere(fromPosition, viewRadius, targetMask);
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - fromPosition).normalized;
            if(Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(fromPosition, target.position);
                if(!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets.Add(target.GetComponent<PlayerObject>());
                }
            }
        }
        if(visibleTargets.Count > 0)
        {
            output = true;
            float minDist = Mathf.Infinity;
            foreach (var item in visibleTargets)
            {
                if (Vector3.Distance(item.transform.position, fromPosition) < minDist)
                {
                    minDist = Vector3.Distance(item.transform.position, fromPosition);
                    targPlayer = item;
                }
            }
        }
        return output;
    }
    int calculateWeighting(Vector3 pointPos)
    {
        int output = 0;
        float minDist = Mathf.Infinity;
        PlayerObject[] enemies = FindObjectsOfType<PlayerObject>();
        foreach (var item in enemies)
        {
            if (Vector3.Distance(item.transform.position, pointPos) < minDist)
                minDist = Vector3.Distance(item.transform.position, pointPos);
        }
        int genWeighting = (int)(100 - minDist);
        return output;
    }
    public enum enemyState
    {
        Stunned,
        Roam,
        Chase,
        targetBarricades,
        Attack,
        Vanish //Runs on Dracula and Alien
=======
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
>>>>>>> Stashed changes
    };
}
[System.Serializable]
public struct webNodePoint
{
    public Vector3 nodePoint;
    public int weighting;
}