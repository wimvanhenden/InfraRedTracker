using com.rfilkov.kinect;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class tKinectScreenshots : MonoBehaviour
{
    // references
    private KinectManager kinectManager = null;
    private KinectInterop.SensorData sensorData = null;
    private int saveCount = 0;
    public RawImage ir_image;
    public RawImage color_image;
    public TMP_Dropdown textureDropdown;

    private void Start()
    {
        kinectManager = KinectManager.Instance;
        sensorData = kinectManager != null ? kinectManager.GetSensorData(0) : null;

        // Populate the dropdown
        textureDropdown.ClearOptions();
        textureDropdown.AddOptions(new List<string> { "IR Image", "Color Image" });

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

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SaveSelectedTextureAsPNG();
            }
        }
    }

    private void UpdateSelectedTexture()
    {
        switch (textureDropdown.value)
        {
            case 0: // IR Image
                UpdateIRTexture();
                DisableColorTexture();
                break;
            case 1: // Color Image
                UpdateColorTexture();
                DisableIRTexture();
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

    private void DisableIRTexture()
    {
        ir_image.gameObject.SetActive(false);
    }

    private void DisableColorTexture()
    {
        color_image.gameObject.SetActive(false);
    }

    public void SaveSelectedTextureAsPNG()
    {
        Texture selectedTexture = GetSelectedTexture();
        if (selectedTexture != null)
        {
            SaveTextureAsPNG(selectedTexture);
        }
        else
        {
            Debug.LogWarning("No texture selected or texture is null.");
        }
    }

    private Texture GetSelectedTexture()
    {
        return textureDropdown.value == 0 ? ir_image.texture : color_image.texture;
    }

    public void SaveTextureAsPNG(Texture textureToSave)
    {
        if (textureToSave == null)
        {
            Debug.LogError("No texture provided to save.");
            return;
        }
        Texture2D texture2D = ConvertToTexture2D(textureToSave);
        if (texture2D == null)
        {
            Debug.LogError("Failed to convert texture to Texture2D.");
            return;
        }
        texture2D = FlipTextureVertically(texture2D);
        byte[] bytes = texture2D.EncodeToPNG();
        string fileName = $"Texture_{saveCount}";
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".png");
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Saved texture to: " + filePath);
        saveCount++;
        // Clean up the temporary texture
        Destroy(texture2D);
    }

    private Texture2D ConvertToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);
        return texture2D;
    }

    private Texture2D FlipTextureVertically(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height);
        int xN = original.width;
        int yN = original.height;
        for (int i = 0; i < xN; i++)
        {
            for (int j = 0; j < yN; j++)
            {
                flipped.SetPixel(i, yN - j - 1, original.GetPixel(i, j));
            }
        }
        flipped.Apply();
        return flipped;
    }
}