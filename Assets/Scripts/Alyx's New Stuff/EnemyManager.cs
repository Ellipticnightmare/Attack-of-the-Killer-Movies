using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyManager : MonoBehaviour
{
    public GameObject[] monsterArray;
    public Transform[] spawnArray;
    List<GameObject> activeMonsters = new List<GameObject>();
    List<Transform> activeSpawns = new List<Transform>();
    public PlayerObject activePlayer, flashTarget;
    public static EnemyManager instance;
    public sandstormManager Sandstorm;
    public ivyManager Ivy;
    public balloonManager Balloons;

    private void Awake()
    {
        StartCoroutine(spawnEnemies());
        instance = this;
    }
    public void triggerUniqueSpawn(MonsterController inEnemy)
    {
        switch (inEnemy.Enemy)
        {
            case MonsterController.enemy.Mummy:
                StartCoroutine(createSandstorm(FindObjectOfType<CameraHandler>().targetTransform.position));
                break;
            case MonsterController.enemy.SwampMonster:
                StartCoroutine(createIvyPath(inEnemy.transform.position));
                break;
            case MonsterController.enemy.Clown:
                DropBalloonAnimal(inEnemy);
                break;
        }
    }
    public void DropBalloonAnimal(MonsterController inEnemy)
    {
        Vector3 position = inEnemy.transform.position;
        float dist = Mathf.Infinity;
        bool isValidPoint = false;
        foreach (var item in Balloons.balloons)
        {
            if (Vector3.Distance(position, item) <= dist)
            {
                dist = Vector3.Distance(position, item);
                if (dist > Balloons.balloonSpawnMin)
                    isValidPoint = true;
            }
        }
        if (isValidPoint)
        {
            GameObject newBalloon = Instantiate(Balloons.balloon, findRandomPoint(position, 0, .1f), Quaternion.identity);
            Balloons.balloons.Add(newBalloon.transform.position);
            newBalloon.GetComponent<Balloon>().Clown = inEnemy;
            Balloons.balloonCount++;
        }
    }

    public bool isPlayerActive(PlayerObject checkPlayer)
    {
        bool output = checkPlayer.isFocus;
        return output;
    }
    public Vector3 findRandomPoint(Vector3 startingPoint, float minRange, float maxRange)
    {
        Vector3 output = Vector3.zero;
        bool isValid = false;
        while (!isValid)
        {
            Vector3 randomDirection = Random.insideUnitSphere * maxRange;
            randomDirection += startingPoint;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, maxRange, 1))
            {
                if (Vector3.Distance(startingPoint, hit.position) >= minRange)
                {
                    isValid = true;
                    output = hit.position;
                }
            }
        }
        return output;
    }
    public Vector3 returnPlayerAttackAngle(PlayerObject checkPlayer, Jericho enemy)
    {
        Vector3 output = checkPlayer.transform.position;
        if (Vector3.Distance(checkPlayer.transform.position, enemy.transform.position) >= enemy.MyAIManager.aggroRange)
        {
            List<weightedAngles> myPreferredAngles = new List<weightedAngles>();
            List<Vector3> targPositions = new List<Vector3>();
            int weightMax = 0;
            for (int i = 0; i < enemy.MyAIManager.myPreferredAngles.Count; i++)
            {
                weightedAngles newAngle = new weightedAngles();
                newAngle.nodeNum = enemy.MyAIManager.myPreferredAngles[i].nodeNum;
                weightMax = enemy.MyAIManager.myPreferredAngles[i].nodeWeighting + weightMax;
                newAngle.nodeWeighting = weightMax;
                myPreferredAngles.Add(newAngle);
            }
            int weightCheck = Random.Range(0, weightMax + 1);
            int nodeOut = 0;
            for (int i = 0; i < myPreferredAngles.Count; i++)
            {
                if (myPreferredAngles[i].nodeWeighting >= weightCheck)
                {
                    if (i > 0)
                    {
                        if (myPreferredAngles[i - 1].nodeWeighting < weightCheck)
                            nodeOut = myPreferredAngles[i].nodeNum;
                    }
                }
            }
            float radius = 1;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8;
                Vector3 newPos = checkPlayer.transform.position + (new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle * radius)));
                targPositions.Add(newPos);
            }
            output = targPositions[nodeOut];
        }
        return output;
    }
    IEnumerator spawnEnemies()
    {
        int x = 0;
        while (activeSpawns.Count < 3)
        {
            x = Random.Range(0, spawnArray.Length);
            if (!activeSpawns.Contains(spawnArray[x]))
                activeSpawns.Add(spawnArray[x]);
        }
        yield return new WaitForEndOfFrame();
        while (activeMonsters.Count < 3)
        {
            x = Random.Range(0, monsterArray.Length);
            if (!activeMonsters.Contains(monsterArray[x]))
                activeMonsters.Add(monsterArray[x]);
        }
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            Instantiate(activeMonsters[i], activeSpawns[i]);
        }
        yield return new WaitForEndOfFrame();
    }
    IEnumerator createSandstorm(Vector3 playerPoint)
    {
        GameObject newSandstorm = Instantiate(Sandstorm.sandstorm, findRandomPoint(playerPoint, Sandstorm.sandStormSpawnMin, Sandstorm.sandStormSpawnMax), Quaternion.identity);
        yield return new WaitForSeconds(Sandstorm.sandstormTimer);
        Destroy(newSandstorm);
    }
    IEnumerator createIvyPath(Vector3 enemyPoint)
    {
        if (Ivy.ivyCount < Ivy.ivyMax)
        {
            GameObject newIvyPatch = Instantiate(Ivy.ivy, findRandomPoint(enemyPoint, Ivy.ivySpawnMin, Ivy.ivySpawnMax), Quaternion.identity);
            Ivy.ivyCount++;
            yield return new WaitForSeconds(Ivy.ivyTimer);
            Destroy(newIvyPatch);
            Ivy.ivyCount--;
        }
        else
            yield return new WaitForEndOfFrame();
    }
}
[System.Serializable]
public class sandstormManager
{
    public GameObject sandstorm;
    public float sandstormTimer, sandStormSpawnMax, sandStormSpawnMin;
    public bool exists;
}
[System.Serializable]
public class ivyManager
{
    public GameObject ivy;
    public float ivyTimer, ivySpawnMax, ivySpawnMin;
    public int ivyCount, ivyMax;
}
[System.Serializable]
public class balloonManager
{
    public GameObject balloon;
    public int balloonCount, balloonMax;
    public float balloonSpawnMin;
    public List<Vector3> balloons = new List<Vector3>();
}
[System.Serializable]
public class animationManager
{
    public Animator anim;
    [Header("DISABLE THIS UNTIL ANIMATIONS")]
    public bool hasAnimations;
}
[System.Serializable]
public class aiManager
{
    public float viewConeRadius = 25;
    [Range(0, 360)]
    public float viewConeAngle = 190;
    public List<weightedAngles> myPreferredAngles = new List<weightedAngles>();
    [Range(5, 20)]
    public float aggroRange = 10;
    [Tooltip("This is ONLY the players")]
    public LayerMask targetMask;
    [Tooltip("This is everything BUT the players")]
    public LayerMask obstacleMask;
    public float moveSpeed = 3;
    public float rotSpeed = 100;
    public enemyState EnemyState = enemyState.Roam;
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
public class navmeshManager
{
    public float walkRadius = 4f;
    [HideInInspector]
    public List<webNodePoint> nodeWeb = new List<webNodePoint>();
    public int nodeMinDistance = 10;
    public int nodeWeightDistance = 20;
    [HideInInspector]
    public NavMeshAgent agent;
    [HideInInspector]
    public NavMeshPath path;
    [HideInInspector]
    public webNodePoint curNode, nextNode;
    [Range(8, 14)]
    public int maximumWebSize;
    [Range(0, 10)]
    public int roamingDesire;
    [Range(70, 90)]
    public int revisitThreshold;
}