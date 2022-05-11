using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyManager : MonoBehaviour
{
    public GameObject[] monsterArray = new GameObject[8];
    public Transform[] spawnArray;
    List<GameObject> activeMonsters = new List<GameObject>();
    List<Transform> activeSpawns = new List<Transform>();
    [HideInInspector]
    public PlayerObject activePlayer, flashTarget;
    public static EnemyManager instance;
    public sandstormManager Sandstorm;
    public ivyManager Ivy;
    public balloonManager Balloons;
    public bool useSus;

    public string enemy01, enemy02, enemy03, gameMode;
    public float difficultyCheck = 0;

    private void Awake()
    {
        if(useSus)
        StartCoroutine(spawnEnemies());
        instance = this;
        gameMode = PlayerPrefs.GetString("gameMode");
        if (gameMode == "Custom")
        {
            enemy01 = PlayerPrefs.GetString("enemy01");
            enemy02 = PlayerPrefs.GetString("enemy02");
            enemy03 = PlayerPrefs.GetString("enemy03");
            difficultyCheck = PlayerPrefs.GetFloat("difficultyCheck");
        }
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
        //logic for spawning enemies here plz
        switch (gameMode)
        {
            case "Story":
                if (!useSus)
                {
                    while (activeMonsters.Count < 3)
                    {
                    x = Random.Range(0, monsterArray.Length);
                    if (!activeMonsters.Contains(monsterArray[x]))
                        activeMonsters.Add(monsterArray[x]);
                    }
                }
                else
                {
                    activeMonsters.Add(monsterArray[0]);
                    activeMonsters.Add(monsterArray[1]);
                    activeMonsters.Add(monsterArray[2]);
                }
                PlayerPrefs.SetString("enemy01", activeMonsters[0].name);
                PlayerPrefs.SetString("enemy02", activeMonsters[1].name);
                PlayerPrefs.SetString("enemy03", activeMonsters[2].name);
                yield return new WaitForEndOfFrame();
                difficultyCheck = 1;
                break;
            case "Custom":
                foreach (var item in monsterArray)
                {
                    if (item.name == enemy01 || item.name == enemy02 || item.name == enemy03)
                        activeMonsters.Add(item);
                }
                break;
            case "Survivor":
                x = Random.Range(0, monsterArray.Length);
                activeMonsters.Add(monsterArray[x]);
                difficultyCheck = 15;
                break;
        }
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < activeMonsters.Count; i++)
        {
            Instantiate(activeMonsters[i], activeSpawns[i]);
        }
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
#region Externals
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
    public float hearingRadius
    {
        get
        {
            return viewConeRadius * 1.5f;
        }
    }
    public float viewConeRadius = 13f;
    [Range(0, 360)]
    public float viewConeAngle = 190;
    public List<weightedAngles> myPreferredAngles = new List<weightedAngles>();
    [Range(5, 20)]
    public float aggroRange = 10;
    [Tooltip("This is ONLY the players")]
    public LayerMask targetMask;
    [Tooltip("This is everything BUT the players")]
    public LayerMask obstacleMask;
    [Tooltip("This is ONLY the sound layer")]
    public LayerMask soundMask;
    [Range(3, 5)]
    public float moveSpeed = 3;
    [Range(3, 20)]
    public float rotSpeed = 3;
    public float turnDst = 5;
    public enemyState EnemyState = enemyState.Roam;
    #region SurvivorDifficulty
    [HideInInspector]
    public int tensionIndex, patienceIndex, hotOrCold;
    #endregion
    public enum enemyState
    {
        Stunned,
        Roam,
        Chase,
        Attack,
        Hunting, //Only triggers on difficulty 15
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
    public int maximumWebSize = 8;
    [Range(0, 10)]
    public int roamingDesire = 0;
    [Range(70, 90)]
    public int revisitThreshold = 70;
}
#endregion