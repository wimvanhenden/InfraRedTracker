using System.Collections;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace com.tool
{

    public class tTrackerWebSocket : MonoBehaviour
    {
        public WebSocket websocket;
        private const float ReconnectInterval = 5f;
        private bool isConnecting = false;

        // Define a delegate for the event
        public delegate void TrackerDataReceivedHandler(Tracker trackerData);
        // Define the event using the delegate
        public static event TrackerDataReceivedHandler OnTrackerDataReceived;

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
            ConfigData data = tConfigManager.Instance.Config;
            string HOST = data.tracker_server_ip_address;
            int PORT = data.tracker_server_port;

            websocket = new WebSocket($"ws://{HOST}:{PORT}");

            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                SendRegistration();
                isConnecting = false;
            };

            websocket.OnError += (e) =>
            {
                Debug.Log("Error! " + e);
                isConnecting = false;
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("Connection closed!");
                isConnecting = false;
            };

            websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Tracker _data = JsonConvert.DeserializeObject<Tracker>(message);
                // Broadcast the event with the tracker object
                OnTrackerDataReceived?.Invoke(_data);
            };

            // waiting for messages
            yield return websocket.Connect();
        }

        public async void SendTrackerList(List<Tracker> trackers)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                string jsonString = JsonConvert.SerializeObject(trackers);
                await websocket.SendText(jsonString);

            }

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
                ConfigData data = tConfigManager.Instance.Config;
                int _id = data.id;
                var jsonObject = new
                {
                    type = "tracker",
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
}