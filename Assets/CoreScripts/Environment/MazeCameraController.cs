using UnityEngine;

public class MazeCameraController : MonoBehaviour
{
    [Header("Режим камеры")]
    public CameraViewMode viewMode = CameraViewMode.OrthographicTop;

    [Header("Настройки ортографической камеры")]
    public float orthographicPadding = 5f;
    public float minOrthographicSize = 10f;

    [Header("Настройки перспективной камеры")]
    public float perspectiveHeight = 30f;

    [Header("Ссылки")]
    public Camera mazeCamera;
    public MazeGenerator mazeGenerator;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalOrthographicSize;
    private bool originalOrthographic;

    public enum CameraViewMode
    {
        OrthographicTop,
        PerspectiveTop,
        Original
    }

    void Start()
    {
        if (mazeCamera == null)
            mazeCamera = Camera.main;

        if (mazeGenerator == null)
            mazeGenerator = FindObjectOfType<MazeGenerator>();

        SaveOriginalCameraState();
        SetupCameraView();
    }

    [ContextMenu("Настроить камеру на лабиринт")]
    public void SetupCameraView()
    {
        if (mazeGenerator == null || mazeCamera == null)
        {
            Debug.LogWarning("MazeGenerator или Camera не назначены!");
            return;
        }

        switch (viewMode)
        {
            case CameraViewMode.OrthographicTop:
                SetupOrthographicTopView();
                break;
            case CameraViewMode.PerspectiveTop:
                SetupPerspectiveTopView();
                break;
            case CameraViewMode.Original:
                RestoreOriginalCamera();
                break;
        }

        Debug.Log($"Камера настроена в режиме: {viewMode}");
    }

    private void SetupOrthographicTopView()
    {
        // Рассчитываем размеры лабиринта
        float mazeWidth = mazeGenerator.GetTotalWidth();
        float mazeDepth = mazeGenerator.GetTotalDepth();

        // Находим центр лабиринта
        Vector3 mazeCenter = new Vector3(
            mazeWidth * 0.5f + mazeGenerator.wallOffset.x,
            0f,
            mazeDepth * 0.5f + mazeGenerator.wallOffset.z
        );

        // Настраиваем камеру строго сверху
        mazeCamera.transform.position = new Vector3(
            mazeCenter.x,
            Mathf.Max(mazeWidth, mazeDepth) * 0.5f + 10f, // Высота зависит от размера лабиринта
            mazeCenter.z
        );
        mazeCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Настраиваем ортографический размер
        mazeCamera.orthographic = true;

        // Рассчитываем ортографический размер чтобы вместить весь лабиринт
        float aspectRatio = (float)Screen.width / Screen.height;
        float requiredSizeX = (mazeWidth * 0.5f + orthographicPadding) / aspectRatio;
        float requiredSizeZ = (mazeDepth * 0.5f + orthographicPadding);

        mazeCamera.orthographicSize = Mathf.Max(requiredSizeX, requiredSizeZ, minOrthographicSize);

        Debug.Log($"Ортографический вид: размер={mazeCamera.orthographicSize:F1}, лабиринт={mazeWidth:F1}x{mazeDepth:F1}");
    }

    private void SetupPerspectiveTopView()
    {
        // Рассчитываем размеры лабиринта
        float mazeWidth = mazeGenerator.GetTotalWidth();
        float mazeDepth = mazeGenerator.GetTotalDepth();

        // Находим центр лабиринта
        Vector3 mazeCenter = new Vector3(
            mazeWidth * 0.5f + mazeGenerator.wallOffset.x,
            0f,
            mazeDepth * 0.5f + mazeGenerator.wallOffset.z
        );

        // Настраиваем камеру сверху с небольшим наклоном для лучшего обзора
        mazeCamera.transform.position = new Vector3(
            mazeCenter.x,
            perspectiveHeight,
            mazeCenter.z - mazeDepth * 0.2f // Немного смещаем назад для лучшего обзора
        );

        // Смотрим на центр лабиринта с небольшим наклоном
        mazeCamera.transform.LookAt(mazeCenter);
        mazeCamera.orthographic = false;

        // Настраиваем поле зрения чтобы вместить весь лабиринт
        float distanceToCenter = Vector3.Distance(mazeCamera.transform.position, mazeCenter);
        float requiredFOV = Mathf.Atan(Mathf.Max(mazeWidth, mazeDepth) * 0.6f / distanceToCenter) * Mathf.Rad2Deg * 2f;
        mazeCamera.fieldOfView = Mathf.Clamp(requiredFOV, 40f, 80f);

        Debug.Log($"Перспективный вид: FOV={mazeCamera.fieldOfView:F1}, высота={perspectiveHeight:F1}");
    }

    private void RestoreOriginalCamera()
    {
        mazeCamera.transform.position = originalPosition;
        mazeCamera.transform.rotation = originalRotation;
        mazeCamera.orthographic = originalOrthographic;
        if (mazeCamera.orthographic)
            mazeCamera.orthographicSize = originalOrthographicSize;
    }

    private void SaveOriginalCameraState()
    {
        if (mazeCamera != null)
        {
            originalPosition = mazeCamera.transform.position;
            originalRotation = mazeCamera.transform.rotation;
            originalOrthographic = mazeCamera.orthographic;
            originalOrthographicSize = mazeCamera.orthographicSize;
        }
    }

    // Методы для смены режима через код
    public void SetOrthographicTopMode()
    {
        viewMode = CameraViewMode.OrthographicTop;
        SetupCameraView();
    }

    public void SetPerspectiveTopMode()
    {
        viewMode = CameraViewMode.PerspectiveTop;
        SetupCameraView();
    }

    public void SetOriginalMode()
    {
        viewMode = CameraViewMode.Original;
        SetupCameraView();
    }

    // Обновление камеры при изменении размера лабиринта
    public void UpdateCameraForNewMaze()
    {
        SetupCameraView();
    }

    void OnValidate()
    {
        // Автоматическое обновление в редакторе при изменении настроек
        if (Application.isPlaying && mazeCamera != null)
        {
            SetupCameraView();
        }
    }

    // Метод для отладки - показывает границы лабиринта
    void OnDrawGizmosSelected()
    {
        if (mazeGenerator != null)
        {
            float mazeWidth = mazeGenerator.GetTotalWidth();
            float mazeDepth = mazeGenerator.GetTotalDepth();
            Vector3 mazeCenter = new Vector3(
                mazeWidth * 0.5f + mazeGenerator.wallOffset.x,
                0f,
                mazeDepth * 0.5f + mazeGenerator.wallOffset.z
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mazeCenter, new Vector3(mazeWidth, 0.1f, mazeDepth));
        }
    }
}