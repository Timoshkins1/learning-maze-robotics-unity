using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LidarPoint
{
    public string name;
    public Transform pointTransform;
    public bool enabled = true;

    // 360° лидар
    public bool enable360Lidar = true;
    public float lidar360Range = 10f;
    public int lidar360Points = 360;

    // 90° лидар
    public bool enable90Lidar = true;
    public float lidar90Range = 10f;
    public int lidar90Points = 90;
    public float lidar90Angle = 90f;

    // Одиночные лидары - теперь только одна сторона
    public bool enableSingleLidar = true;
    public float singleLidarRange = 10f;

    // Направление одиночного лидара
    public enum SingleLidarDirection
    {
        Forward,    // Вперед
        Right,      // Вправо
        Backward,   // Назад
        Left        // Влево
    }

    public SingleLidarDirection singleLidarDirection = SingleLidarDirection.Forward;

    // Результаты
    [HideInInspector] public float[] lidar360Results;
    [HideInInspector] public float[] lidar90Results;
    [HideInInspector] public float singleLidarResult; // Теперь одно значение вместо массива
}

public class LidarController : MonoBehaviour
{
    [Header("Точки лидаров")]
    public List<LidarPoint> lidarPoints = new List<LidarPoint>();

    [Header("Общие настройки")]
    public LayerMask wallLayerMask = 1 << 8; // Слой "Wall"

    [Header("Отладка")]
    public bool showDebugRays = true;
    public Color debugColor360 = Color.cyan;
    public Color debugColor90 = Color.yellow;
    public Color debugColorSingle = Color.green;

    void Start()
    {
        InitializeAllLidars();
    }

    void Update()
    {
        ScanAllLidarPoints();

        if (showDebugRays)
        {
            DrawAllDebugRays();
        }
    }

    private void InitializeAllLidars()
    {
        foreach (var point in lidarPoints)
        {
            if (point.enabled)
            {
                InitializeLidarPoint(point);
            }
        }
    }

    private void InitializeLidarPoint(LidarPoint point)
    {
        if (point.enable360Lidar)
        {
            point.lidar360Results = new float[point.lidar360Points];
        }

        if (point.enable90Lidar)
        {
            point.lidar90Results = new float[point.lidar90Points];
        }

        // Одиночный лидар теперь инициализирует одно значение
        point.singleLidarResult = point.singleLidarRange;
    }

    private void ScanAllLidarPoints()
    {
        foreach (var point in lidarPoints)
        {
            if (point.enabled && point.pointTransform != null)
            {
                ScanLidarPoint(point);
            }
        }
    }

    private void ScanLidarPoint(LidarPoint point)
    {
        Vector3 origin = point.pointTransform.position;
        Transform referenceTransform = point.pointTransform;

        if (point.enable360Lidar)
        {
            Scan360Lidar(point, origin);
        }

        if (point.enable90Lidar)
        {
            Scan90Lidar(point, origin, referenceTransform);
        }

        if (point.enableSingleLidar)
        {
            ScanSingleLidar(point, origin, referenceTransform);
        }
    }

