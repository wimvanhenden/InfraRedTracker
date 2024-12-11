using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace com.tool
{

    [System.Serializable]
    public class ConfigData
    {
        public string tracker_server_ip_address = "localhost";
        public int tracker_server_port = 8080;
        public int id = 1;
        public int cameraindex = 0;
        public bool useRecording = false;
        public string recordingFile = "";
    }


    public class tConfigManager : MonoBehaviour
    {
        public static tConfigManager Instance { get; private set; }

        public ConfigData Config { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadConfig();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadConfig()
        {


            string filePath = Path.Combine(Application.streamingAssetsPath, "config.json");

            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                Config = JsonUtility.FromJson<ConfigData>(jsonContent);

                UnityEngine.Debug.Log("Config loaded successfully");
            }
            else
            {
                UnityEngine.Debug.LogError("Config file not found!");
                Config = new ConfigData(); // Use default values
            }
        }
    }
}
