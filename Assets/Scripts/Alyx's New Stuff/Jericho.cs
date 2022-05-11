using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Jericho : MonoBehaviour
{
    public personality Personality = personality.Aggressive;
    public myAudio MyAudio;
    public animationManager MyAnimations;
    public aiManager MyAIManager;
    public navmeshManager MyNavMeshManager;
    [HideInInspector]
    public PlayerObject targPlayer;
    [HideInInspector]
    public SoundPoint targNoise;
    Vector3 avoidDest;
    [HideInInspector]
    public Barrier targBar;
    [HideInInspector]
    public float trackTimer, stunTimer, attackCooldown, attackCooldownReal;
    [HideInInspector]
    public bool hasFinishedPath = false;
    [HideInInspector]
    public float realMovSpeed;
    public PathRequest currentPathRequest;
    public bool processingPath = true;

    #region Pathfinding
    #region Generic
    public virtual void startHuntTrigger(Vector3 newTarget) //Can be manually triggered by Clown
    {
        if (EnemyManager.instance.isPlayerActive(targPlayer) && targPlayer.transform.position == newTarget)
            FindPath(transform.position, EnemyManager.instance.returnPlayerAttackAngle(targPlayer, this));
        else
            FindPath(transform.position, newTarget);
        targNoise = null;
        updateNodeWeight();
        SFXManager.instance.PlaySound(MyAudio.mySound, MyAudio.mySource.position);
        MyAIManager.EnemyState = aiManager.enemyState.Chase;
    }
    public void UpdateWeightingList()
    {
        MyAIManager.myPreferredAngles.Sort((x, y) => y.nodeWeighting.CompareTo(x.nodeWeighting));
    }
    void updateNodeWeight() //gets run when enemy spots player for the first time, exiting the Roam state
    {
        float distance = MyNavMeshManager.nodeWeightDistance;
        webNodePoint x = new webNodePoint();
        int newI = 0;
        for (int i = 0; i < MyNavMeshManager.nodeWeb.Count; i++)
        {
            if (Vector3.Distance(MyNavMeshManager.nodeWeb[i].nodePoint, targPlayer.transform.position) < distance)
            {
                x = MyNavMeshManager.nodeWeb[i];
                newI = i;
                distance = Vector3.Distance(MyNavMeshManager.nodeWeb[i].nodePoint, targPlayer.transform.position);
            }
        }
        x.weighting = 100;
        avoidDest = x.nodePoint;
        MyNavMeshManager.nodeWeb[newI] = x;
    }
    public int calculateWeighting(Vector3 pointPos)
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
    public bool canSeePlayer()
    {
        bool output = false;
        List<PlayerObject> visibleTargets = new List<PlayerObject>();
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(MyPosition(), MyAIManager.viewConeRadius, MyAIManager.targetMask);
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - MyPosition()).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < MyAIManager.viewConeAngle / 2)
            {
                float dstToTarget = Vector3.Distance(MyPosition(), target.position);
                if (!Physics.Raycast(new Vector3(transform.position.x, transform.position.y, transform.position.z + 1), dirToTarget, dstToTarget, MyAIManager.obstacleMask))
                {
                    visibleTargets.Add(target.GetComponent<PlayerObject>());
                }
            }
        }
        if (visibleTargets.Count > 0)
        {
            output = true;
            float minDist = Mathf.Infinity;
            foreach (var item in visibleTargets)
            {
                if (Vector3.Distance(item.transform.position, MyPosition()) < minDist)
                {
                    minDist = Vector3.Distance(item.transform.position, MyPosition());
                    targPlayer = item;
                }
            }
        }
        return output;
    }
    public bool hearSound()
    {
        bool output = false;
        List<SoundPoint> soundTargets = new List<SoundPoint>();
        soundTargets.Clear();
        Collider[] targetsInHearRadius = Physics.OverlapSphere(MyPosition(), MyAIManager.hearingRadius, MyAIManager.targetMask);
        float distCheck = MyAIManager.hearingRadius + 1;
        int x = -1;
        for (int i = 0; i < targetsInHearRadius.Length; i++)
        {
            float dstToTarget = Vector3.Distance(MyPosition(), targetsInHearRadius[i].transform.position);
            Vector3 dirToTarget = (targetsInHearRadius[i].transform.position - MyPosition()).normalized;
            if (!Physics.Raycast(MyPosition(), dirToTarget))
                dstToTarget = dstToTarget + 4;
            if (dstToTarget < distCheck && targetsInHearRadius[i].GetComponent<SoundPoint>() != null)
            {
                targNoise = targetsInHearRadius[i].GetComponent<SoundPoint>();
                distCheck = dstToTarget;
                output = true;
            }
        }
        return output;
    }
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
    public Vector3 MyPosition()
    {
        return this.transform.position;
    }
    #endregion
    #region NavMesh
    public virtual void BuildNode()
    {
        if (MyAIManager.tensionIndex > MyAIManager.patienceIndex)
            MyAIManager.EnemyState = aiManager.enemyState.Hunting;
        bool isValidNewWebPoint = true;
        Vector3 newPoint = Vector3.zero;
        if (EnemyManager.instance.difficultyCheck >= 15 && MyAIManager.EnemyState == aiManager.enemyState.Hunting)
        {
            MyAIManager.patienceIndex--;
            MyAIManager.tensionIndex = 0;
            MyAIManager.patienceIndex = Mathf.Clamp(MyAIManager.patienceIndex, 3, 15);
            List<Vector3> searchChecks = new List<Vector3>();
            Vector3 newPointA = Vector3.zero;
            Vector3 newPointB = Vector3.zero;
            Vector3 newPointC = Vector3.zero;
            while (newPointA == Vector3.zero)
                newPointA = RandomNavmeshLocation(MyNavMeshManager.walkRadius + 15);
            while (newPointB == Vector3.zero)
                newPointB = RandomNavmeshLocation(MyNavMeshManager.walkRadius + 15);
            while (newPointC == Vector3.zero)
                newPointC = RandomNavmeshLocation(MyNavMeshManager.walkRadius + 15);
            searchChecks.Add(newPointA);
            searchChecks.Add(newPointB);
            searchChecks.Add(newPointC);
            float hotOrCold = Mathf.Infinity;
            Vector3 targPos = Vector3.zero;
            foreach (var item in searchChecks)
            {
                if (Vector3.Distance(item, FindObjectOfType<PlayerObject>().transform.position) < hotOrCold)
                {
                    hotOrCold = Vector3.Distance(item, FindObjectOfType<PlayerObject>().transform.position);
                    targPos = item;
                }
            }
            while (hotOrCold < MyAIManager.hotOrCold && hotOrCold > 1)
            {
                Vector3 chaseTarg = Random.insideUnitSphere * MyNavMeshManager.walkRadius;
                chaseTarg += targPos;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(chaseTarg, out hit, MyNavMeshManager.walkRadius, 1) && Vector3.Distance(hit.position, FindObjectOfType<PlayerObject>().transform.position) < MyAIManager.hotOrCold)
                {
                    MyAIManager.hotOrCold = (int)hotOrCold;
                    hotOrCold = Vector3.Distance(hit.position, FindObjectOfType<PlayerObject>().transform.position);
                    targPos = hit.position;
                }
            }
            newPoint = targPos;
        }
        else
        {
            if (EnemyManager.instance.difficultyCheck >= 15)
                MyAIManager.tensionIndex++;
            while (newPoint == Vector3.zero)
            {
                newPoint = RandomNavmeshLocation(MyNavMeshManager.walkRadius);
            }
            if (MyNavMeshManager.nodeWeb.Count > 1 && MyNavMeshManager.nodeWeb.Count <= 7)
            {
                for (int i = 0; i < MyNavMeshManager.nodeWeb.Count; i++)
                {
                    var x = MyNavMeshManager.nodeWeb[i];
                    x.weighting = Mathf.Clamp(x.weighting - Random.Range(1, 4), 0, 100);
                    MyNavMeshManager.nodeWeb[i] = x;
                    if (Vector3.Distance(newPoint, MyNavMeshManager.nodeWeb[i].nodePoint) <= MyNavMeshManager.nodeMinDistance)
                        isValidNewWebPoint = false;
                }
            }
            else if (MyNavMeshManager.nodeWeb.Count >= 8)
            {
                MyNavMeshManager.nodeWeb.Sort((x, y) => y.weighting.CompareTo(x.weighting));
                MyNavMeshManager.nodeWeb.Remove(MyNavMeshManager.nodeWeb[MyNavMeshManager.nodeWeb.Count - 1]);
                isValidNewWebPoint = false;
            }
        }
        if (isValidNewWebPoint)
        {
            webNodePoint newNode = new webNodePoint();
            int genWeighting = calculateWeighting(newPoint);
            newNode.nodePoint = newPoint;
            newNode.weighting = (int)Mathf.Clamp(genWeighting, 0, 100);
            if (EnemyManager.instance.difficultyCheck < 15)
                MyNavMeshManager.nodeWeb.Add(newNode);
            MyNavMeshManager.nextNode = newNode;
        }
        else
        {
            if (avoidDest != Vector3.zero)
            {
                foreach (var node in MyNavMeshManager.nodeWeb)
                {
                    if (node.nodePoint == avoidDest && node.weighting <= MyNavMeshManager.revisitThreshold)
                        avoidDest = Vector3.zero;
                }
            }
            int wantRandom = Random.Range(0, 20);
            if (wantRandom + MyNavMeshManager.roamingDesire <= 11)
            {
                MyNavMeshManager.nodeWeb.Sort((x, y) => y.weighting.CompareTo(x.weighting));
                if (avoidDest != MyNavMeshManager.nodeWeb[0].nodePoint)
                    MyNavMeshManager.nextNode = MyNavMeshManager.nodeWeb[0];
                else if (MyNavMeshManager.nodeWeb.Count > 1)
                    MyNavMeshManager.nextNode = MyNavMeshManager.nodeWeb[1];
                var x = MyNavMeshManager.nodeWeb[0];
                x.weighting = Mathf.Clamp(x.weighting - 10, 0, 100);
                MyNavMeshManager.nodeWeb[0] = x;
            }
            else
                MyNavMeshManager.nextNode = MyNavMeshManager.nodeWeb[Random.Range(0, MyNavMeshManager.nodeWeb.Count)];
        }
        MyNavMeshManager.curNode = MyNavMeshManager.nextNode;
        if (NavMesh.CalculatePath(transform.position, MyNavMeshManager.curNode.nodePoint, NavMesh.AllAreas, MyNavMeshManager.path))
        {
            if (MyNavMeshManager.path.status == NavMeshPathStatus.PathComplete)
                FindPath(transform.position, MyNavMeshManager.curNode.nodePoint);
            else
            {
                MyAIManager.EnemyState = aiManager.enemyState.targetBarricades;
            }
        }
    }
    public virtual void pingBarricade(bool fromPlayer)
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
                    FindPath(transform.position, targBar.transform.position);
                else
                    Debug.Log(destrucTargs[index]);
            }
            else
                BuildNode();
        }
        else
            BuildNode();
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
    #endregion
    #endregion
    #region A*
    public void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        currentPathRequest = new PathRequest(startPos, targetPos, null);
        TryProcessNext();
    }
    void TryProcessNext()
    {
        StartCoroutine(FindNewPath(currentPathRequest.pathStart, currentPathRequest.pathEnd));
    }
    void FinishedProcessingPath(Vector3[] path) => currentPathRequest.path = path;
    public Vector3[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> newPath = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            newPath.Add(currentNode);
            currentNode = currentNode.parent;
        }
        Vector3[] wayPoints = SimplifyPath(newPath);
        System.Array.Reverse(wayPoints);
        return wayPoints;
    }
    public Vector3[] SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector3 directionOld = Vector3.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 directionNew = new Vector3(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY, path[i - 1].gridZ - path[i].gridZ);
            if (directionNew != directionOld)
                waypoints.Add(path[i - 1].worldPosition);
            directionOld = directionNew;
        }
        return waypoints.ToArray();
    }
    IEnumerator FindNewPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;
        Node startNode = EnemyManager.instance.NodeFromWorldPoint(startPos);
        Node targetNode = EnemyManager.instance.NodeFromWorldPoint(targetPos + (Vector3.up));
        if (targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(EnemyManager.instance.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);
            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);
                if(currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }
                Jericho[] enemies = FindObjectsOfType<Jericho>();
                foreach (var item in enemies)
                {
                    if (item != this)
                    {
                        foreach (var item2 in EnemyManager.instance.GetNeighbors(EnemyManager.instance.NodeFromWorldPoint(item.transform.position), 1))
                            item2.movementPenalty = 10;
                    }
                }
                foreach (Node neighbour in EnemyManager.instance.GetNeighbors(currentNode, 1))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour)){
                        continue; 
                    }
                    switch (Personality)
                    {
                        case personality.Stealthy:
                            if (neighbour.openArea)
                                neighbour.movementPenalty = 6;
                            break;
                        case personality.Seeker:
                            if (neighbour.corridor)
                                neighbour.movementPenalty = 6;
                            break;
                    }
                    //EnemyManager.instance.BlurPenaltyMap(3);
                    int newMovementCostToNeighbour = currentNode.gCost + EnemyManager.instance.GetDistance(currentNode, neighbour);
                    if(newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = EnemyManager.instance.GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;
                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);  
                    }
                }
            }
        }
        else
        {
            foreach (var item in MyNavMeshManager.nodeWeb)
            {
                if (!EnemyManager.instance.NodeFromWorldPoint(item.nodePoint).walkable)
                {
                    MyNavMeshManager.nodeWeb.Remove(item);
                    break;
                }
            }
            BuildNode();
            StopCoroutine(FindNewPath(startPos, targetPos));
        }
        yield return new WaitForEndOfFrame();
        if (pathSuccess)
            waypoints = RetracePath(startNode, targetNode);
        FinishedProcessingPath(waypoints);
        processingPath = false;
        foreach (var item in EnemyManager.instance.grid)
        {
            if (item.movementPenalty > 0 && !item.obstructed)
                item.movementPenalty = 0;
        }
    }
    public IEnumerator FollowPath()
    {
        bool followingPath = true;
        int pathIndex = 0;
        if (currentPathRequest.path != null)
        {
            Path MyAStarManager = new Path(currentPathRequest.path, transform.position, MyAIManager.turnDst);

            transform.LookAt(MyAStarManager.lookPoints[0]);
            Quaternion targetRotation = Quaternion.LookRotation(MyAStarManager.lookPoints[0] - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * MyAIManager.rotSpeed);

            while (followingPath)
            {
                Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
                while (MyAStarManager.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
                {
                    if (pathIndex == MyAStarManager.finishLineIndex)
                    {
                        followingPath = false;
                        hasFinishedPath = true;
                        Debug.Log("FinishedPath");
                        break;
                    }
                    else
                        pathIndex++;
                }
                if (followingPath)
                {
                    targetRotation = Quaternion.LookRotation(MyAStarManager.lookPoints[pathIndex] - transform.position);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * MyAIManager.rotSpeed);
                    transform.Translate(Vector3.forward * Time.deltaTime * realMovSpeed, Space.Self);
                }
                yield return null;
            }
        }
        else
        {
            StopAllCoroutines();
            hasFinishedPath = false;
            followingPath = false;
            processingPath = true;
            BuildNode();
        }
    }
    #endregion
    #region AIs
    IEnumerator waitForEndOfAnimation()
    {
        while (MyAIManager.EnemyState != aiManager.enemyState.Roam)
        {
            yield return new WaitForEndOfFrame();
        }
        MyAIManager.EnemyState = aiManager.enemyState.Roam;
    }
    public virtual void attack(PlayerObject player) //For damage logic and unique effects
    {
        Debug.Log("AttackingPlayer");
        MyAIManager.EnemyState = aiManager.enemyState.Attack;
        player.takeDamage();
    }
    public virtual void hitBarricade() //Doesn't get run by Dracula or Blob
    {
        MyAIManager.EnemyState = aiManager.enemyState.Attack;
        targBar.Open();
    }
    public virtual void VanishEnemy()
    {
        MyAIManager.EnemyState = aiManager.enemyState.Vanish;
        MyAnimations.anim.CrossFade("VanishAnim", 0.2f); //Animation MUST be named this
    }
    public virtual void ReturnEnemy()
    {
        MyAnimations.anim.CrossFade("ReturnAnim", 0.2f); //Animation MUST be named this
        StartCoroutine(waitForEndOfAnimation());
    }
    public virtual void FinishHit()
    {
        MyAIManager.EnemyState = aiManager.enemyState.Roam;
        targBar = null;
        BuildNode();
    }
    public virtual void StartStun(float stunTime)
    {
        stunTimer = stunTime;
        MyAIManager.EnemyState = aiManager.enemyState.Stunned;
    }
    #endregion
    public enum personality
    {
        Stealthy, //higher weighting against Open Areas
        Seeker, //higher weighting against Corridors
        Aggressive, //higher speed in Corridors
        Cocky, //higher speed in Open Areas
        Null
    };
}
[System.Serializable]
public struct weightedAngles
{
    [Header("CLOCKWISE, STARTING AT 0 IN FRONT OF PLAYER")]
    [Range(0, 7)]
    public int nodeNum;
    [Header("HIGHER IS BETTER, ONLY 1 EIGHT")]
    [Range(0, 8)]
    public int nodeWeighting;
}