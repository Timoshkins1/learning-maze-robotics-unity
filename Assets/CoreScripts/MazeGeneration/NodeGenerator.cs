using System.Collections.Generic;
using UnityEngine;

public class NodeGenerator
{
    private MazeGenerator generator;
    private List<GameObject> nodes;
    private GameObject nodesParent;

    public NodeGenerator(MazeGenerator mazeGenerator)
    {
        generator = mazeGenerator;
        nodes = new List<GameObject>();
    }

    public void CreateNodes()
    {
        if (generator.nodePrefab == null)
        {
            Debug.LogWarning("Node prefab не назначен! Ноды не будут созданы.");
            return;
        }

        Clear();
        nodesParent = new GameObject("MazeNodes");
        var mazeData = generator.GetMazeData();

        if (mazeData.Chunks == null)
        {
            Debug.LogError("MazeData не инициализирована! Сначала вызовите GenerateMaze()");
            return;
        }

        int totalNodes = 0;

        for (int chunkX = 0; chunkX < mazeData.MazeSizeInChunks.x; chunkX++)
        {
            for (int chunkZ = 0; chunkZ < mazeData.MazeSizeInChunks.y; chunkZ++)
            {
                // Проверяем существование чанка
                if (mazeData.Chunks[chunkX, chunkZ] == null)
                {
                    Debug.LogWarning($"Чанк [{chunkX}, {chunkZ}] не инициализирован");
                    continue;
                }

                for (int cellX = 0; cellX < mazeData.ChunkSize; cellX++)
                {
                    for (int cellZ = 0; cellZ < mazeData.ChunkSize; cellZ++)
                    {
                        CreateNode(chunkX, chunkZ, cellX, cellZ);
                        totalNodes++;
                    }
                }
            }
        }

        Debug.Log($"Создано нодов: {totalNodes} (чанков: {mazeData.MazeSizeInChunks.x * mazeData.MazeSizeInChunks.y}, ячеек: {mazeData.ChunkSize * mazeData.ChunkSize})");
    }

    private void CreateNode(int chunkX, int chunkZ, int cellX, int cellZ)
    {
        try
        {
            Vector3 nodePosition = generator.GetCellWorldPosition(chunkX, chunkZ, cellX, cellZ);
            GameObject node = Object.Instantiate(generator.nodePrefab, nodePosition, Quaternion.identity, nodesParent.transform);
            node.name = $"Node_Chunk({chunkX},{chunkZ})_Cell({cellX},{cellZ})";

            NodeInfo nodeInfo = node.GetComponent<NodeInfo>() ?? node.AddComponent<NodeInfo>();
            nodeInfo.SetCoordinates(chunkX, chunkZ, cellX, cellZ);

            nodes.Add(node);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка создания нода в [{chunkX},{chunkZ}] [{cellX},{cellZ}]: {e.Message}");
        }
    }

    public void Clear()
    {
        if (nodesParent != null)
        {
            if (Application.isPlaying)
                Object.Destroy(nodesParent);
            else
                Object.DestroyImmediate(nodesParent);
        }
        nodes.Clear();
    }
}