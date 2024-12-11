using System.Collections;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class TrackerData
{
    public float x;
    public float y;
    public int id = 1;
}


public class tTrackerWebSocket : MonoBehaviour
{
    WebSocket websocket;
    private const float ReconnectInterval = 5f;
    private bool isConnecting = false;

    
    public string TrackingStatus { get; private set; } = string.Empty;

    // Define a delegate for the event
    public delegate void TrackerDataReceivedHandler(TrackerData trackerData);
    // Define the event using the delegate
    public static event TrackerDataReceivedHandler OnTrackerDataReceived;

    // Define a delegate for the event
    public delegate void DebugReceiverHandler();
    // Define the event using the delegate
    public static event DebugReceiverHandler OnDebugReceived;

    private void Start()
    {
        StartCoroutine(ConnectWithRetry());
    }

    private IEnumerator ConnectWithRetry()
    {
        while (true)
        {
            if (websocket == null || websocket.State == WebSocketState.Closed)
            {
                yield return StartCoroutine(ConnectToWebSocket());
            }
            yield return new WaitForSeconds(ReconnectInterval);
        }
    }

    private IEnumerator ConnectToWebSocket()
    {
        if (isConnecting) yield break;

        isConnecting = true;
        TrackerConfigData congig_data = tConfigManager.Instance.TrackerConfig;
        string HOST = congig_data.server_ip_address;
        int PORT = congig_data.server_port;

        websocket = new WebSocket($"ws://{HOST}:{PORT}");

        TrackingStatus = "Connecting...";

        websocket.OnOpen += () =>
        {
            SendRegistration();
            isConnecting = false;
            TrackingStatus = "Connected";
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("Error! " + e);
            isConnecting = false;
            TrackingStatus = "ERROR";
        };

        websocket.OnClose += (e) =>
        {
            isConnecting = false;
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            TrackerData _data = JsonConvert.DeserializeObject<TrackerData>(message);
            // Broadcast the event with the tracker object

            if (tConfigManager.Instance.TrackerConfig.flipx)
                _data.x = 1f - _data.x;
            if (tConfigManager.Instance.TrackerConfig.flipy)
                _data.y = 1f - _data.y;

            TrackerConfigData data = tConfigManager.Instance.TrackerConfig;
            int _id = data.id;

            if (_data.id == -1 && _data.x == -1 && _data.y == -1)
            {
                OnDebugReceived?.Invoke();
            }
            else
            {
                if (_id == _data.id)
                {
                    OnTrackerDataReceived?.Invoke(_data);
                }
            }
        };

        // waiting for messages
        yield return websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif
    }

    async void SendRegistration()
    {
        if (websocket.State == WebSocketState.Open)
        {
            TrackerConfigData data = tConfigManager.Instance.TrackerConfig;
            int _id = data.id;
            var jsonObject = new
            {
                type = "trackerreceiver",
                id = _id,
            };
            string jsonString = JsonConvert.SerializeObject(jsonObject);
            await websocket.SendText(jsonString);
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}