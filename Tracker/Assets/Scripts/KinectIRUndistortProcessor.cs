using UnityEngine;
using System.Xml.Linq;
using System.IO;
using System;
using com.rfilkov.kinect;
namespace com.tool
{
    public class KinectIRUndistortProcessor : MonoBehaviour
    {
        public string xmlFileName = "camera_calibration.xml";
        public Material infraredMaterial;
        public Material undistortionMaterial;
        public bool doUndistortion = true;
        public bool visualizeResult = true;
        public bool useSave = false;
        public float xpos = 0;
        public float ypos = 0;
        public float tex_size = 512;
        public string title = "infra red";

        [Header("IR Range")]
        [Tooltip("Minimum IR value for normalization")]
        public float minIRValue = 0f;
        [Tooltip("Maximum IR value for normalization")]
        public float maxIRValue = 65535f;

        private Vector4 cameraMatrix;
        private Vector4 distortionCoeffs;
        private Vector2 imageSize;


        public RenderTexture finalProcessedTexture;
        private RenderTexture intermediateTexture;
        private Texture2D rawInfraredTexture;

        private bool isInitialized = false;
        private KinectManager kinectManager;
        private int saveCount = 0;


        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (isInitialized)
            {
                ProcessFrame();

                if (useSave)
                {

                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        SaveSelectedTextureAsPNG();
                    }
                }

            }
        }

        void Initialize()
        {
            kinectManager = KinectManager.Instance;
            kinectManager.StartDepthSensors(tConfigManager.Instance.Config.cameraindex, tConfigManager.Instance.Config.useRecording, tConfigManager.Instance.Config.recordingFile);

            if (kinectManager == null)
            {
                Debug.LogError("Failed to get KinectManager instance.");
                return;
            }

            if (LoadCalibrationData())
            {
                SetupTextures();
                isInitialized = true;
            }
        }

        bool LoadCalibrationData()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, xmlFileName);

            if (!File.Exists(filePath))
            {
                Debug.LogError($"Calibration file not found: {filePath}");
                return false;
            }

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement root = doc.Root;

                if (root == null || root.Name != "camera_calibration")
                {
                    Debug.LogError("Invalid XML structure: 'camera_calibration' element not found as root.");
                    return false;
                }

                // Load camera matrix
                XElement matrix = root.Element("camera_matrix");
                if (matrix != null)
                {
                    cameraMatrix = new Vector4(
                        ParseFloatSafe(matrix.Element("row_0")?.Element("value_0")),
                        ParseFloatSafe(matrix.Element("row_1")?.Element("value_1")),
                        ParseFloatSafe(matrix.Element("row_0")?.Element("value_2")),
                        ParseFloatSafe(matrix.Element("row_1")?.Element("value_2"))
                    );
                }
                else
                {
                    Debug.LogError("Camera matrix not found in the XML.");
                    return false;
                }

                // Load distortion coefficients
                XElement distortion = root.Element("distortion_coefficients");
                if (distortion != null)
                {
                    distortionCoeffs = new Vector4(
                        ParseFloatSafe(distortion.Element("value_0")),
                        ParseFloatSafe(distortion.Element("value_1")),
                        ParseFloatSafe(distortion.Element("value_2")),
                        ParseFloatSafe(distortion.Element("value_3"))
                    );
                }
                else
                {
                    Debug.LogError("Distortion coefficients not found in the XML.");
                    return false;
                }

                // Load image size
                imageSize = new Vector2(
                    ParseFloatSafe(root.Element("image_width")),
                    ParseFloatSafe(root.Element("image_height"))
                );

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading calibration data: {e.Message}");
                return false;
            }
        }

        float ParseFloatSafe(XElement element)
        {
            if (element == null || string.IsNullOrEmpty(element.Value))
            {
                Debug.LogWarning($"Missing or empty element while parsing float.");
                return 0f;
            }

            if (float.TryParse(element.Value, out float result))
            {
                return result;
            }
            else
            {
                Debug.LogWarning($"Failed to parse float value: {element.Value}");
                return 0f;
            }
        }

        void SetupTextures()
        {
            int width = 512;
            int height = 512;

            rawInfraredTexture = new Texture2D(width, height, TextureFormat.R16, false);
            intermediateTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            intermediateTexture.Create();

            if (finalProcessedTexture == null)
            {
                finalProcessedTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
                finalProcessedTexture.Create();
            }

        }

        void UpdateInfraredTexture()
        {
            ushort[] infraredData = kinectManager.GetRawInfraredMap(0);
            if (infraredData.Length != (int)imageSize.x * (int)imageSize.y)
            {
                Debug.LogError("Infrared data size doesn't match texture dimensions!");
                return;
            }

            byte[] byteArray = new byte[infraredData.Length * 2];
            Buffer.BlockCopy(infraredData, 0, byteArray, 0, byteArray.Length);

            rawInfraredTexture.LoadRawTextureData(byteArray);
            rawInfraredTexture.Apply();
        }

        void ProcessFrame()
        {
            UpdateInfraredTexture();

            if (doUndistortion)
            {

                // Apply infrared shader
                infraredMaterial.SetFloat("_MinValue", minIRValue);
                infraredMaterial.SetFloat("_MaxValue", maxIRValue);
                Graphics.Blit(rawInfraredTexture, intermediateTexture, infraredMaterial);

                // Apply undistortion shader
                undistortionMaterial.SetVector("_CameraMatrix", cameraMatrix);
                undistortionMaterial.SetVector("_DistCoeffs", distortionCoeffs);
                undistortionMaterial.SetVector("_ImageSize", imageSize);
                undistortionMaterial.SetTexture("_DistortedTex", intermediateTexture);
                undistortionMaterial.SetFloat("_DebugMode", 0);
                Graphics.Blit(intermediateTexture, finalProcessedTexture, undistortionMaterial);
            }
            else
            {
                // Apply infrared shader
                infraredMaterial.SetFloat("_MinValue", minIRValue);
                infraredMaterial.SetFloat("_MaxValue", maxIRValue);
                Graphics.Blit(rawInfraredTexture, finalProcessedTexture, infraredMaterial);

            }
        }

        void OnGUI()
        {
            if (visualizeResult && finalProcessedTexture != null)
            {
                GUI.DrawTexture(new Rect(xpos, ypos, tex_size, tex_size), finalProcessedTexture);
                GUI.Label(new UnityEngine.Rect(xpos + 10, ypos + 10, 200, 20), title, new GUIStyle { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } });
                // Draw horizontal line
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(xpos + (tex_size / 2), ypos, 2, tex_size), Texture2D.whiteTexture);

                // Draw vertical line
                GUI.DrawTexture(new Rect(xpos, ypos + (tex_size / 2), tex_size, 2), Texture2D.whiteTexture);
            }


        }

        void OnDestroy()
        {
            if (intermediateTexture != null)
            {
                intermediateTexture.Release();
            }
            if (finalProcessedTexture != null)
            {
                finalProcessedTexture.Release();
            }
        }

        public void SaveSelectedTextureAsPNG()
        {

            SaveTextureAsPNG(finalProcessedTexture);

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
}