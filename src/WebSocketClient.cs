using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


[Serializable]
public class SensorPayload
{
    public string token;
    public string height;
    public string temp;
}

public class SensorValues
{
    public string token;
    public float height;
    public float temp;

    public SensorValues Copy() => MemberwiseClone();

    public static SensorValues FromJSON(string json)
    {
        var payload = JsonUtility.FromJson<SensorPayload>(json);
        return new SensorValues {
            token = payload.token,
            height = payload.height switch { // map "lo", "mid", "hi" to scale 0..1
                "lo"  => 0.2f,
                "mid" => 0.5f,
                "hi"  => 0.9f,
                _     => 0.5f },
            temp = float.TryParse(
                payload.temp.Replace(" C", "").Trim(), // parse "15 C" -> 15
                out var t) ? t : 0f
        };
    }
}


public class WebSocketClient : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverUrl;
    [SerializeField] private string authToken;

    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;

    private SensorValues _latestData;
    private bool _dataReceived = false;
    private int iframe = 0;
    private SensorValues oldValues;
    private SensorValues currentValues;
    private SensorValues newValues;

    [Header("Animation Settings")]
    [SerializeField] private Transform waterLevelObject;
    [SerializeField] private ParticleSystem bubblesObject;
    [SerializeField] private int interpolationFramesMax = 240;
    public float defaultWaterLevel = 0.8f;
    public float defaultBubbles = 20;
    // TODO: how are iMax,defH,defT set?


    // Start is called before the first frame update
    async void Start()
    {
        // Initialize with default values
        oldValues = newValues = currentValues = new SensorValues {
            token = "",
            height = defaultWaterLevel,
            temp = defaultBubbles
        };
        // Update scene with initial values
        ApplyData(currentValues);

        _cts = new CancellationTokenSource();
        await Connect();
    }

    async Task Connect()
    {
        try
        {
            _webSocket = new ClientWebSocket();
            Debug.Log($"Connecting to {serverUrl}...");
            await _webSocket.ConnectAsync(new Uri(serverUrl), _cts.Token);
            Debug.Log("Connected!");

            // Send initial message to server to authenticate and subscribe to messages
            await SendMsg(authToken);

            // Start listening for messages in a loop
            await ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection Error: {e.Message}");
        }
    }

    async Task SendMsg(string message)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        Debug.Log($"Sent: {message}");
    }

    async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _cts.Token);
                    Debug.Log("Server closed connection.");
                }
                else
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log($"Received: {receivedMessage}");
                    ProcessJson(receivedMessage);
                }
            }
        }
        catch (Exception e)
        {
            if (_webSocket.State != WebSocketState.Aborted)
                Debug.LogError($"Receive Error: {e.Message}");
        }
    }

    void ProcessJson(string json)
    {
        try
        {
            _latestData = SensorValues.FromJSON(json);
            _dataReceived = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"JSON Parse Error: {e.Message}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // When new data has arrived, process it on the main thread
        if (_dataReceived)
        {
            _dataReceived = false;
            // Save start and end point values for interpolation
            oldValues = currentValues.Copy();
            newValues = _latestData;
            iframe = 1; // start animating
        }

        // Animate if current values haven't yet reached the new values
        if (iframe != 0)
        {
            float ratio = (float) iframe / interpolationFramesMax;
            if (++iframe > interpolationFramesMax)
                iframe = 0; // end point reached, stop animating

            // Interpolate and apply new values
            currentValues.height = Mathf.Lerp(oldValues.height, newValues.height, ratio);
            currentValues.temp = Mathf.Lerp(oldValues.temp, newValues.temp, ratio);
            ApplyData(currentValues);
        }
    }

    // Apply sensor data to game objects
    void ApplyData(SensorValues data)
    {
        // Transform water cube
        float scaleY = data.height;
        float posY = (scaleY-1) / 2;
        waterLevelObject.localScale = new Vector3(waterLevelObject.localScale.x, scaleY, waterLevelObject.localScale.z);
        waterLevelObject.localPosition = new Vector3(waterLevelObject.localPosition.x, posY, waterLevelObject.localPosition.z);

        // Set bubble amount
        var emission = bubblesObject.emission;
        emission.rateOverTime = data.temp / 2;
        // Set bubble rate
        var main = bubblesObject.main;
        main.startSpeed = data.temp / 30;
    }

    private async void OnDestroy()
    {
        // Clean up connection when the object is destroyed or game stops
        if (_webSocket != null)
        {
            _cts.Cancel();
            if (_webSocket.State != WebSocketState.Aborted) // stopping play aborts
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App Closing", CancellationToken.None);
            _webSocket.Dispose();
        }
        _cts.Dispose();
    }

}
