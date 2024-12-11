using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;

public class BlobTracker : MonoBehaviour
{
    public RenderTexture inputTexture;
    public RenderTexture outputTexture;

    public bool visualizeResult = true;
    public float xpos = 0;
    public float ypos = 0;
    public float tex_size = 512;

    public float _lowthresh = 0;
    public float _highthresh = 255;

    private Mat inputMat;
    private Mat grayMat;
    private Mat binaryMat;
    private Mat outputMat;
    private Texture2D tempInputTexture;
    private Texture2D tempOutputTexture;

    void Start()
    {
        // Initialize matrices
        inputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);
        grayMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC1);
        binaryMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC1);
        outputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);

        // Create temporary textures
        tempInputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGBA32, false);
        tempOutputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGBA32, false);

        // Create output RenderTexture if not provided
        if (outputTexture == null)
        {
            outputTexture = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGBFloat);
            outputTexture.Create();
        }
    }

    void Update()
    {
        // Read the input RenderTexture into the temporary Texture2D
        /*RenderTexture.active = inputTexture;
        tempInputTexture.ReadPixels(new UnityEngine.Rect(0, 0, inputTexture.width, inputTexture.height), 0, 0);
        tempInputTexture.Apply();
        RenderTexture.active = null;

        // Convert texture to Mat
        Utils.texture2DToMat(tempInputTexture, inputMat);

        // Copy input to output for visualization
        inputMat.copyTo(outputMat);

        // Convert to grayscale
        Imgproc.cvtColor(inputMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

        // Threshold to create binary image
        Imgproc.threshold(grayMat, binaryMat, _lowthresh, _highthresh, Imgproc.THRESH_BINARY);

        // Find contours
        using (Mat hierarchy = new Mat())
        {
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Imgproc.findContours(binaryMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            // Find the largest contour (blob)
            MatOfPoint largestContour = null;
            double largestArea = 0;

            foreach (MatOfPoint contour in contours)
            {
                double area = Imgproc.contourArea(contour);
                if (area > largestArea)
                {
                    largestArea = area;
                    largestContour = contour;
                }
            }

            // Draw the largest contour and its center
            if (largestContour != null)
            {
                Imgproc.drawContours(outputMat, new List<MatOfPoint> { largestContour }, -1, new Scalar(0, 255, 0, 255), 2);

                // Get the center of the blob
                Moments moments = Imgproc.moments(largestContour);
                int cx = (int)(moments.get_m10() / moments.get_m00());
                int cy = (int)(moments.get_m01() / moments.get_m00());

                // Draw the center point
                Imgproc.circle(outputMat, new Point(cx, cy), 5, new Scalar(255, 0, 0, 255), -1);

                //Draw coordinates
                string coordText = $"({cx}, {cy})";
                Imgproc.putText(outputMat, coordText, new Point(cx - 40, cy + 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                //Debug.Log($"Blob center: ({cx}, {cy})");
            }

            // Clean up
            foreach (MatOfPoint contour in contours)
            {
                contour.Dispose();
            }
        }

        // Convert Mat to Texture2D
        Utils.matToTexture2D(outputMat, tempOutputTexture);*/

        // Update the output RenderTexture
        Graphics.Blit(inputTexture, outputTexture);
    }

    void OnGUI()
    {
        if (visualizeResult && outputTexture != null)
        {
            GUI.DrawTexture(new UnityEngine.Rect(xpos, ypos, tex_size, tex_size), outputTexture);
            GUI.Label(new UnityEngine.Rect(xpos + 10, ypos + 10, 200, 20), "Blob Tracking", new GUIStyle { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } });
        }
    }

    void OnDestroy()
    {
        if (outputTexture != null && outputTexture.IsCreated())
        {
            outputTexture.Release();
        }
    }
}