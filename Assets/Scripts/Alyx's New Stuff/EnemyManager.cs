using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    public GameObject[] monsterArray = new GameObject[8];
    public Transform[] spawnArray;
    List<GameObject> activeMonsters = new List<GameObject>();
    List<Transform> activeSpawns = new List<Transform>();
    public PlayerObject activePlayer, flashTarget;
    public static EnemyManager instance;
    public sandstormManager Sandstorm;
    public ivyManager Ivy;
    public balloonManager Balloons;
    [Header("A*Logic")]
    public Transform corner1, corner2;
    List<GameObject> pathFindNodes = new List<GameObject>();
    public int xRange, yRange, zRange;

    public float nodeRadius;
    public Node[,,] grid;
    public LayerMask unwalkableMask, environmentMask;

    public BoxCollider[] openAreasBounds, corridorBounds;
    public string enemy01, enemy02, enemy03, gameMode;
    public float difficultyCheck = 0;

    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;
    bool isProcessingPath;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(corner1.position.x, 1, corner2.position.z));
        if (grid != null)
            foreach (Node n in grid)
            {
                if (n.walkable)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * .9f);
                }
                if (n.obstructed)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * .9f);
                }
                if (n.raised)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * .9f);
                }
                if (n.corridor)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * .9f);
                }
                if (n.openArea)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * .9f);
                }
                /*if (path.Contains(n))
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * .9f);
                }*/
            }
    }

    private void Awake()
    {
        StartCoroutine(spawnEnemies());
        instance = this;
        CreateNodeGrid();
        gameMode = PlayerPrefs.GetString("gameMode");
        if (gameMode == "Custom")
        {
            enemy01 = PlayerPrefs.GetString("enemy01");
            enemy02 = PlayerPrefs.GetString("enemy02");
            enemy03 = PlayerPrefs.GetString("enemy03");
            difficultyCheck = PlayerPrefs.GetFloat("difficultyCheck");
        }
    }

    #region Pathfinding
    #region Grid
    public void CreateNodeGrid()
    {
        xRange = (int)(Mathf.Abs(corner1.position.x - corner2.position.x));
        yRange = (int)(Mathf.Abs(corner1.position.y - corner2.position.y));
        zRange = (int)(Mathf.Abs(corner2.position.z - corner1.position.z));
        grid = new Node[xRange, yRange, zRange];
        Vector3 worldBottomLeft = transform.position - Vector3.right * xRange / 2 - Vector3.forward * zRange / 2;
        worldBottomLeft = new Vector3(worldBottomLeft.z, corner2.position.y, worldBottomLeft.z);
        worldBottomLeft.y -= .1f;
        for (int x = 0; x < xRange; x++)
        {
            for (int y = 0; y < yRange; y++)
            {
                for (int z = 0; z < zRange; z++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * 1 + .5f) + Vector3.forward * (z * 1 + .5f) + Vector3.up * (y * .75f);
                    bool walkable = !(Physics.CheckSphere(worldPoint, 1f, unwalkableMask)) && Physics.CheckSphere(worldPoint, 1f);
                    bool obstructed = (Physics.CheckSphere(worldPoint, 1f, unwalkableMask));
                    if (y > 0)
                        walkable = grid[x, y - 1, z].walkable ? false : walkable;
                    walkable = walkable ? (Physics.Raycast(worldPoint, Vector3.down, 1f) && !Physics.CheckSphere(worldPoint, .01f, environmentMask)) : false;
                    bool raised = walkable ? y > 1 ? true : false : false;
                    bool openArea = false;
                    bool inCorridor = false;
                    int movementValue = 0;
                    if (walkable)
                    {
                        foreach (var item in openAreasBounds)
                        {
                            if (pointInBounds(worldPoint, item))
                                openArea = true;
                        }
                        foreach (var item in corridorBounds)
                        {
                            if (pointInBounds(worldPoint, item))
                                inCorridor = true;
                        }
                    }
                    else
                        movementValue = 10;
                    grid[x, y, z] = new Node(x, y, z, walkable, obstructed, raised, openArea, inCorridor, movementValue, worldPoint);
                }
            }
        }
        BlurPenaltyMap(3);
    }
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + xRange / 2) / xRange;
        float percentY = (worldPosition.y + yRange / 2) / yRange;
        float percentZ = (worldPosition.z + zRange / 2) / zRange;
        int x = Mathf.FloorToInt(Mathf.Clamp((xRange) * percentX, 0, xRange - 1));
        int y = Mathf.RoundToInt((yRange) * percentY);
        int z = Mathf.RoundToInt((zRange) * percentZ) + 1;
        return grid[x, y, z];
    }
    public bool pointInBounds(Vector3 point, BoxCollider box)
    {
        point = box.transform.InverseTransformPoint(point) - box.center;
        float halfX = box.size.x * .5f;
        float halfY = box.size.y * .5f;
        float halfZ = box.size.z * .5f;
        if (point.x < halfX && point.x > -halfX &&
            point.y < halfY && point.y > -halfY &&
            point.z < halfZ && point.z > -halfZ)
            return true;
        else
            return false;
    }
    public List<Node> GetNeighbors(Node node, int neighborRange)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -neighborRange; x <= neighborRange; x++)
        {
            for (int y = -neighborRange; y <= neighborRange; y++)
            {
                for (int z = -neighborRange; z <= neighborRange; z++)
                {
                    if (x == 0 && y == 0 && z == 0)
                        continue;
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    int checkZ = node.gridZ + z;

                    if (checkX >= 0 && checkX < xRange && checkY >= 0 && checkY < yRange && checkZ >= 0 && checkZ < zRange)
                        neighbours.Add(grid[checkX, checkY, checkZ]);
                }
            }
        }

        return neighbours;
    }
    void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = blurSize;

        int[,] penaltiesHorizontalPass = new int[xRange, zRange];
        int[,] penaltiesVerticalPass = new int[xRange, zRange];

        for (int y = 0; y < yRange; y++)
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += grid[sampleX, 0, y].movementPenalty;
            }
            for (int x = 0; x < xRange; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, xRange);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, xRange - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, 0, y].movementPenalty + grid[addIndex, 0, y].movementPenalty;
            }
        }
        for (int x = 0; x < xRange; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }
            for (int y = 0; y < yRange; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, yRange);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, yRange - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                int blurredPenalty = Mathf.RoundToInt((float)(penaltiesVerticalPass[x, y] / (kernelSize * kernelSize)));
                grid[x, 0, y].movementPenalty = blurredPenalty;
            }
        }
    }
    #endregion
    #region Heap
    public int MaxSize
    {
        get
        {
            return xRange * yRange * zRange;
        }
    }
    #endregion
    #region Logic
    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, Jericho enemy)
    {
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);
        if (targetNode.walkable)
        {

            Heap<Node> openSet = new Heap<Node>(MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);
            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);
                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;//We've found our path!
                }

                foreach (Node neighbour in GetNeighbors(currentNode, 1))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }
                    switch (enemy.Personality)
                    {
                        case Jericho.personality.Stealthy:
                            if (neighbour.openArea)
                                neighbour.movementPenalty = 6;
                            break;
                        case Jericho.personality.Seeker:
                            if (neighbour.corridor)
                                neighbour.movementPenalty = 6;
                            break;
                    }
                    Jericho[] enemies = FindObjectsOfType<Jericho>();
                    foreach (var item in enemies)
                    {
                        if (item != enemy)
                        {
                            foreach (var item2 in GetNeighbors(NodeFromWorldPoint(item.transform.position), 1))
                            {
                                item2.movementPenalty = 10;
                            }
                        }
                    }
                    BlurPenaltyMap(3);
                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;
                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }
        }
        yield return null;
        if (pathSuccess)
            waypoints = RetracePath(startNode, targetNode);
        FinishedProcessingPath(waypoints, pathSuccess);

        foreach (var item in grid)
        {
            if (item.movementPenalty > 0 && !item.obstructed)
                item.movementPenalty = 0;
        }
    }
    Vector3[] RetracePath(Node startNode, Node endNode)
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
    Vector3[] SimplifyPath(List<Node> path)
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
    int GetDistance(Node nodeA, Node nodeB)
    {
        int output = 0;
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);

        if (dstX > dstZ)
            output = 14 * dstZ + 10 * (dstX - dstZ);
        output = 14 * dstX + 10 * (dstZ - dstX);
        return output - dstY;
    }
    public void RequestPath(Vector3 pathStart, Vector3 pathEnd, System.Action<Vector3[], bool> callBack, Jericho enemy)
    {
        pathRequestQueue.Enqueue(new PathRequest(pathStart, pathEnd, callBack, enemy));
        TryProcessNext();
    }
    void TryProcessNext()
    {
        Debug.Log("trying process next");
        if (!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd, currentPathRequest.enemy);
        }
    }
    void FinishedProcessingPath(Vector3[] path, bool success)
    {
        currentPathRequest.callBack(path, success);
        isProcessingPath = false;
        TryProcessNext();
    }
    void StartFindPath(Vector3 startPos, Vector3 targetPos, Jericho enemy)
    {
        Debug.Log("tryign to find path");
        StartCoroutine(FindPath(startPos, targetPos, enemy));
    }
    #endregion
    #endregion

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
        List<weightedAngles> myPreferredAngles = new List<weightedAngles>();
        Vector3 output = checkPlayer.transform.position;
        if (enemy.MyAIManager.myPreferredAngles.Count > 0)
        {
            if (Vector3.Distance(checkPlayer.transform.position, enemy.transform.position) >= enemy.MyAIManager.aggroRange)
            {
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
            int cA = myPreferredAngles[0].nodeNum;
            int dst = 10;
            switch (cA)
            {
                case 0:
                    dst = 0;
                    break;
                case 1:
                    dst = 4;
                    break;
                case 2:
                    dst = 7;
                    break;
                case 6:
                    dst = 7;
                    break;
                case 7:
                    dst = 4;
                    break;
            }
            WeighNodesInFrontOfPlayer(checkPlayer.transform, enemy, dst);
        }
        return output;
    }
    public void WeighNodesInFrontOfPlayer(Transform inTransform, Jericho enemy, int dist)
    {
        RaycastHit hit;
        Vector3 lastPoint = Vector3.zero;
        if (Physics.Raycast(inTransform.position, inTransform.forward, out hit, dist))
            lastPoint = hit.point;
        else
            lastPoint = inTransform.forward * dist;
        float distance = Vector3.Distance(inTransform.position, lastPoint);
        int howManyPoints = (int)dist / 1;
        Vector3 direction = inTransform.forward.normalized;
        Node[] points = new Node[howManyPoints];
        for (int i = 0; i < howManyPoints; i++)
        {
            points[i] = NodeFromWorldPoint(inTransform.forward * howManyPoints);
        }
        List<Node> checkedPoints = new List<Node>();
        foreach (var item in points)
        {
            if (item != points[0])
                checkedPoints.AddRange(GetNeighbors(item, 2));
            else
                checkedPoints.AddRange(GetNeighbors(item, 1));
        }
        checkedPoints = checkedPoints.Distinct().ToList();
        foreach (var item in checkedPoints)
        {
            int newSum = (item.corridor && enemy.Personality == Jericho.personality.Seeker) ? 4 : (item.openArea && enemy.Personality == Jericho.personality.Stealthy) ? 4 : 0;
            item.movementPenalty = 10 + newSum;
        }
        BlurPenaltyMap(2);
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
                while (activeMonsters.Count < 3)
                {
                    x = Random.Range(0, monsterArray.Length);
                    if (!activeMonsters.Contains(monsterArray[x]))
                        activeMonsters.Add(monsterArray[x]);
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
        targetBarricades,
        Attack,
        Hunting, //Only triggers on difficulty 15
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
    public int maximumWebSize = 8;
    [Range(0, 10)]
    public int roamingDesire = 0;
    [Range(70, 90)]
    public int revisitThreshold = 70;
}
public class Node : IHeapItem<Node>
{
    public bool walkable, obstructed, raised, openArea, corridor;
    public Vector3 worldPosition;
    public int gridX, gridY, gridZ, heapIndex, movementPenalty;

    public int gCost; //how far away the node is from the starting node
    public int hCost; //how far away the node is from the ending node

    public Node parent;
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public Node(int _gridX, int _gridY, int _gridZ, bool _walkable, bool _obstructed, bool _raised, bool _openArea, bool _corridor, int _penalty, Vector3 _worldPosition)
    {
        gridX = _gridX;
        gridY = _gridY;
        gridZ = _gridZ;
        walkable = _walkable;
        obstructed = _obstructed;
        raised = _raised;
        openArea = _openArea;
        corridor = _corridor;
        movementPenalty = _penalty;
        worldPosition = _worldPosition;
    }
    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }
    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }
}
public class Heap<T> where T : IHeapItem<T>
{
    T[] items;
    int currentItemCount;
    public Heap(int maxHeapSize)
    {
        items = new T[maxHeapSize];
    }
    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }
    public void UpdateItem(T item)
    {
        SortUp(item);
    }
    public T RemoveFirst()
    {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }
    public bool Contains(T item)
    {
        return Equals(items[item.HeapIndex], item);
    }
    public int Count
    {
        get
        {
            return currentItemCount;
        }
    }
    void SortDown(T item)
    {
        while (true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;
            if (childIndexLeft < currentItemCount)
            {
                swapIndex = childIndexLeft;

                if (childIndexRight < currentItemCount)
                {
                    if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                    {
                        swapIndex = childIndexRight;
                    }
                }
                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else
                    return;
            }
            else return;
        }
    }
    void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;
        while (true)
        {
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0)
            {
                Swap(item, parentItem);
            }
            else
                break;
            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }
    void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}
