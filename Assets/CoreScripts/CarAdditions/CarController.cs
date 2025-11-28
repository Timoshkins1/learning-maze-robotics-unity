using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    [Header("Настройки машинки")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 270f;
    public float nodeProximityThreshold = 0.1f;

    [Header("Анимация")]
    public float rotationAnimationTime = 0.07f;
    public float moveAnimationTime = 0.09f;

    [Header("Ссылки")]
    public GameObject carPrefab;
    public MazeGenerator mazeGenerator;

    private GameObject carInstance;
    private NodeInfo currentNode;
    private NodeInfo targetNode;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isRotating = false;
    private bool isInitialized = false;

    private int currentDirection = 0;
    private Vector2Int[] directionVectors = {
        Vector2Int.up,    // вперед (Z+)
        Vector2Int.right, // вправо (X+)
        Vector2Int.down,  // назад (Z-)
        Vector2Int.left   // влево (X-)
    };

    private MazeData mazeData;
    private Dictionary<Vector2Int, NodeInfo> nodeMap;
    private Coroutine currentMovementCoroutine;

    public void InitializeCar()
    {
        if (isInitialized) return;

        StartCoroutine(InitializeCarCoroutine());
    }

    private IEnumerator InitializeCarCoroutine()
    {
        Debug.Log("🚗 Initializing car...");

        if (mazeGenerator == null)
        {
            mazeGenerator = FindObjectOfType<MazeGenerator>();
            if (mazeGenerator == null)
            {
                Debug.LogError("MazeGenerator not found!");
                yield break;
            }
        }

        // Ждем пока данные лабиринта будут готовы
        yield return new WaitUntil(() => mazeGenerator.GetMazeData() != null);

        mazeData = mazeGenerator.GetMazeData();

        // Ждем пока все ноды будут созданы
        yield return new WaitUntil(() => {
            NodeInfo[] nodes = FindObjectsOfType<NodeInfo>();
            bool nodesReady = nodes.Length >= mazeData.TotalCellsX * mazeData.TotalCellsZ * 0.8f; // 80% нодов создано
            if (!nodesReady) Debug.Log($"⏳ Waiting for nodes... {nodes.Length}/{(mazeData.TotalCellsX * mazeData.TotalCellsZ)}");
            return nodesReady;
        });

        BuildNodeMap();
        SpawnCarAtStart();

        isInitialized = true;
        Debug.Log("✅ Car initialized successfully!");
    }

    void Update()
    {
        if (!isInitialized) return;

        HandleInput();
        DebugWallsAroundCar();
    }

    public bool IsCarReady()
    {
        return isInitialized && carInstance != null && currentNode != null;
    }

    private void BuildNodeMap()
    {
        nodeMap = new Dictionary<Vector2Int, NodeInfo>();

        NodeInfo[] allNodes = FindObjectsOfType<NodeInfo>();
        Debug.Log($"🔍 Building node map from {allNodes.Length} nodes...");

        foreach (NodeInfo node in allNodes)
        {
            Vector2Int detailedKey = new Vector2Int(
                node.chunkX * mazeData.ChunkSize + node.cellX,
                node.chunkZ * mazeData.ChunkSize + node.cellZ
            );

            if (!nodeMap.ContainsKey(detailedKey))
            {
                nodeMap[detailedKey] = node;
            }
            else
            {
                Debug.LogWarning($"⚠️ Duplicate node at {detailedKey}: {node.name}");
            }
        }

        Debug.Log($"📍 Node map built: {nodeMap.Count} unique nodes");

        // Валидация карты нодов
        ValidateNodeMap();
    }

    private void ValidateNodeMap()
    {
        int expectedNodes = mazeData.TotalCellsX * mazeData.TotalCellsZ;
        if (nodeMap.Count != expectedNodes)
        {
            Debug.LogError($"❌ Node map validation failed: Expected {expectedNodes}, got {nodeMap.Count}");

            // Поиск отсутствующих нодов
            for (int x = 0; x < mazeData.TotalCellsX; x++)
            {
                for (int z = 0; z < mazeData.TotalCellsZ; z++)
                {
                    Vector2Int key = new Vector2Int(x, z);
                    if (!nodeMap.ContainsKey(key))
                    {
                        Debug.LogWarning($"🔴 Missing node at global position: {key}");
                    }
                }
            }
        }
        else
        {
            Debug.Log("✅ Node map validated successfully");
        }
    }

    private void SpawnCarAtStart()
    {
        Vector2Int startKey = new Vector2Int(0, 0);

        if (nodeMap.ContainsKey(startKey))
        {
            currentNode = nodeMap[startKey];
            SpawnCarAtNode(currentNode);
            Debug.Log($"🚗 Car spawned at start node: {startKey}");
        }
        else
        {
            Debug.LogWarning($"Start node {startKey} not found. Looking for alternative...");
            FindAlternativeStartNode();
        }
    }

    private void FindAlternativeStartNode()
    {
        // Ищем любой нод в чанке (0,0)
        foreach (var pair in nodeMap)
        {
            int chunkX = pair.Key.x / mazeData.ChunkSize;
            int chunkZ = pair.Key.y / mazeData.ChunkSize;

            if (chunkX == 0 && chunkZ == 0)
            {
                currentNode = pair.Value;
                SpawnCarAtNode(currentNode);
                Debug.Log($"🚗 Car spawned at alternative node: {pair.Key}");
                return;
            }
        }

        // Если не нашли в чанке (0,0), используем первый доступный нод
        foreach (var pair in nodeMap)
        {
            currentNode = pair.Value;
            SpawnCarAtNode(currentNode);
            Debug.Log($"🚗 Car spawned at random node: {pair.Key}");
            return;
        }

        Debug.LogError("❌ No nodes found for car spawn!");
    }

    private void SpawnCarAtNode(NodeInfo node)
    {
        if (carPrefab == null)
        {
            Debug.LogError("❌ Car prefab not assigned!");
            return;
        }

        Vector3 spawnPosition = node.transform.position + Vector3.up * 0.5f;
        carInstance = Instantiate(carPrefab, spawnPosition, Quaternion.identity, transform);
        carInstance.name = "PlayerCar";

        currentDirection = 0;
        UpdateCarRotationImmediate();

        Debug.Log($"🎯 Car positioned at: Chunk({node.chunkX},{node.chunkZ}) Cell({node.cellX},{node.cellZ})");
    }

    private void HandleInput()
    {
        if (carInstance == null || currentNode == null || isMoving || isRotating) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TurnLeft();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            TurnRight();
        }
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveForward();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveBackward();
        }
    }

    public void TurnLeft()
    {
        if (!IsCarReady() || isMoving || isRotating) return;
        StartCoroutine(RotateCar(-1));
    }

    public void TurnRight()
    {
        if (!IsCarReady() || isMoving || isRotating) return;
        StartCoroutine(RotateCar(1));
    }

    public void MoveForward()
    {
        if (!IsCarReady() || isMoving || isRotating) return;
        TryMoveInDirection(currentDirection);
    }

    public void MoveBackward()
    {
        if (!IsCarReady() || isMoving || isRotating) return;
        TryMoveInDirection((currentDirection + 2) % 4);
    }

    private IEnumerator RotateCar(int directionChange)
    {
        if (isRotating) yield break;

        isRotating = true;

        int targetDirection = (currentDirection + directionChange + 4) % 4;
        float startAngle = carInstance.transform.eulerAngles.y;
        float targetAngle = targetDirection * 90f;

        float currentAngle = startAngle;
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        float elapsedTime = 0f;

        while (elapsedTime < rotationAnimationTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationAnimationTime;
            float newAngle = Mathf.LerpAngle(currentAngle, currentAngle + angleDifference, t);

            carInstance.transform.rotation = Quaternion.Euler(0f, newAngle, 0f);
            yield return null;
        }

        carInstance.transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        currentDirection = targetDirection;
        isRotating = false;

        Debug.Log($"🔄 Rotation completed. Direction: {GetDirectionName()}");
    }

    private void TryMoveInDirection(int direction)
    {
        Vector2Int moveDirection = directionVectors[direction];
        NodeInfo nextNode = GetNodeInDirection(moveDirection);

        if (nextNode != null && CanMoveToDirection(moveDirection))
        {
            if (currentMovementCoroutine != null)
                StopCoroutine(currentMovementCoroutine);

            currentMovementCoroutine = StartCoroutine(MoveToNodeCoroutine(nextNode));
        }
        else
        {
            Debug.Log($"🚫 Movement {GetDirectionName(direction)} blocked by wall! Current: {GetCurrentGlobalPosition()}, Direction: {moveDirection}");
        }
    }

    private IEnumerator MoveToNodeCoroutine(NodeInfo targetNode)
    {
        isMoving = true;
        this.targetNode = targetNode;
        targetPosition = targetNode.transform.position + Vector3.up * 0.5f;

        Vector3 startPosition = carInstance.transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            carInstance.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        carInstance.transform.position = targetPosition;
        currentNode = targetNode;
        isMoving = false;

        Debug.Log($"➡️ Moved to node: Chunk({currentNode.chunkX},{currentNode.chunkZ}) Cell({currentNode.cellX},{currentNode.cellZ})");
    }

    private NodeInfo GetNodeInDirection(Vector2Int direction)
    {
        Vector2Int currentGlobal = GetCurrentGlobalPosition();
        Vector2Int targetGlobal = currentGlobal + direction;

        if (nodeMap.ContainsKey(targetGlobal))
        {
            return nodeMap[targetGlobal];
        }

        Debug.LogWarning($"🔴 Node not found at global position: {targetGlobal}");
        return null;
    }

    private Vector2Int GetCurrentGlobalPosition()
    {
        return new Vector2Int(
            currentNode.chunkX * mazeData.ChunkSize + currentNode.cellX,
            currentNode.chunkZ * mazeData.ChunkSize + currentNode.cellZ
        );
    }

    private bool CanMoveToDirection(Vector2Int direction)
    {
        Vector2Int currentGlobal = GetCurrentGlobalPosition();
        Vector2Int targetGlobal = currentGlobal + direction;

        // Проверяем что целевая позиция существует
        if (!nodeMap.ContainsKey(targetGlobal))
        {
            Debug.LogWarning($"🎯 Target position doesn't exist: {targetGlobal}");
            return false;
        }

        // Проверяем стену с детальной отладкой асимметрии
        bool hasWall = mazeData.HasWallBetween(currentGlobal, targetGlobal);

        // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: смотрим стены в обе стороны
        bool wallForward = mazeData.CheckWallInDirection(currentGlobal, direction);
        bool wallBackward = mazeData.CheckWallInDirection(targetGlobal, -direction);

        if (wallForward != wallBackward)
        {
            Debug.LogError($"🚨 ASYMMETRIC WALL DETECTED!");
            Debug.LogError($"   {currentGlobal} -> {targetGlobal}: Forward={wallForward}, Backward={wallBackward}");
            Debug.LogError($"   This means the wall data is inconsistent!");

            // Показываем все стены вокруг обеих точек
            mazeData.DebugAllWallsAround(currentGlobal);
            mazeData.DebugAllWallsAround(targetGlobal);
        }

        if (hasWall)
        {
            Debug.Log($"🚫 MOVEMENT BLOCKED: {currentGlobal} -> {targetGlobal}");
            DrawDebugLine(currentGlobal, targetGlobal, Color.red);
            return false;
        }
        else
        {
            Debug.Log($"✅ MOVEMENT ALLOWED: {currentGlobal} -> {targetGlobal}");
            DrawDebugLine(currentGlobal, targetGlobal, Color.green);
            return true;
        }
    }
    private void DebugWallsAroundCar()
    {
        if (currentNode == null) return;

        Vector2Int currentGlobal = GetCurrentGlobalPosition();

        // Визуализация стен вокруг машинки
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Color[] colors = { Color.blue, Color.cyan, Color.yellow, Color.magenta };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int target = currentGlobal + directions[i];
            if (nodeMap.ContainsKey(target))
            {
                bool hasWall = mazeData.HasWallBetween(currentGlobal, target);
                Color color = hasWall ? Color.red : Color.green;
                Debug.DrawLine(GetWorldPosition(currentGlobal), GetWorldPosition(target), color, 0.1f);
            }
        }
    }
    private int GetDirectionFromVector(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return 0;
        if (direction == Vector2Int.right) return 1;
        if (direction == Vector2Int.down) return 2;
        if (direction == Vector2Int.left) return 3;
        return 0;
    }

    private void DrawDebugLine(Vector2Int from, Vector2Int to, Color color)
    {
        Vector3 fromPos = GetWorldPosition(from);
        Vector3 toPos = GetWorldPosition(to);
        Debug.DrawLine(fromPos, toPos, color, 2f);
    }

    private Vector3 GetWorldPosition(Vector2Int globalPos)
    {
        if (nodeMap.ContainsKey(globalPos))
        {
            return nodeMap[globalPos].transform.position + Vector3.up * 0.5f;
        }
        return Vector3.zero;
    }
    private void UpdateCarRotationImmediate()
    {
        if (carInstance == null) return;

        float[] rotationAngles = { 0f, 90f, 180f, 270f };
        carInstance.transform.rotation = Quaternion.Euler(0f, rotationAngles[currentDirection], 0f);
    }

    private string GetDirectionName(int direction = -1)
    {
        if (direction == -1) direction = currentDirection;
        string[] directionNames = { "Вперед", "Вправо", "Назад", "Влево" };
        return directionNames[direction];
    }

    public void TeleportToNode(NodeInfo node)
    {
        if (carInstance != null && node != null)
        {
            if (currentMovementCoroutine != null)
                StopCoroutine(currentMovementCoroutine);

            currentNode = node;
            Vector3 newPosition = node.transform.position + Vector3.up * 0.5f;
            carInstance.transform.position = newPosition;
            isMoving = false;
            isRotating = false;

            Debug.Log($"Машинка телепортирована на нод: Чанк({node.chunkX},{node.chunkZ}) Ячейка({node.cellX},{node.cellZ})");
        }
    }

    public Vector2Int GetCurrentChunkCoordinates()
    {
        return currentNode != null ? new Vector2Int(currentNode.chunkX, currentNode.chunkZ) : Vector2Int.zero;
    }

    public Vector2Int GetCurrentCellCoordinates()
    {
        return currentNode != null ? new Vector2Int(currentNode.cellX, currentNode.cellZ) : Vector2Int.zero;
    }

    public string GetCurrentDirectionName()
    {
        return GetDirectionName();
    }

    void OnDrawGizmos()
    {
        if (currentNode != null && carInstance != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(currentNode.transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);

            if (isMoving && targetNode != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(carInstance.transform.position, targetNode.transform.position + Vector3.up * 0.5f);
            }

            Gizmos.color = Color.blue;
            Vector3 direction = carInstance.transform.forward;
            Gizmos.DrawRay(carInstance.transform.position, direction * 1f);
        }
    }
}