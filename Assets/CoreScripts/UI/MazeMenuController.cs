using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MazeMenuController : MonoBehaviour
{
    [Header("Основные ссылки")]
    public MazeGenerator mazeGenerator;
    public CanvasGroup menuCanvasGroup;
    public RectTransform menuPanel;
    public MazeTimer mazeTimer; // НОВОЕ: ссылка на таймер

    [Header("Режимы игры")]
    public ToggleGroup modeToggleGroup;
    public Toggle hardModeToggle;
    public Toggle easyModeToggle;
    public Toggle proModeToggle;

    [Header("Настройки лабиринта")]
    public TMP_InputField chunkSizeInput;
    public TMP_InputField mazeWidthInput;
    public TMP_InputField mazeHeightInput;
    public Toggle createFinishToggle;
    public Toggle useRightHandRuleToggle;

    [Header("Кнопки")]
    public Button generateButton;
    public Button closeButton;
    public Button resetSettingsButton;
    public Button restartButton; // НОВАЯ КНОПКА

    [Header("Настройки UI")]
    public float fadeDuration = 0.3f;
    public bool startVisible = true;
    public bool hideDuringGeneration = true;

    private Vector2 menuHiddenPosition;
    private Vector2 menuVisiblePosition;
    private bool isMenuVisible = true;
    private Coroutine fadeCoroutine;

    [System.Serializable]
    private struct DefaultSettings
    {
        public int chunkSize;
        public int mazeWidth;
        public int mazeHeight;
        public float cellSize;
        public float wallHeight;
        public bool hasFinish;
        public bool useRightHandRule;
    }

    private DefaultSettings defaultSettings;

    void Start()
    {
        InitializeMenu();
        LoadDefaultSettings();
        ApplyDefaultSettingsToUI();

        if (!startVisible)
        {
            menuCanvasGroup.alpha = 0;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
            isMenuVisible = false;
        }
    }

    void InitializeMenu()
    {
        menuVisiblePosition = menuPanel.anchoredPosition;
        menuHiddenPosition = menuVisiblePosition + new Vector2(-menuPanel.rect.width, 0);

        generateButton.onClick.AddListener(OnGenerateButtonClick);
        closeButton.onClick.AddListener(ToggleMenu);
        resetSettingsButton.onClick.AddListener(ResetToDefaults);

        // НОВОЕ: инициализация кнопки рестарта
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClick);
        }

        chunkSizeInput.onEndEdit.AddListener(OnChunkSizeChanged);
        mazeWidthInput.onEndEdit.AddListener(OnMazeWidthChanged);
        mazeHeightInput.onEndEdit.AddListener(OnMazeHeightChanged);

        easyModeToggle.interactable = false;
        proModeToggle.interactable = false;
        hardModeToggle.isOn = true;

        Debug.Log("✅ Меню инициализировано");
    }

    void LoadDefaultSettings()
    {
        defaultSettings = new DefaultSettings
        {
            chunkSize = 4,
            mazeWidth = 3,
            mazeHeight = 3,
            cellSize = 2f,
            wallHeight = 3f,
            hasFinish = true,
            useRightHandRule = true
        };
    }

    void ApplyDefaultSettingsToUI()
    {
        chunkSizeInput.text = defaultSettings.chunkSize.ToString();
        mazeWidthInput.text = defaultSettings.mazeWidth.ToString();
        mazeHeightInput.text = defaultSettings.mazeHeight.ToString();
        createFinishToggle.isOn = defaultSettings.hasFinish;
        useRightHandRuleToggle.isOn = defaultSettings.useRightHandRule;
    }

    void OnGenerateButtonClick()
    {
        if (mazeGenerator == null)
        {
            Debug.LogError("❌ MazeGenerator не назначен!");
            return;
        }
        ToggleMenu();
        ApplySettingsToMazeGenerator();
        StartCoroutine(GenerationSequence());
    }

    // НОВЫЙ МЕТОД: обработка нажатия кнопки рестарта
    void OnRestartButtonClick()
    {
        if (mazeTimer != null)
        {
            mazeTimer.OnRestartButtonClick();
            Debug.Log("🔄 Рестарт выполнен через MazeMenuController");
        }
        else
        {
            // Пытаемся найти все компоненты
            mazeTimer = FindObjectOfType<MazeTimer>();
            if (mazeTimer != null)
            {
                mazeTimer.OnRestartButtonClick();
                Debug.Log("🔄 Рестарт выполнен (таймер найден автоматически)");
            }
            else
            {
                Debug.LogWarning("⚠️ MazeTimer не найден, пытаемся выполнить рестарт вручную");

                // Прямой рестарт без таймера
                CarController car = FindObjectOfType<CarController>();
                if (car != null && mazeGenerator != null)
                {
                    // Возвращаем машинку на старт
                    if (mazeGenerator.createFinishArea)
                    {
                        var mazeData = mazeGenerator.GetMazeData();
                        if (mazeData != null)
                        {
                            int startChunkX = mazeData.StartGenerationChunk.x;
                            int startChunkZ = mazeData.StartGenerationChunk.y;
                            int startCellX = Mathf.Max(0, mazeData.StartGenerationCell.x - 2);
                            int startCellZ = Mathf.Max(0, mazeData.StartGenerationCell.y - 2);

                            car.SetCarPosition(startChunkX, startChunkZ, startCellX, startCellZ);
                            car.ResetDirection();
                            Debug.Log($"🔄 Машинка возвращена на старт вручную");
                        }
                    }
                }
            }
        }
    }

    IEnumerator GenerationSequence()
    {
        if (hideDuringGeneration)
        {
            ToggleMenu();
        }

        Debug.Log("🔄 Запуск генерации лабиринта...");
        generateButton.interactable = false;

        mazeGenerator.GenerateMaze();

        yield return new WaitUntil(() => !mazeGenerator.IsGenerating());

        generateButton.interactable = true;

        if (hideDuringGeneration)
        {
            ToggleMenu();
        }

        Debug.Log("✅ Генерация завершена");
    }

    void ApplySettingsToMazeGenerator()
    {
        if (int.TryParse(chunkSizeInput.text, out int chunkSize))
            mazeGenerator.chunkSize = Mathf.Clamp(chunkSize, 2, 10);

        if (int.TryParse(mazeWidthInput.text, out int width))
            mazeGenerator.mazeSizeInChunks.x = Mathf.Clamp(width, 1, 10);

        if (int.TryParse(mazeHeightInput.text, out int height))
            mazeGenerator.mazeSizeInChunks.y = Mathf.Clamp(height, 1, 10);

        mazeGenerator.createFinishArea = createFinishToggle.isOn;
        mazeGenerator.useRightHandRule = useRightHandRuleToggle.isOn;

        Debug.Log("⚙️ Настройки применены к генератору");
    }

    void ToggleMenu()
    {
        isMenuVisible = !isMenuVisible;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeMenu(isMenuVisible));
    }

    IEnumerator FadeMenu(bool show)
    {
        float targetAlpha = show ? 1 : 0;
        float startAlpha = menuCanvasGroup.alpha;
        float elapsedTime = 0;

        Vector2 startPos = menuPanel.anchoredPosition;
        Vector2 targetPos = show ? menuVisiblePosition : menuHiddenPosition;

        menuCanvasGroup.interactable = show;
        menuCanvasGroup.blocksRaycasts = show;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            menuPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        menuCanvasGroup.alpha = targetAlpha;
        menuPanel.anchoredPosition = targetPos;

        fadeCoroutine = null;
    }

    void ResetToDefaults()
    {
        Debug.Log("🔄 Сброс настроек к значениям по умолчанию");
        ApplyDefaultSettingsToUI();
        ApplySettingsToMazeGenerator();
    }

    void OnChunkSizeChanged(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            intValue = Mathf.Clamp(intValue, 2, 10);
            chunkSizeInput.text = intValue.ToString();
        }
    }

    void OnMazeWidthChanged(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            intValue = Mathf.Clamp(intValue, 1, 10);
            mazeWidthInput.text = intValue.ToString();
        }
    }

    void OnMazeHeightChanged(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            intValue = Mathf.Clamp(intValue, 1, 10);
            mazeHeightInput.text = intValue.ToString();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftControl))
        {
            OnGenerateButtonClick();
        }

        // НОВОЕ: горячая клавиша для рестарта (Ctrl+R)
        if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftControl))
        {
            OnRestartButtonClick();
        }
    }

    public void ShowMenu()
    {
        if (!isMenuVisible)
            ToggleMenu();
    }

    public void HideMenu()
    {
        if (isMenuVisible)
            ToggleMenu();
    }

    public bool IsMenuVisible
    {
        get { return isMenuVisible; }
    }
}