public interface IHeapItem<T> : System.IComparable<T>
{
    int HeapIndex
    {
        get;
        set;
    }
}
public struct PathRequest
{
    public Vector3 pathStart;
    public Vector3 pathEnd;
    public System.Action<Vector3[], bool> callBack;
    public Jericho enemy;

    public PathRequest(Vector3 _start, Vector3 _end, System.Action<Vector3[], bool> _callBack, Jericho _enemy)
    {
        pathStart = _start;
        pathEnd = _end;
        callBack = _callBack;
        enemy = _enemy;
    }
}
public struct Line
{
    const float verticalLineGradient = 1e5f;
    float gradient, y_intercept, gradientPerpendicular;
    Vector2 pointOnLine_1, pointOnLine_2;
    bool approachSide;

    public Line(Vector2 pointOnLine, Vector2 pointPerpendicularToLine)
    {
        float dx = pointOnLine.x - pointPerpendicularToLine.x;
        float dy = pointOnLine.y - pointPerpendicularToLine.y;
        if (dx == 0)
            gradientPerpendicular = verticalLineGradient;
        else
            gradientPerpendicular = dy / dx;

        if (gradientPerpendicular == 0)
            gradient = verticalLineGradient;
        else
            gradient = -1 / gradientPerpendicular;

        y_intercept = pointOnLine.y - gradient * pointOnLine.x;
        pointOnLine_1 = pointOnLine;
        pointOnLine_2 = pointOnLine_1 + new Vector2(1, gradient);
        approachSide = false;
        approachSide = GetSide(pointPerpendicularToLine);
    }
    bool GetSide(Vector2 p)
    {
        return (p.x - pointOnLine_1.x) * (pointOnLine_2.y - pointOnLine_1.y) > (p.y - pointOnLine_1.y) * (pointOnLine_2.x - pointOnLine_1.x);
    }
    public bool HasCrossedLine(Vector2 p)
    {
        return GetSide(p) != approachSide;
    }
}
public class Path
{
    public readonly Vector3[] lookPoints;
    public readonly Line[] turnBoundaries;
    public readonly int finishLineIndex;

    public Path(Vector3[] waypoints, Vector3 startPos, float turnDst)
    {
        lookPoints = waypoints;
        turnBoundaries = new Line[lookPoints.Length];
        finishLineIndex = turnBoundaries.Length - 1;

        Vector2 previousPoint = V3ToV2(startPos);
        for (int i = 0; i < lookPoints.Length; i++)
        {
            Vector2 currentPoint = V3ToV2(lookPoints[i]);
            Vector2 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
            Vector2 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
            turnBoundaries[i] = new Line(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
            previousPoint = turnBoundaryPoint;
        }
    }
    Vector2 V3ToV2(Vector3 v3)
    {
        return new Vector2(v3.x, v3.z);
    }
}
#endregion