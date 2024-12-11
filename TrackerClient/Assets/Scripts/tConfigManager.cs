using UnityEngine;
using System.IO;

public class ConfigData
{
    public string server_ip_address = "localhost";
    public int server_port = 8080;
}

[System.Serializable]
public class TrackerConfigData : ConfigData
{
    public int id = 1;
    public bool flipx = false;
    public bool flipy = false;
}


public class tConfigManager : MonoBehaviour
{
    public static tConfigManager Instance { get; private set; }

    public TrackerConfigData TrackerConfig { get; private set; }
  

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
        var trackerConfigFilePath = Path.Combine(Application.streamingAssetsPath, "tracker_config.json");

        if (File.Exists(trackerConfigFilePath))
        {
            string jsonContent = File.ReadAllText(trackerConfigFilePath);
            TrackerConfig = JsonUtility.FromJson<TrackerConfigData>(jsonContent);

            UnityEngine.Debug.Log("Tracker config loaded successfully");
        }
        else
        {
            UnityEngine.Debug.LogError("Tracker config file not found!");
            TrackerConfig = new TrackerConfigData(); // Use default values
        }


    }
}
