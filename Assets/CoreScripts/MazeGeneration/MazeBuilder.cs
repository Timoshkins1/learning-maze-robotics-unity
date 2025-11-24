using System.Collections.Generic;
using UnityEngine;

public class MazeBuilder
{
    private MazeData mazeData;
    private MazeGenerator generator;
    private MazeVisualizer visualizer;

    public MazeBuilder(MazeData data, MazeGenerator mazeGenerator)
    {
        mazeData = data;
        generator = mazeGenerator;
        visualizer = new MazeVisualizer(data, mazeGenerator);
    }

    public void Generate()
    {
        if (generator.createFinishArea)
        {
            CreateFinishArea();
        }

        GenerateMazeFromStartPoint();
        RemoveBoundaryWallsBetweenChunks(); // ВАЖНО: убираем стены между чанками
        visualizer.CreateMazeVisuals();
    }

    private void CreateFinishArea()
    {
        int centerChunkX = mazeData.StartGenerationChunk.x;
        int centerChunkZ = mazeData.StartGenerationChunk.y;
        int centerCellX = mazeData.StartGenerationCell.x;
        int centerCellY = mazeData.StartGenerationCell.y;

        mazeData.StartGenerationCells.Clear();
        var chunk = mazeData.Chunks[centerChunkX, centerChunkZ];

        for (int offsetX = 0; offsetX < 2; offsetX++)
        {
            for (int offsetY = 0; offsetY < 2; offsetY++)
            {
                int cellX = centerCellX - 1 + offsetX;
                int cellY = centerCellY - 1 + offsetY;

                if (cellX >= 0 && cellX < mazeData.ChunkSize && cellY >= 0 && cellY < mazeData.ChunkSize)
                {
                    mazeData.StartGenerationCells.Add(new Vector2Int(cellX, cellY));

                    // Убираем внутренние стены квадрата 2x2
                    if (cellX > centerCellX - 1) chunk.RemoveVerticalWall(cellX, cellY);
                    if (cellX < centerCellX) chunk.RemoveVerticalWall(cellX + 1, cellY);
                    if (cellY > centerCellY - 1) chunk.RemoveHorizontalWall(cellX, cellY);
                    if (cellY < centerCellY) chunk.RemoveHorizontalWall(cellX, cellY + 1);

                    chunk.Visited[cellX, cellY] = true;
                }
            }
        }
    }

    private void GenerateMazeFromStartPoint()
    {
        if (generator.createFinishArea)
        {
            foreach (var startCell in mazeData.StartGenerationCells)
            {
                GenerateMazeRecursive(mazeData.StartGenerationChunk.x, mazeData.StartGenerationChunk.y, startCell.x, startCell.y);
            }
        }
        else
        {
            GenerateMazeRecursive(mazeData.StartGenerationChunk.x, mazeData.StartGenerationChunk.y,
                                mazeData.StartGenerationCell.x, mazeData.StartGenerationCell.y);
        }

        EnsureAllCellsConnected();
    }

    private void GenerateMazeRecursive(int chunkX, int chunkZ, int x, int y)
    {
        var chunk = mazeData.GetChunk(chunkX, chunkZ);
        if (chunk == null) return;

        chunk.Visited[x, y] = true;

        Vector2Int[] directions = generator.useRightHandRule ?
            new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left } :
            new Vector2Int[] { Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

        ShuffleArray(directions);

        foreach (var direction in directions)
        {
            var newPos = GetNewCellPosition(chunkX, chunkZ, x, y, direction);

            if (mazeData.IsValidCell(newPos.chunkX, newPos.chunkZ, newPos.cellX, newPos.cellY))
            {
                RemoveWall(chunkX, chunkZ, x, y, newPos.chunkX, newPos.chunkZ, newPos.cellX, newPos.cellY, direction);

                // Помечаем новую клетку как посещённую перед рекурсией
                var newChunk = mazeData.GetChunk(newPos.chunkX, newPos.chunkZ);
                if (newChunk != null)
                {
                    newChunk.Visited[newPos.cellX, newPos.cellY] = true;
                    GenerateMazeRecursive(newPos.chunkX, newPos.chunkZ, newPos.cellX, newPos.cellY);
                }
            }
        }
    }

    private (int chunkX, int chunkZ, int cellX, int cellY) GetNewCellPosition(int chunkX, int chunkZ, int x, int y, Vector2Int direction)
    {
        int newChunkX = chunkX;
        int newChunkZ = chunkZ;
        int newX = x + direction.x;
        int newY = y + direction.y;

        if (newX < 0) { newChunkX--; newX = mazeData.ChunkSize - 1; }
        else if (newX >= mazeData.ChunkSize) { newChunkX++; newX = 0; }

        if (newY < 0) { newChunkZ--; newY = mazeData.ChunkSize - 1; }
        else if (newY >= mazeData.ChunkSize) { newChunkZ++; newY = 0; }

        return (newChunkX, newChunkZ, newX, newY);
    }

