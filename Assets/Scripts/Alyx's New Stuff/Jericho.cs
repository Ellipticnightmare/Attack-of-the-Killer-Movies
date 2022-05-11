using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Jericho : MonoBehaviour
{
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
    public bool isStun, isTargetingBarricades, isHunting, isVanish;

    #region Pathfinding
    #region Generic
    public virtual void startHuntTrigger(Vector3 newTarget) //Can be manually triggered by Clown
    {
        MyNavMeshManager.agent.SetDestination(newTarget);
        targNoise = null;
        updateNodeWeight();
       if(MyAudio.hasSound)
        SFXManager.instance.PlaySound(MyAudio.mySound, this.transform.position);
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
        Collider[] targetsInViewRadius = Physics.OverlapSphere(this.transform.position, MyAIManager.viewConeRadius, MyAIManager.targetMask);
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - this.transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < MyAIManager.viewConeAngle / 2)
            {
                float dstToTarget = Vector3.Distance(this.transform.position, target.position);
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
                if (Vector3.Distance(item.transform.position, this.transform.position) < minDist)
                {
                    minDist = Vector3.Distance(item.transform.position, this.transform.position);
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
        Collider[] targetsInHearRadius = Physics.OverlapSphere(this.transform.position, MyAIManager.hearingRadius, MyAIManager.soundMask);
        float distCheck = MyAIManager.hearingRadius + 1;
        int x = -1;
        for (int i = 0; i < targetsInHearRadius.Length; i++)
        {
            float dstToTarget = Vector3.Distance(this.transform.position, targetsInHearRadius[i].transform.position);
            Vector3 dirToTarget = (targetsInHearRadius[i].transform.position - this.transform.position).normalized;
            if (!Physics.Raycast(this.transform.position, dirToTarget))
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
                MyNavMeshManager.agent.SetDestination(MyNavMeshManager.curNode.nodePoint);
            else
            {
                MyNavMeshManager.nodeWeb.Remove(MyNavMeshManager.curNode);
                BuildNode();
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
                    MyNavMeshManager.agent.SetDestination(targBar.transform.position);
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
        if (player == null)
            Debug.Break();
        Debug.Log("AttackingPlayer");
        MyAIManager.EnemyState = aiManager.enemyState.Attack;
        if (MyAnimations.hasAnimations)
            MyAnimations.anim.SetInteger("state", 3); //Attack anim
        player.takeDamage();
    }
    public virtual void hitBarricade() //Doesn't get run by Dracula or Blob
    {
        MyAIManager.EnemyState = aiManager.enemyState.Attack;
        targBar.Open();
    }
    public virtual void VanishEnemy()
    {
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
        targPlayer = null;
        BuildNode();
    }
    public virtual void StartStun(float stunTime)
    {
        stunTimer = stunTime;
        MyAIManager.EnemyState = aiManager.enemyState.Stunned;
    }
    #endregion
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