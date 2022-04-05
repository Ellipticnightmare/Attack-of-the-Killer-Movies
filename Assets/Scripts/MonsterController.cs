using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    NavMeshAgent agent;
    NavMeshPath path;
    List<webNodePoint> nodeWeb;
    webNodePoint curNode, nextNode;
    Animator anim;
    public int nodeMinDistance, nodeWeightDistance;
    Vector3 avoidDest;
    [Range(8, 14)]
    public int maximumWebSize;
    [Range(0, 10)]
    public int roamingDesire;
    [Range(70, 90)]
    public int revisitThreshold;
    public float walkRadius = 4f;
    public float moveSpeed, rotSpeed;
    CameraHandler mainCamera;
    enemyState EnemyState = enemyState.Roam;
    [HideInInspector]
    public bool isClown;
    float clownTimer, trackTimer, stunTimer, attackCooldown, attackCooldownReal;
    Barrier targBar;
    // Start is called before the first frame update
    void Start()
    {
        trackTimer = 2;
        clownTimer = 300;
        agent = this.GetComponent<NavMeshAgent>();
        anim = this.GetComponent<Animator>();
        path = new NavMeshPath();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraHandler>();
        BuildNode();
    }

    // Update is called once per frame
    void Update()
    {
        maximumWebSize = Mathf.Clamp(maximumWebSize, 7, 15);
        revisitThreshold = Mathf.Clamp(revisitThreshold, 69, 91);
        agent.speed = moveSpeed;
        agent.angularSpeed = rotSpeed;
        if (attackCooldownReal < attackCooldown)
            attackCooldownReal += Time.deltaTime;
        switch (EnemyState)
        {
            case enemyState.Roam:
                anim.SetInteger("state", 0); //walk anim
                if (canSeePlayer(mainCamera.targetTransform))
                {
                    if (NavMesh.CalculatePath(transform.position, mainCamera.targetTransform.position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            startHuntTrigger();
                        }
                        else
                        {
                            EnemyState = enemyState.targetBarricades;
                        }
                    }
                }
                break;
            case enemyState.Chase:
                anim.SetInteger("state", 1); //run anim
                if (trackTimer >= 0)
                    trackTimer -= Time.deltaTime;
                else
                {
                    trackTimer = 2;
                    if (canSeePlayer(mainCamera.targetTransform))
                    {
                        if (NavMesh.CalculatePath(transform.position, mainCamera.transform.position, NavMesh.AllAreas, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete)
                            {
                                agent.SetDestination(mainCamera.transform.position);
                            }
                            else
                            {
                                EnemyState = enemyState.targetBarricades;
                            }
                        }
                    }
                    else
                    {
                        if (isClown)
                        {
                            if (clownTimer >= 0)
                                clownTimer--;
                            else
                            {
                                pingBarricade(true);
                                EnemyState = enemyState.Roam;
                            }
                        }
                        else
                        {
                            pingBarricade(false);
                            EnemyState = enemyState.Roam;
                        }
                    }
                }
                break;
            case enemyState.targetBarricades:
                pingBarricade(false);
                break;
            case enemyState.Stunned:
                anim.SetInteger("state", 2); //Dizzy/stun anim
                agent.speed = 0;
                agent.angularSpeed = 0;
                if (stunTimer >= 0)
                    stunTimer -= Time.deltaTime;
                else
                {
                    BuildNode();
                    EnemyState = enemyState.Roam;
                }
                break;
            case enemyState.Attack:
                anim.SetInteger("state", 3); //Attack anim
                break;
        }
        if (!agent.pathPending)
        {
            if(agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.velocity = new Vector3(0, 0, 0);
                if(!agent.hasPath || agent.velocity.magnitude == 0)
                {
                    if(attackCooldownReal >= attackCooldown)
                    {
                        switch (EnemyState)
                        {
                            case enemyState.Roam:
                                BuildNode();
                                break;
                            case enemyState.Chase:
                                attack(mainCamera.targetTransform);
                                break;
                            case enemyState.targetBarricades:
                                hitBarricade();
                                break;
                            case enemyState.Attack:
                                attack(mainCamera.targetTransform);
                                break;
                        }
                        attackCooldownReal = 0;
                    }
                }
            }
        }
    }
    void BuildNode() /*gets run at start, when enemy exits Roam state,
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
            int genWeighting = (int)(100 - Vector3.Distance(newPoint, mainCamera.targetTransform.position));
            webNodePoint newNode = new webNodePoint();
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
        int distance = nodeWeightDistance;
        webNodePoint x = new webNodePoint();
        int newI = 0;
        for (int i = 0; i < nodeWeb.Count; i++)
        {
            if (Vector3.Distance(nodeWeb[i], mainCamera.targetTransform.position) < distance)
            {
                x = nodeWeb[i];
                newI = i;
                distance = Vector3.Distance(nodeWeb[i], mainCamera.targetTransform.position);
            }
        }
        x.weighting = 100;
        avoidDest = x.nodePoint;
        nodeWeb[newI] = x;
    }
    void pingBarricade(bool fromPlayer)
    {
        float hitRange = fromPlayer ? 50 : 5;
        Vector3 startPos = fromPlayer ? mainCamera.targetTransform.position : transform.position;
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
            }
            if (targBar != null)
                agent.SetDestination(targBar.transform.position);
            else
                Debug.Log(destrucTargs[index]);
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
        agent.SetDestination(mainCamera.targetTransform.position);
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
    bool canSeePlayer(Transform player)
    {
        bool output = false;
        RaycastHit hit;
        Vector3 fromPosition = this.transform.position;
        Vector3 targetPosition = player.position;
        Vector3 direction = targetPosition - fromPosition;
        Vector3 posRelative = transform.InverseTransformPoint(player.position);
        if(Physics.Raycast(this.transform.position, direction, out hit) && posRelative.z > 0 )
        {
            if (hit.collider.gameObject == player.gameObject)
                output = true;
        }
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
    };
}
[System.Serializable]
public struct webNodePoint
{
    public Vector3 nodePoint;
    public int weighting;
}