    private void RemoveWall(int chunkX, int chunkZ, int x, int y, int newChunkX, int newChunkZ, int newX, int newY, Vector2Int direction)
    {
        // Удаляем стену между текущей и новой клеткой
        if (chunkX == newChunkX && chunkZ == newChunkZ)
        {
            // Внутри одного чанка
            var chunk = mazeData.GetChunk(chunkX, chunkZ);
            if (chunk != null)
            {
                if (direction == Vector2Int.right) chunk.RemoveVerticalWall(x + 1, y);
                else if (direction == Vector2Int.left) chunk.RemoveVerticalWall(x, y);
                else if (direction == Vector2Int.up) chunk.RemoveHorizontalWall(x, y + 1);
                else if (direction == Vector2Int.down) chunk.RemoveHorizontalWall(x, y);
            }
        }
        else
        {
            // Межчанковый переход - удаляем граничные стены
            if (direction == Vector2Int.right)
            {
                // Из текущего чанка в правый
                var currentChunk = mazeData.GetChunk(chunkX, chunkZ);
                var rightChunk = mazeData.GetChunk(newChunkX, newChunkZ);
                if (currentChunk != null) currentChunk.RemoveVerticalWall(mazeData.ChunkSize, y);
                if (rightChunk != null) rightChunk.RemoveVerticalWall(0, newY);
            }
            else if (direction == Vector2Int.left)
            {
                // Из текущего чанка в левый
                var currentChunk = mazeData.GetChunk(chunkX, chunkZ);
                var leftChunk = mazeData.GetChunk(newChunkX, newChunkZ);
                if (currentChunk != null) currentChunk.RemoveVerticalWall(0, y);
                if (leftChunk != null) leftChunk.RemoveVerticalWall(mazeData.ChunkSize, newY);
            }
            else if (direction == Vector2Int.up)
            {
                // Из текущего чанка в верхний
                var currentChunk = mazeData.GetChunk(chunkX, chunkZ);
                var topChunk = mazeData.GetChunk(newChunkX, newChunkZ);
                if (currentChunk != null) currentChunk.RemoveHorizontalWall(x, mazeData.ChunkSize);
                if (topChunk != null) topChunk.RemoveHorizontalWall(newX, 0);
            }
            else if (direction == Vector2Int.down)
            {
                // Из текущего чанка в нижний
                var currentChunk = mazeData.GetChunk(chunkX, chunkZ);
                var bottomChunk = mazeData.GetChunk(newChunkX, newChunkZ);
                if (currentChunk != null) currentChunk.RemoveHorizontalWall(x, 0);
                if (bottomChunk != null) bottomChunk.RemoveHorizontalWall(newX, mazeData.ChunkSize);
            }
        }
    }

    private void RemoveBoundaryWallsBetweenChunks()
    {
        // Убираем внешние стены между чанками для проходимости
        for (int chunkX = 0; chunkX < mazeData.MazeSizeInChunks.x; chunkX++)
        {
            for (int chunkZ = 0; chunkZ < mazeData.MazeSizeInChunks.y; chunkZ++)
            {
                var chunk = mazeData.GetChunk(chunkX, chunkZ);
                if (chunk == null) continue;

                // Если есть сосед справа - убираем правую границу
                if (chunkX < mazeData.MazeSizeInChunks.x - 1)
                {
                    for (int y = 0; y < mazeData.ChunkSize; y++)
                    {
                        chunk.RemoveVerticalWall(mazeData.ChunkSize, y);
                    }
                }

                // Если есть сосед сверху - убираем верхнюю границу
                if (chunkZ < mazeData.MazeSizeInChunks.y - 1)
                {
                    for (int x = 0; x < mazeData.ChunkSize; x++)
                    {
                        chunk.RemoveHorizontalWall(x, mazeData.ChunkSize);
                    }
                }
            }
        }
    }

    private void EnsureAllCellsConnected()
    {
        // Простая реализация - проверяем и соединяем несвязанные области
        for (int chunkX = 0; chunkX < mazeData.MazeSizeInChunks.x; chunkX++)
        {
            for (int chunkZ = 0; chunkZ < mazeData.MazeSizeInChunks.y; chunkZ++)
            {
                var chunk = mazeData.GetChunk(chunkX, chunkZ);
                if (chunk == null) continue;

                for (int x = 0; x < mazeData.ChunkSize; x++)
                {
                    for (int y = 0; y < mazeData.ChunkSize; y++)
                    {
                        if (!chunk.Visited[x, y])
                        {
                            // Соединяем с соседней посещённой клеткой
                            ConnectToNearestVisited(chunkX, chunkZ, x, y);
                        }
                    }
                }
            }
        }
    }

    private void ConnectToNearestVisited(int chunkX, int chunkZ, int x, int y)
    {
        // Упрощённое соединение с ближайшей посещённой клеткой
        var directions = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        foreach (var direction in directions)
        {
            var newPos = GetNewCellPosition(chunkX, chunkZ, x, y, direction);
            var neighborChunk = mazeData.GetChunk(newPos.chunkX, newPos.chunkZ);

            if (neighborChunk != null && neighborChunk.Visited[newPos.cellX, newPos.cellY])
            {
                RemoveWall(chunkX, chunkZ, x, y, newPos.chunkX, newPos.chunkZ, newPos.cellX, newPos.cellY, direction);

                var currentChunk = mazeData.GetChunk(chunkX, chunkZ);
                if (currentChunk != null)
                {
                    currentChunk.Visited[x, y] = true;
                }
                break;
            }
        }
    }

    private void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
        }
    }

    public void Clear()
    {
        visualizer.Clear();
    }
}