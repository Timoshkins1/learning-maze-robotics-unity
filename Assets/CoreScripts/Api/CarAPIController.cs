using UnityEngine;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

public class CarAPIController : MonoBehaviour
{
    [Header("API Settings")]
    public int port = 8080;
    public bool autoStartServer = true;
    public bool runInBackground = true; // Добавьте эту настройку

    private HttpListener httpListener;
    private CarController carController;
    private bool isServerRunning = false;
    private CancellationTokenSource cancellationTokenSource;

    void Start()
    {
        // Заставляем Unity работать в фоне
        Application.runInBackground = runInBackground;

        carController = GetComponent<CarController>();

        if (autoStartServer)
        {
            StartServer();
        }
    }

    void OnDestroy()
    {
        StopServer();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Логируем изменение фокуса для отладки
        Debug.Log($"Application focus changed: {hasFocus}");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // Логируем паузу для отладки
        Debug.Log($"Application pause: {pauseStatus}");
    }

    public void StartServer()
    {
        if (isServerRunning) return;

        StartCoroutine(StartServerAsync());
    }

    public void StopServer()
    {
        isServerRunning = false;

        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();

        if (httpListener != null)
        {
            if (httpListener.IsListening)
            {
                httpListener.Stop();
            }
            httpListener.Close();
            httpListener = null;
        }

        Debug.Log("Car API Server stopped");
    }

    private IEnumerator StartServerAsync()
    {
        httpListener = new HttpListener();

        // Добавляем все возможные префиксы
        httpListener.Prefixes.Add($"http://localhost:{port}/");
        httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
        httpListener.Prefixes.Add($"http://*:{port}/");  // Принимаем соединения со всех адресов

        try
        {
            httpListener.Start();
            isServerRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            Debug.Log($"Car API Server started successfully on port {port}");
            Debug.Log($"Application.runInBackground: {Application.runInBackground}");
            Debug.Log($"Available endpoints:");
            Debug.Log($"  POST http://localhost:{port}/turn/left");
            Debug.Log($"  POST http://localhost:{port}/turn/right");
            Debug.Log($"  POST http://localhost:{port}/move/forward");
            Debug.Log($"  POST http://localhost:{port}/move/backward");
            Debug.Log($"  GET  http://localhost:{port}/status");

            // Запускаем обработку запросов
            Task.Run(() => HandleRequests(cancellationTokenSource.Token));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start server: {e.Message}");
            Debug.LogError($"Exception type: {e.GetType()}");

            // Попробуем запустить с правами администратора
            if (e is System.Net.HttpListenerException)
            {
                Debug.LogWarning("Try running Unity as Administrator or use a different port");
            }
        }

        yield return null;
    }

    private async Task HandleRequests(CancellationToken cancellationToken)
    {
        Debug.Log("Starting request handler...");

        while (isServerRunning && httpListener != null && httpListener.IsListening && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await httpListener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => ProcessRequest(context), cancellationToken);
            }
            catch (System.Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Debug.LogWarning($"Error in request handler: {ex.Message}");
                    await Task.Delay(1000); // Ждем перед повторной попыткой
                }
            }
        }

        Debug.Log("Request handler stopped");
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Debug.Log($"Received request: {request.HttpMethod} {request.Url.LocalPath} (Focus: {Application.isFocused}, Running: {Application.runInBackground})");

            string responseText = "";
            string method = request.HttpMethod;
            string path = request.Url.LocalPath.ToLower();

            // Устанавливаем CORS headers
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            // Обрабатываем preflight запросы
            if (method == "OPTIONS")
            {
                response.StatusCode = 200;
                response.ContentLength64 = 0;
                response.Close();
                return;
            }

            // Обрабатываем основные запросы
            if (path == "/turn/left" && method == "POST")
            {
                Debug.Log("Executing Turn Left command");
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    if (carController != null)
                    {
                        //carController.TurnLeft();
                        Debug.Log("Turn Left executed successfully");
                    }
                    else
                    {
                        Debug.LogError("CarController is null!");
                    }
                });
                responseText = "{\"status\":\"success\",\"action\":\"turn_left\"}";
                response.StatusCode = 200;
            }
            else if (path == "/turn/right" && method == "POST")
            {
                Debug.Log("Executing Turn Right command");
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    //carController?.TurnRight();
                });
                responseText = "{\"status\":\"success\",\"action\":\"turn_right\"}";
                response.StatusCode = 200;
            }
            else if (path == "/move/forward" && method == "POST")
            {
                Debug.Log("Executing Move Forward command");
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    //carController?.MoveForward();
                });
                responseText = "{\"status\":\"success\",\"action\":\"move_forward\"}";
                response.StatusCode = 200;
            }
            else if (path == "/move/backward" && method == "POST")
            {
                Debug.Log("Executing Move Backward command");
                MainThreadDispatcher.ExecuteOnMainThread(() => {
                    //carController?.MoveBackward();
                });
                responseText = "{\"status\":\"success\",\"action\":\"move_backward\"}";
                response.StatusCode = 200;
            }
            else if (path == "/status" && method == "GET")
            {
                Debug.Log("Processing status request");

                if (carController != null)
                {
                    UnityEngine.Vector2Int chunk = carController.GetCurrentChunkCoordinates();
                    UnityEngine.Vector2Int cell = carController.GetCurrentCellCoordinates();
                    string direction = carController.GetCurrentDirectionName();

                    responseText = $"{{\"status\":\"running\",\"position\":{{\"chunk\":{{\"x\":{chunk.x},\"y\":{chunk.y}}},\"cell\":{{\"x\":{cell.x},\"y\":{cell.y}}},\"direction\":\"{direction}\"}},\"focus\":{Application.isFocused.ToString().ToLower()}}}";
                }
                else
                {
                    responseText = "{\"status\":\"car_controller_not_found\"}";
                }
                response.StatusCode = 200;
            }
            else
            {
                Debug.LogWarning($"Unknown endpoint: {method} {path}");
                responseText = "{\"status\":\"error\",\"message\":\"Endpoint not found\"}";
                response.StatusCode = 404;
            }

            // Отправляем ответ
            byte[] buffer = Encoding.UTF8.GetBytes(responseText);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            Debug.Log($"Response sent: {response.StatusCode} - {responseText}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing request: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");

            try
            {
                response.StatusCode = 500;
                string errorText = "{\"status\":\"error\",\"message\":\"Internal server error\"}";
                byte[] buffer = Encoding.UTF8.GetBytes(errorText);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch
            {
                // Игнорируем ошибки при отправке ошибки
            }
        }
    }

    // Для тестирования
    void Update()
    {
        // Периодически логируем состояние для отладки
        if (Time.frameCount % 300 == 0) // Каждые 300 кадров
        {
            Debug.Log($"Server status - Running: {isServerRunning}, Focus: {Application.isFocused}, InBackground: {Application.runInBackground}");
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"Server running: {isServerRunning}, Listening: {httpListener?.IsListening ?? false}, Focus: {Application.isFocused}");
        }

        if (Input.GetKeyDown(KeyCode.F2) && !isServerRunning)
        {
            StartServer();
        }

        // Принудительно включаем работу в фоне если отключилась
        if (!Application.runInBackground)
        {
            Application.runInBackground = true;
            Debug.LogWarning("runInBackground was false, forced to true");
        }
    }
}