    private void Scan360Lidar(LidarPoint point, Vector3 origin)
    {
        float angleStep = 360f / point.lidar360Points;

        for (int i = 0; i < point.lidar360Points; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, point.lidar360Range, wallLayerMask))
            {
                point.lidar360Results[i] = hit.distance;
            }
            else
            {
                point.lidar360Results[i] = point.lidar360Range;
            }
        }
    }

    private void Scan90Lidar(LidarPoint point, Vector3 origin, Transform referenceTransform)
    {
        float halfAngle = point.lidar90Angle / 2f;
        float angleStep = point.lidar90Angle / (point.lidar90Points - 1);

        for (int i = 0; i < point.lidar90Points; i++)
        {
            float angle = -halfAngle + (i * angleStep);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * referenceTransform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, point.lidar90Range, wallLayerMask))
            {
                point.lidar90Results[i] = hit.distance;
            }
            else
            {
                point.lidar90Results[i] = point.lidar90Range;
            }
        }
    }

    private void ScanSingleLidar(LidarPoint point, Vector3 origin, Transform referenceTransform)
    {
        Vector3 direction = GetSingleLidarDirection(point, referenceTransform);
        point.singleLidarResult = ScanSingleRay(origin, direction, point.singleLidarRange);
    }

    private Vector3 GetSingleLidarDirection(LidarPoint point, Transform referenceTransform)
    {
        switch (point.singleLidarDirection)
        {
            case LidarPoint.SingleLidarDirection.Forward:
                return referenceTransform.forward;
            case LidarPoint.SingleLidarDirection.Right:
                return referenceTransform.right;
            case LidarPoint.SingleLidarDirection.Backward:
                return -referenceTransform.forward;
            case LidarPoint.SingleLidarDirection.Left:
                return -referenceTransform.right;
            default:
                return referenceTransform.forward;
        }
    }

    private float ScanSingleRay(Vector3 origin, Vector3 direction, float range)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, wallLayerMask))
        {
            return hit.distance;
        }
        return range;
    }

    // Методы для получения данных
    public LidarPoint GetLidarPoint(int index)
    {
        if (index >= 0 && index < lidarPoints.Count)
        {
            return lidarPoints[index];
        }
        return null;
    }

    public LidarPoint GetLidarPointByName(string name)
    {
        foreach (var point in lidarPoints)
        {
            if (point.name == name)
            {
                return point;
            }
        }
        return null;
    }

    public float[] Get360LidarData(int pointIndex)
    {
        var point = GetLidarPoint(pointIndex);
        return point?.lidar360Results;
    }

    public float[] Get90LidarData(int pointIndex)
    {
        var point = GetLidarPoint(pointIndex);
        return point?.lidar90Results;
    }

    // Новый метод для получения одного значения одиночного лидара
    public float GetSingleLidarDistance(int pointIndex)
    {
        var point = GetLidarPoint(pointIndex);
        return point != null ? point.singleLidarResult : -1f;
    }

    // Метод для получения направления одиночного лидара
    public string GetSingleLidarDirectionName(int pointIndex)
    {
        var point = GetLidarPoint(pointIndex);
        if (point != null)
        {
            return point.singleLidarDirection.ToString().ToLower();
        }
        return "unknown";
    }

    // Метод для получения данных одиночного лидара в формате для API
    public string GetSingleLidarDataJSON(int pointIndex)
    {
        var point = GetLidarPoint(pointIndex);
        if (point != null)
        {
            return $"{{\"direction\":\"{point.singleLidarDirection.ToString().ToLower()}\",\"distance\":{point.singleLidarResult:F2}}}";
        }
        return "{\"status\":\"error\",\"message\":\"Point not found\"}";
    }

    // Метод для получения минимального расстояния из всех точек
    public float GetGlobalMinDistance()
    {
        float minDistance = float.MaxValue;

        foreach (var point in lidarPoints)
        {
            if (!point.enabled) continue;

            float pointMin = GetPointMinDistance(point);
            if (pointMin < minDistance) minDistance = pointMin;
        }

        return minDistance < float.MaxValue ? minDistance : -1f;
    }

    private float GetPointMinDistance(LidarPoint point)
    {
        float minDistance = float.MaxValue;

        if (point.enable360Lidar && point.lidar360Results != null)
        {
            foreach (float dist in point.lidar360Results)
            {
                if (dist < minDistance) minDistance = dist;
            }
        }

        if (point.enable90Lidar && point.lidar90Results != null)
        {
            foreach (float dist in point.lidar90Results)
            {
                if (dist < minDistance) minDistance = dist;
            }
        }

        if (point.enableSingleLidar)
        {
            if (point.singleLidarResult < minDistance)
                minDistance = point.singleLidarResult;
        }

        return minDistance;
    }

    // Отладка - рисование лучей
    private void DrawAllDebugRays()
    {
        foreach (var point in lidarPoints)
        {
            if (point.enabled && point.pointTransform != null)
            {
                DrawPointDebugRays(point);
            }
        }
    }

    private void DrawPointDebugRays(LidarPoint point)
    {
        Vector3 origin = point.pointTransform.position;
        Transform refTransform = point.pointTransform;

        // 360° лидар
        if (point.enable360Lidar && point.lidar360Results != null)
        {
            float angleStep = 360f / point.lidar360Points;
            for (int i = 0; i < point.lidar360Points; i += 20)
            {
                float angle = i * angleStep;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                float distance = point.lidar360Results[i];

                if (distance < point.lidar360Range)
                {
                    Debug.DrawRay(origin, direction * distance, debugColor360);
                }
                else
                {
                    Debug.DrawRay(origin, direction * point.lidar360Range, debugColor360 * 0.3f);
                }
            }
        }

        // 90° лидар
        if (point.enable90Lidar && point.lidar90Results != null)
        {
            float halfAngle = point.lidar90Angle / 2f;
            float angleStep = point.lidar90Angle / (point.lidar90Points - 1);

            for (int i = 0; i < point.lidar90Points; i += 3)
            {
                float angle = -halfAngle + (i * angleStep);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * refTransform.forward;
                float distance = point.lidar90Results[i];

                if (distance < point.lidar90Range)
                {
                    Debug.DrawRay(origin, direction * distance, debugColor90);
                }
                else
                {
                    Debug.DrawRay(origin, direction * point.lidar90Range, debugColor90 * 0.3f);
                }
            }
        }

        // Одиночный лидар
        if (point.enableSingleLidar)
        {
            Vector3 direction = GetSingleLidarDirection(point, refTransform);
            float distance = point.singleLidarResult;

            if (distance < point.singleLidarRange)
            {
                Debug.DrawRay(origin, direction * distance, debugColorSingle);
            }
            else
            {
                Debug.DrawRay(origin, direction * point.singleLidarRange, debugColorSingle * 0.3f);
            }
        }
    }

    // API методы
    public string GetAllLidarDataJSON()
    {
        List<LidarPointData> pointDataList = new List<LidarPointData>();

        foreach (var point in lidarPoints)
        {
            if (!point.enabled) continue;

            pointDataList.Add(new LidarPointData
            {
                name = point.name,
                position = point.pointTransform != null ?
                    new Vector3Data(point.pointTransform.position) : new Vector3Data(),
                rotation = point.pointTransform != null ?
                    new Vector3Data(point.pointTransform.eulerAngles) : new Vector3Data(),
                lidar360 = point.enable360Lidar ? point.lidar360Results : null,
                lidar90 = point.enable90Lidar ? point.lidar90Results : null,
                singleLidar = point.enableSingleLidar ?
                    new SingleLidarData
                    {
                        direction = point.singleLidarDirection.ToString().ToLower(),
                        distance = point.singleLidarResult
                    } : null
            });
        }

        AllLidarData data = new AllLidarData
        {
            points = pointDataList.ToArray(),
            globalMinDistance = GetGlobalMinDistance(),
            pointCount = pointDataList.Count
        };

        return JsonUtility.ToJson(data, true);
    }

    [System.Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }
        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
    }

    [System.Serializable]
    public class SingleLidarData
    {
        public string direction;
        public float distance;
    }

    [System.Serializable]
    public class LidarPointData
    {
        public string name;
        public Vector3Data position;
        public Vector3Data rotation;
        public float[] lidar360;
        public float[] lidar90;
        public SingleLidarData singleLidar;
    }

    [System.Serializable]
    public class AllLidarData
    {
        public LidarPointData[] points;
        public float globalMinDistance;
        public int pointCount;
    }

    [ContextMenu("Add New Lidar Point")]
    private void AddNewLidarPoint()
    {
        GameObject newPointObj = new GameObject($"LidarPoint_{lidarPoints.Count}");
        newPointObj.transform.SetParent(transform);
        newPointObj.transform.localPosition = Vector3.zero;

        LidarPoint newPoint = new LidarPoint
        {
            name = $"Point_{lidarPoints.Count}",
            pointTransform = newPointObj.transform,
            singleLidarDirection = LidarPoint.SingleLidarDirection.Forward
        };

        lidarPoints.Add(newPoint);
        InitializeLidarPoint(newPoint);
    }

    [ContextMenu("Reset All Lidars")]
    public void ResetAllLidars()
    {
        InitializeAllLidars();
    }
}