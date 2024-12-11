using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class tDebug : MonoBehaviour
{

    public GameObject canvas;
    public TextMeshProUGUI myText;

    // Start is called before the first frame update
    void Start()
    {
        TrackerConfigData data = tConfigManager.Instance.TrackerConfig;
        int _id = data.id;
        myText.text = "ID: " + _id.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            toggleDebug();
        }
    }


    void OnEnable()
    {
        tTrackerWebSocket.OnDebugReceived += toggleDebug;
    }

    void OnDisable()
    {
        tTrackerWebSocket.OnDebugReceived -= toggleDebug;
    }

    void toggleDebug()
    {

       Debug.Log("Toggle Debug");
       canvas.SetActive(!canvas.activeSelf);
    }
}
