using UnityEngine;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    [Header("Настройки машинки")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;
    public float nodeProximityThreshold = 0.1f;

    [Header("Ссылки")]
    public GameObject carPrefab;
    public MazeGenerator mazeGenerator;

    private GameObject carInstance;
    private NodeInfo currentNode;
    private NodeInfo targetNode;
    private Vector3 targetPosition;
    private bool isMoving = false;

    // Направление машинки (0: вперед/Z+, 1: вправо/X+, 2: назад/Z-, 3: влево/X-)
    private int currentDirection = 0;
    private Vector2Int[] directionVectors = {
        Vector2Int.up,    // вперед (Z+)
        Vector2Int.right, // вправо (X+)
        Vector2Int.down,  // назад (Z-)
        Vector2Int.left   // влево (X-)
    };

    // Кэш для проверки стен
    private MazeData mazeData;
    private Dictionary<Vector2Int, NodeInfo> nodeMap;

    void Start()
    {
        // Ждем завершения генерации лабиринта
        Invoke(nameof(InitializeCar), 0.1f);
    }

    void Update()
    {
        HandleInput();
        MoveCar();
    }

    private void InitializeCar()
    {
        if (mazeGenerator == null)
        {
            mazeGenerator = FindObjectOfType<MazeGenerator>();
            if (mazeGenerator == null)
            {
                Debug.LogError("MazeGenerator не найден!");
                return;
            }
        }

        mazeData = mazeGenerator.GetMazeData();
        BuildNodeMap();
        SpawnCarAtStart();
    }

    private void BuildNodeMap()
    {
        nodeMap = new Dictionary<Vector2Int, NodeInfo>();

        NodeInfo[] allNodes = FindObjectsOfType<NodeInfo>();
        foreach (NodeInfo node in allNodes)
        {
            // ИСПРАВЛЕННЫЙ РАСЧЕТ КЛЮЧА:
            Vector2Int detailedKey = new Vector2Int(
                node.chunkX * mazeData.ChunkSize + node.cellX,
                node.chunkZ * mazeData.ChunkSize + node.cellZ
            );
            nodeMap[detailedKey] = node;
        }

        Debug.Log($"Построена карта нодов: {nodeMap.Count} нодов");
    }

    private void SpawnCarAtStart()
    {
        // Находим нод с координатами (0,0,0,0)
        Vector2Int startKey = new Vector2Int(0, 0); // (0 * chunkSize + 0, 0 * chunkSize + 0)

        Debug.Log($"Ищем нод с ключом: {startKey}");
        Debug.Log($"Всего нодов в карте: {nodeMap.Count}");

        foreach (var key in nodeMap.Keys)
        {
            Debug.Log($"Доступный нод: {key}");
        }

        if (nodeMap.ContainsKey(startKey))
        {
            currentNode = nodeMap[startKey];
            SpawnCarAtNode(currentNode);
            Debug.Log($"Найден стартовый нод: {startKey}");
        }
        else
        {
            Debug.LogWarning($"Нод с координатами {startKey} не найден. Использую первый доступный нод.");
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
                return;
            }
        }

        // Если не нашли в чанке (0,0), используем любой нод
        foreach (var pair in nodeMap)
        {
            currentNode = pair.Value;
            SpawnCarAtNode(currentNode);
            return;
        }

        Debug.LogError("Не найдено ни одного нода для спавна машинки!");
    }

    private void SpawnCarAtNode(NodeInfo node)
    {
        if (carPrefab == null)
        {
            Debug.LogError("Car prefab не назначен!");
            return;
        }

        Vector3 spawnPosition = node.transform.position + Vector3.up * 0.5f;
        carInstance = Instantiate(carPrefab, spawnPosition, Quaternion.identity);
        carInstance.name = "PlayerCar";

        // Устанавливаем начальное направление (вперед)
        currentDirection = 0;
        UpdateCarRotation();

        Debug.Log($"Машинка создана на ноде: Чанк({node.chunkX},{node.chunkZ}) Ячейка({node.cellX},{node.cellZ})");
    }

    private void HandleInput()
    {
        if (carInstance == null || currentNode == null || isMoving) return;

        // Управление поворотами
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TurnLeft();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            TurnRight();
        }
        // Управление движением
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
        if (carInstance == null || currentNode == null || isMoving) return;

        currentDirection = (currentDirection + 3) % 4;
        UpdateCarRotation();
        Debug.Log($"Поворот налево. Текущее направление: {GetDirectionName()}");
    }

    public void TurnRight()
    {
        if (carInstance == null || currentNode == null || isMoving) return;

        currentDirection = (currentDirection + 1) % 4;
        UpdateCarRotation();
        Debug.Log($"Поворот направо. Текущее направление: {GetDirectionName()}");
    }

    public void MoveForward()
    {
        if (carInstance == null || currentNode == null || isMoving) return;

        TryMoveInDirection(currentDirection);
    }

    public void MoveBackward()
    {
        if (carInstance == null || currentNode == null || isMoving) return;

        int backwardDirection = (currentDirection + 2) % 4;
        TryMoveInDirection(backwardDirection);
    }

    private void TryMoveInDirection(int direction)
    {
        Vector2Int moveDirection = directionVectors[direction];
        NodeInfo nextNode = GetNodeInDirection(moveDirection);

        if (nextNode != null && CanMoveToDirection(moveDirection))
        {
            StartMovementToNode(nextNode);
        }
        else
        {
            Debug.Log($"Движение {GetDirectionName(direction)} заблокировано стеной!");
        }
    }

    private void UpdateCarRotation()
    {
        if (carInstance == null) return;

        // Углы поворота для каждого направления (в градусах)
        float[] rotationAngles = { 0f, 90f, 180f, 270f };

        Quaternion targetRotation = Quaternion.Euler(0f, rotationAngles[currentDirection], 0f);
        carInstance.transform.rotation = targetRotation;
    }

    private string GetDirectionName(int direction = -1)
    {
        if (direction == -1) direction = currentDirection;

        string[] directionNames = { "Вперед", "Вправо", "Назад", "Влево" };
        return directionNames[direction];
    }

    private NodeInfo GetNodeInDirection(Vector2Int direction)
    {
        // Вычисляем глобальные координаты целевой ячейки
        int globalX = currentNode.chunkX * mazeData.ChunkSize + currentNode.cellX + direction.x;
        int globalZ = currentNode.chunkZ * mazeData.ChunkSize + currentNode.cellZ + direction.y;

        Vector2Int targetKey = new Vector2Int(globalX, globalZ);

        if (nodeMap.ContainsKey(targetKey))
        {
            return nodeMap[targetKey];
        }

        return null;
    }

    private bool CanMoveToDirection(Vector2Int direction)
    {
        MazeChunk currentChunk = mazeData.GetChunk(currentNode.chunkX, currentNode.chunkZ);
        if (currentChunk == null) return false;

        int cellX = currentNode.cellX;
        int cellZ = currentNode.cellZ;

        // Проверяем наличие стен в указанном направлении
        if (direction == Vector2Int.up) // Вперёд (Z+)
        {
            return !currentChunk.HorizontalWalls[cellX, cellZ + 1];
        }
        else if (direction == Vector2Int.down) // Назад (Z-)
        {
            return !currentChunk.HorizontalWalls[cellX, cellZ];
        }
        else if (direction == Vector2Int.right) // Вправо (X+)
        {
            return !currentChunk.VerticalWalls[cellX + 1, cellZ];
        }
        else if (direction == Vector2Int.left) // Влево (X-)
        {
            return !currentChunk.VerticalWalls[cellX, cellZ];
        }

        return false;
    }

    private void StartMovementToNode(NodeInfo targetNode)
    {
        this.targetNode = targetNode;
        targetPosition = targetNode.transform.position + Vector3.up * 0.5f;
        isMoving = true;
    }

    private void MoveCar()
    {
        if (!isMoving || carInstance == null) return;

        // Двигаем машинку к целевой позиции
        carInstance.transform.position = Vector3.MoveTowards(
            carInstance.transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // Проверяем достижение цели
        if (Vector3.Distance(carInstance.transform.position, targetPosition) <= nodeProximityThreshold)
        {
            carInstance.transform.position = targetPosition;
            currentNode = targetNode;
            isMoving = false;

            Debug.Log($"Машинка перемещена на нод: Чанк({currentNode.chunkX},{currentNode.chunkZ}) Ячейка({currentNode.cellX},{currentNode.cellZ})");
        }
    }

    // Метод для принудительной установки позиции машинки (например, при респавне)
    public void TeleportToNode(NodeInfo node)
    {
        if (carInstance != null && node != null)
        {
            currentNode = node;
            Vector3 newPosition = node.transform.position + Vector3.up * 0.5f;
            carInstance.transform.position = newPosition;
            isMoving = false;

            Debug.Log($"Машинка телепортирована на нод: Чанк({node.chunkX},{node.chunkZ}) Ячейка({node.cellX},{node.cellZ})");
        }
    }

    // Метод для получения текущей позиции машинки в координатах лабиринта
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
            // Визуализация текущего нода
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(currentNode.transform.position + Vector3.up * 0.5f, Vector3.one * 0.3f);

            // Визуализация пути движения
            if (isMoving && targetNode != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(carInstance.transform.position, targetNode.transform.position + Vector3.up * 0.5f);
            }

            // Визуализация текущего направления
            Gizmos.color = Color.blue;
            Vector3 direction = carInstance.transform.forward;
            Gizmos.DrawRay(carInstance.transform.position, direction * 1f);
        }
    }
}