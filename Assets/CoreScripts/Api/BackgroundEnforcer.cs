using UnityEngine;

public class BackgroundEnforcer : MonoBehaviour
{
    [Header("Background Settings")]
    public bool runInBackground = true;
    public int targetFrameRate = 60;

    void Start()
    {
        // Заставляем работать в фоне
        Application.runInBackground = runInBackground;

        // Устанавливаем целевой FPS
        Application.targetFrameRate = targetFrameRate;

        // Отключаем сон экрана
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log($"Background settings applied: RunInBackground={Application.runInBackground}, TargetFPS={Application.targetFrameRate}");
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"Application focus: {hasFocus}");

        // Если потеряли фокус, но runInBackground true - все должно продолжать работать
        if (!hasFocus)
        {
            Debug.Log("Application lost focus, but should continue running in background");
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        Debug.Log($"Application pause: {pauseStatus}");
    }
}