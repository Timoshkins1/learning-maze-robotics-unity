using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Настройки лабиринта")]
    public int chunkSize = 10;
    public Vector2Int mazeSizeInChunks = new Vector2Int(3, 3);
    [Tooltip("При включении использует правило правой руки, при выключении - полностью случайную генерацию")]
    public bool useRightHandRule = true;
    public bool createFinishArea = true;

    [Header("Настройки смещения")]
    public Vector3 chunkOffset = Vector3.zero;
    public Vector3 cellOffset = Vector3.zero;
    public Vector3 wallOffset = Vector3.zero;

    [Header("Префабы")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject finishPrefab;
    public GameObject nodePrefab;

    [Header("Настройки генерации")]
    public float cellSize = 2f;
    public float wallHeight = 3f;
    public float wallThickness = 0.1f;

    [Header("Камера")]
    public MazeCameraController cameraController;

    private MazeData mazeData;
    private MazeBuilder mazeBuilder;
    private NodeGenerator nodeGenerator;
    private bool isGenerating = false;

    void Start()
    {
        GenerateMaze();
    }

    private void InitializeComponents()
    {
        mazeData = new MazeData(chunkSize, mazeSizeInChunks);
        mazeBuilder = new MazeBuilder(mazeData, this);
        nodeGenerator = new NodeGenerator(this);

        if (cameraController == null)
            cameraController = FindObjectOfType<MazeCameraController>();
    }

    [ContextMenu("Сгенерировать лабиринт")]
    public void GenerateMaze()
    {
        if (isGenerating) return;

        StartCoroutine(GenerateMazeCoroutine());
    }

    private IEnumerator GenerateMazeCoroutine()
    {
        isGenerating = true;
        ClearExistingMaze();
        InitializeComponents();

        mazeData.Initialize();
        mazeBuilder.Generate();

        // Ждем один кадр перед созданием нодов
        yield return null;

        nodeGenerator.CreateNodes();

        if (cameraController != null)
            cameraController.UpdateCameraForNewMaze();

        Debug.Log($"Лабиринт сгенерирован: {mazeSizeInChunks.x}x{mazeSizeInChunks.y} чанков, {chunkSize} ячеек в чанке. Правило правой руки: {useRightHandRule}");
        isGenerating = false;
    }

    [ContextMenu("Очистить лабиринт")]
    private void ClearExistingMaze()
    {
        mazeBuilder?.Clear();
        nodeGenerator?.Clear();
    }

    public Vector3 GetCellWorldPosition(int chunkX, int chunkZ, int cellX, int cellY)
    {
        return new Vector3(
            chunkX * (chunkSize * cellSize + chunkOffset.x) + cellX * (cellSize + cellOffset.x) + wallOffset.x,
            wallOffset.y,
            chunkZ * (chunkSize * cellSize + chunkOffset.z) + cellY * (cellSize + cellOffset.z) + wallOffset.z
        );
    }

    public MazeData GetMazeData() => mazeData;

    public int GetTotalCellsX() => mazeSizeInChunks.x * chunkSize;
    public int GetTotalCellsZ() => mazeSizeInChunks.y * chunkSize;
    public float GetTotalWidth() => GetTotalCellsX() * cellSize + (mazeSizeInChunks.x - 1) * chunkOffset.x;
    public float GetTotalDepth() => GetTotalCellsZ() * cellSize + (mazeSizeInChunks.y - 1) * chunkOffset.z;
}