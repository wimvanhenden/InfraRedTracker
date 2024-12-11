using com.rfilkov.kinect;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class tKinectTextures : MonoBehaviour
{
    // references
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    
    public RawImage ir_image;
    public RawImage color_image;
    public RawImage depth_image;

    public TMP_Dropdown textureDropdown;

    private void Start()
    {
        kinectManager = KinectManager.Instance;
        sensorData = kinectManager != null ? kinectManager.GetSensorData(0) : null;

        // Populate the dropdown
        textureDropdown.ClearOptions();
        textureDropdown.AddOptions(new List<string> { "IR Image", "Color Image","Depth Image" });

        // Set initial state
        UpdateSelectedTexture();

        // Add listener for when the selected option changes
        textureDropdown.onValueChanged.AddListener(delegate { UpdateSelectedTexture(); });
    }

    private void Update()
    {
        if (kinectManager && kinectManager.IsInitialized())
        {
            UpdateSelectedTexture();


        }
    }

    private void UpdateSelectedTexture()
    {
        switch (textureDropdown.value)
        {
            case 0: // IR Image
                UpdateIRTexture();
                DisableColorTexture();
                DisableDepthTexture();
                break;
            case 1: // Color Image
                UpdateColorTexture();
                DisableIRTexture();
                DisableDepthTexture();
                break;
            case 2: // Depth
                UpdateDepthTexture();
                DisableIRTexture();
                DisableColorTexture();
                break;
        }
    }

    private void UpdateIRTexture()
    {
        if (ir_image.gameObject.activeSelf)
        {
            Texture irTex = kinectManager.GetInfraredImageTex(0);
            ir_image.rectTransform.localScale = sensorData.depthImageScale;
            ir_image.texture = irTex;
        }
        else
        {
            ir_image.gameObject.SetActive(true);
        }
    }

    private void UpdateColorTexture()
    {
        if (color_image.gameObject.activeSelf)
        {
            Texture cTex = kinectManager.GetColorImageTex(0);
            color_image.rectTransform.localScale = sensorData.colorImageScale;
            color_image.texture = cTex;
        }
        else
        {
            color_image.gameObject.SetActive(true);
        }
    }

    private void UpdateDepthTexture()
    {
        if (depth_image.gameObject.activeSelf)
        {
            Texture dTex = kinectManager.GetDepthImageTex(0);
            depth_image.rectTransform.localScale = sensorData.depthImageScale;
            depth_image.texture = dTex;
        }
        else
        {
            depth_image.gameObject.SetActive(true);
        }
    }

    private void DisableIRTexture()
    {
        ir_image.gameObject.SetActive(false);
    }

    private void DisableColorTexture()
    {
        color_image.gameObject.SetActive(false);
    }

    private void DisableDepthTexture()
    {
        depth_image.gameObject.SetActive(false);
    }

}