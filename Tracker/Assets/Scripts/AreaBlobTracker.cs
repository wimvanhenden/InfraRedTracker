using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.IO;


[Serializable]
public class Tracker
{
    public float x;
    public float y;
    public int id = 1;
}


[Serializable]
public class SerializableVector2
{
    public float x;
    public float y;

    // Constructor to create a SerializableVector2 from a Unity Vector2
    public SerializableVector2(Vector2 vector)
    {
        x = vector.x;
        y = vector.y;
    }

    // Convert back to Unity's Vector2
    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}

[Serializable]
public class SerializableRect
{
    public float x;
    public float y;
    public float width;
    public float height;

    // Constructor to create a SerializableRect from a Unity Rect
    public SerializableRect(UnityEngine.Rect rect)
    {
        x = rect.x;
        y = rect.y;
        width = rect.width;
        height = rect.height;
    }

    // Convert back to Unity's Rect
    public UnityEngine.Rect ToRect()
    {
        return new UnityEngine.Rect(x, y, width, height);
    }
}

[System.Serializable]
public class Area
{
    public int id;          // This is the internal id for the area
    public int customId;     // This is the user-defined custom ID (input by the user)
    public List<Vector2> points;
    public UnityEngine.Rect normalizedRect;
    public Color color;

    public Area(int id, Color color)
    {
        this.id = id;
        this.color = color;
        this.points = new List<Vector2>();
        this.customId = -1; // Default value, will be set by the user
    }
}
[Serializable]
public class SerializableArea
{
    public int id;
    public int customId;
    public List<SerializableVector2> points;
    public SerializableRect normalizedRect;

    // This class no longer relies on an Area object; it's purely a data holder
    public SerializableArea() { }

    // This method converts an Area object to SerializableArea
    public static SerializableArea FromArea(Area area)
    {
        SerializableArea serializedArea = new SerializableArea();
        serializedArea.id = area.id;
        serializedArea.customId = area.customId;

        // Convert points
        serializedArea.points = new List<SerializableVector2>();
        foreach (var point in area.points)
        {
            serializedArea.points.Add(new SerializableVector2(point));
        }

        // Convert normalizedRect
        serializedArea.normalizedRect = new SerializableRect(area.normalizedRect);

        return serializedArea;
    }

    // This method converts SerializableArea back into an Area
    public Area ToArea(Color color)
    {
        Area area = new Area(id, color);
        area.customId = customId;

        // Convert points back to Vector2
        area.points = new List<Vector2>();
        foreach (var point in points)
        {
            area.points.Add(point.ToVector2());
        }

        // Convert normalizedRect back to Rect
        area.normalizedRect = normalizedRect.ToRect();

        return area;
    }
}




namespace com.tool
{
    public class AreaBlobTracker : MonoBehaviour
    {
        public RenderTexture inputTexture;
        public RenderTexture outputTexture;

        public float minContourArea = 100f;
        public float maxContourArea = 1000f;

        public KinectIRUndistortProcessor undistortProcessor;

        public bool visualizeResult = true;
        public float xpos = 0;
        public float ypos = 0;
        public float tex_size = 512;

        public float _lowthresh = 0;
        public float _highthresh = 255;

        [Tooltip("Number of areas that can be set")]
        public int maxAreas = 3;

        private bool isSettingArea = false;
        private List<Area> areas = new List<Area>();
        private int currentAreaIndex = -1;
        private int selectedAreaIndex = -1; // Index for the selected area

        private Mat inputMat;
        private Mat grayMat;
        private Mat binaryMat;
        private Mat outputMat;

        private Texture2D tempInputTexture;
        private Texture2D tempOutputTexture;

        private int areaIdInput = 0; // Input field for custom Area ID

        public List<Tracker> trackers;
        // Reference to the WebSocket client
        public tTrackerWebSocket websocketClient;

        private Texture2D pointTexture;
        private Texture2D lineTex;

        // New fields for text inputs
        private string minIRValueString;
        private string maxIRValueString;

        // New fields for text inputs
        private string minContourAreaString;
        private string maxContourAreaString;

        private bool showInputFields = false;


        void Start()
        {
            // Initialize matrices
            inputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);
            grayMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC1);
            binaryMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC1);
            outputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);

            // Create temporary textures
            tempInputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGBAFloat, false);
            tempOutputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGBAFloat, false);

            // Create output RenderTexture if not provided
            if (outputTexture == null)
            {
                outputTexture = new RenderTexture(inputTexture.width, inputTexture.height, 0, inputTexture.format);
                outputTexture.Create();
            }

            websocketClient = FindObjectOfType<tTrackerWebSocket>();
            pointTexture = new Texture2D(1, 1);
            lineTex = new Texture2D(1, 1);


            minIRValueString = undistortProcessor.minIRValue.ToString();
            maxIRValueString = undistortProcessor.maxIRValue.ToString();

            minContourAreaString = minContourArea.ToString();
            maxContourAreaString = maxContourArea.ToString();

            LoadAreas();
        }

        void Update()
        {
            // Read the input RenderTexture into the temporary Texture2D
            RenderTexture.active = inputTexture;
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


            trackers.Clear();

            // Find contours
            using (Mat hierarchy = new Mat())
            {
                List<MatOfPoint> contours = new List<MatOfPoint>();
                Imgproc.findContours(binaryMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

                List<MatOfPoint> validContours = new List<MatOfPoint>();

                foreach (MatOfPoint contour in contours)
                {
                    double area = Imgproc.contourArea(contour);
                    if (area >= minContourArea && area <= maxContourArea)
                    {
                        validContours.Add(contour);
                    }
                }

                // Process all valid contours
                for (int i = 0; i < validContours.Count; i++)
                {
                    // Get the center of the contour
                    Moments moments = Imgproc.moments(validContours[i]);

                    float cx = (float)(moments.get_m10() / moments.get_m00());
                    float cy = (float)(moments.get_m01() / moments.get_m00());

                    //Get the corners
                    //Core.ApproxPolyDP(validContours[i], Imgproc.ArcLength(contours[i], true) * 0.02, true);

                    //Imgproc.approxPolyDP(validContours[i], Imgproc.arcLength(validContours[i], true) * 0.02, true);

                    // Convert to normalized coordinates (0-1 range)
                    Vector2 normalizedPoint = new Vector2(cx / inputTexture.width, cy / inputTexture.height);

                    // Draw the contour
                    Imgproc.drawContours(outputMat, validContours, i, new Scalar(0, 255, 0, 255), 1);

                    // Draw the center point
                    Imgproc.circle(outputMat, new OpenCVForUnity.CoreModule.Point(cx, cy), 2, new Scalar(255, 0, 0, 255), -1);

                    // Check if the point is in any defined area
                    string coordinatesText = "";
                    foreach (var area in areas)
                    {
                        if (area.normalizedRect.width > 0 && area.normalizedRect.height > 0 && IsPointInPolygon(normalizedPoint, area.points))
                        {
                            Vector2 areaCoordinates = NormalizePointToArea(normalizedPoint, area);

                            Tracker _tracker = new Tracker();
                            _tracker.id = area.customId;
                            _tracker.x = areaCoordinates.x;
                            _tracker.y = areaCoordinates.y;

                            trackers.Add(_tracker);
                            coordinatesText += $"Area {area.customId}: ({areaCoordinates.x:F2}, {areaCoordinates.y:F2})\n";
                        }
                    }

                    // If coordinates were found, display them
                    if (!string.IsNullOrEmpty(coordinatesText))
                    {
                        Imgproc.putText(outputMat, coordinatesText.TrimEnd('\n'), new OpenCVForUnity.CoreModule.Point(cx, cy + 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.3, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
                        //Debug.Log($"Contour {i} center: {coordinatesText.TrimEnd('\n')}");
                    }
                }

                // Clean up
                foreach (MatOfPoint contour in contours)
                {
                    contour.Dispose();
                }
            }

            // Convert Mat to Texture2D
            Utils.matToTexture2D(outputMat, tempOutputTexture);

            // Update the output RenderTexture
            Graphics.Blit(tempOutputTexture, outputTexture);

            // Send the tracker list via WebSocket every update
            if (websocketClient != null)
            {
                websocketClient.SendTrackerList(trackers);
            }



        }

        private string GetSaveFilePath()
        {
            ConfigData data = tConfigManager.Instance.Config;
            int _id = data.id;
            return Path.Combine(Application.persistentDataPath, "areas" + _id.ToString() + ".json");
        }
        private void AutoSave()
        {
            SaveAreas();
        }
        private void SaveAreas()
        {
            try
            {
                List<SerializableArea> serializedAreas = new List<SerializableArea>();
                foreach (var area in areas)
                {
                    if (area.points.Count > 0) // Only save areas with points
                    {
                        Debug.Log($"Saving Area ID: {area.id}, CustomID: {area.customId}");
                        // Use the FromArea static method for conversion
                        serializedAreas.Add(SerializableArea.FromArea(area));
                    }
                }

                string json = JsonConvert.SerializeObject(serializedAreas, Formatting.Indented);
                File.WriteAllText(GetSaveFilePath(), json);

                Debug.Log("Areas saved to " + GetSaveFilePath());
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving areas: " + ex.Message);
            }
        }

        private void LoadAreas()
        {
            string filePath = GetSaveFilePath();
            int maxAreas = 4;  // Define the maximum number of areas allowed

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    //Debug.Log("Loaded JSON: " + json);  // Log the raw JSON for debugging

                    if (json.TrimStart().StartsWith("["))
                    {
                        List<SerializableArea> serializedAreas = JsonConvert.DeserializeObject<List<SerializableArea>>(json);
                        Debug.Log($"Found {serializedAreas.Count} areas in saved file.");

                        Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

                        // Clear only if there are saved areas
                        areas.Clear();

                        // Load areas from the saved file
                        foreach (var serializedArea in serializedAreas)
                        {
                            int colorIndex = serializedArea.id % colors.Length;

                            Debug.Log($"Deserializing Area ID: {serializedArea.id}, CustomID: {serializedArea.customId}");

                            areas.Add(serializedArea.ToArea(colors[colorIndex]));
                        }

                        // Fill up to maxAreas with empty areas if there are less than maxAreas
                        while (areas.Count < maxAreas)
                        {
                            int newAreaId = areas.Count;  // New area ID
                            areas.Add(new Area(newAreaId, colors[newAreaId % colors.Length]));  // Create a new empty area
                            Debug.Log($"Created empty Area ID: {newAreaId} to fill up to {maxAreas} areas.");
                        }

                        Debug.Log("Areas successfully loaded from " + filePath);
                    }
                    else
                    {
                        Debug.LogError("Invalid JSON format: Expected an array but got something else.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error loading areas: " + ex.Message);
                }
            }
            else
            {
                Debug.Log("No saved area data found. Starting fresh.");

                // Create maxAreas empty areas on the first run
                Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
                areas.Clear();

                for (int i = 0; i < maxAreas; i++)
                {
                    areas.Add(new Area(i, colors[i % colors.Length]));
                }

                Debug.Log($"Initialized {maxAreas} empty areas.");
            }
        }


        void OnGUI()
        {
            if (visualizeResult && outputTexture != null)
            {
                GUI.DrawTexture(new UnityEngine.Rect(xpos, ypos, tex_size, tex_size), outputTexture);
            }

            UnityEngine.Rect textureRect = new UnityEngine.Rect(xpos, ypos, tex_size, tex_size);

            // First render the reset button to ensure no overlap with other elements
            if (selectedAreaIndex != -1)
            {
                if (GUI.Button(new UnityEngine.Rect(10, 50, 150, 30), "Clear Selected Area"))
                {
                    ResetSelectedArea();
                }
            }

            // Start/Finish setting area button
            if (GUI.Button(new UnityEngine.Rect(10, 10, 150, 30), isSettingArea ? "Finish Setting Area" : "Start Setting Area"))
            {
                isSettingArea = !isSettingArea;
                if (isSettingArea)
                {
                    currentAreaIndex = GetNextEmptyAreaIndex();
                    if (currentAreaIndex != -1)
                    {
                        areas[currentAreaIndex].points.Clear();
                        areaIdInput = 0; // Reset the input field for a new area
                    }
                    else
                    {
                        Debug.LogWarning("Maximum number of areas reached.");
                        isSettingArea = false;
                    }
                }

            }


            // Input field for Area ID when setting a new area
            if (isSettingArea && currentAreaIndex != -1)
            {
                GUI.Label(new UnityEngine.Rect(10, 50, 150, 30), "Set Custom Area ID:");
                string areaIdString = areaIdInput.ToString();  // Convert the integer to a string for display
                areaIdString = GUI.TextField(new UnityEngine.Rect(10, 80, 30, 20), areaIdString, 10);

                // Try to convert the input back to an integer
                if (int.TryParse(areaIdString, out int result))
                {
                    areaIdInput = result;
                }
            }

            // Display all set areas and handle selection logic
            for (int i = 0; i < areas.Count; i++)
            {
                DisplayArea(textureRect, areas[i], i == selectedAreaIndex); // Highlight the selected area
            }

            // Handle mouse input for setting area points
            Event e = Event.current;
            if (isSettingArea && e.type == EventType.MouseDown && e.button == 0)
            {
                if (textureRect.Contains(e.mousePosition) && currentAreaIndex != -1)
                {
                    Vector2 normalizedPoint = new Vector2(
                        (e.mousePosition.x - textureRect.x) / textureRect.width,
                        (e.mousePosition.y - textureRect.y) / textureRect.height
                    );
                    areas[currentAreaIndex].points.Add(normalizedPoint);

                    if (areas[currentAreaIndex].points.Count == 4)
                    {
                        isSettingArea = false;
                        CalculateNormalizedArea(currentAreaIndex);
                        areas[currentAreaIndex].customId = areaIdInput; // Assign the custom ID
                        Debug.Log($"Area {currentAreaIndex} is assigned customId: {areas[currentAreaIndex].customId}");
                        AutoSave();
                    }
                }
            }
            else if (!isSettingArea && e.type == EventType.MouseDown && e.button == 0) // Click to select an area
            {
                if (textureRect.Contains(e.mousePosition))
                {
                    Vector2 clickPoint = new Vector2(
                        (e.mousePosition.x - textureRect.x) / textureRect.width,
                        (e.mousePosition.y - textureRect.y) / textureRect.height
                    );

                    // Find which area was clicked
                    selectedAreaIndex = -1;
                    for (int i = 0; i < areas.Count; i++)
                    {
                        if (areas[i].points.Count == 4 && IsPointInPolygon(clickPoint, areas[i].points))
                        {
                            selectedAreaIndex = i;
                            Debug.Log($"Area {areas[i].customId} selected.");
                            break;
                        }
                    }
                }
            }

            // Display normalized coordinates on mouse hover
            if (!isSettingArea && textureRect.Contains(e.mousePosition))
            {
                Vector2 normalizedPoint = new Vector2(
                    (e.mousePosition.x - textureRect.x) / textureRect.width,
                    (e.mousePosition.y - textureRect.y) / textureRect.height
                );

                string coordinatesText = "";
                foreach (var area in areas)
                {
                    if (area.normalizedRect.width > 0 && area.normalizedRect.height > 0 && IsPointInPolygon(normalizedPoint, area.points))
                    {
                        Vector2 areaCoordinates = NormalizePointToArea(normalizedPoint, area);
                        coordinatesText += $"Area {area.customId}: ({areaCoordinates.x:F2}, {areaCoordinates.y:F2})\n"; // Display custom ID
                    }
                }

                if (!string.IsNullOrEmpty(coordinatesText))
                {
                    GUI.Label(new UnityEngine.Rect(e.mousePosition.x + 10, e.mousePosition.y + 10, 300, 60), coordinatesText);
                }
            }


            // Toggle button above the input fields
            GUILayout.BeginArea(new UnityEngine.Rect(Screen.width - 220, 10, 200, 30));
            GUI.color = Color.white;
            if (GUILayout.Button(showInputFields ? "Hide Inputs" : "Show Inputs"))
            {
                showInputFields = !showInputFields; // Toggle visibility
            }
            GUILayout.EndArea();

            // If the input fields should be visible
            if (showInputFields)
            {
                // Upper-right corner input fields for minIRValue, maxIRValue, minContourArea, and maxContourArea
                GUILayout.BeginArea(new UnityEngine.Rect(Screen.width - 220, 50, 200, 500));
                GUI.color = Color.white;

                // Checkbox to toggle doUndistortion inside the input fields area
                undistortProcessor.doUndistortion = GUILayout.Toggle(undistortProcessor.doUndistortion, "Undistort");

                GUILayout.Label("Min IR Value:");
                minIRValueString = GUILayout.TextField(minIRValueString, 10);
                if (float.TryParse(minIRValueString, out float parsedMinIR))
                {
                    undistortProcessor.minIRValue = parsedMinIR;
                }

                GUILayout.Label("Max IR Value:");
                maxIRValueString = GUILayout.TextField(maxIRValueString, 10);
                if (float.TryParse(maxIRValueString, out float parsedMaxIR))
                {
                    undistortProcessor.maxIRValue = parsedMaxIR;
                }

                GUILayout.Label("Min Contour Value:");
                minContourAreaString = GUILayout.TextField(minContourAreaString, 10);
                if (float.TryParse(minContourAreaString, out float parsedMinContour))
                {
                    minContourArea = parsedMinContour;
                }

                GUILayout.Label("Max Contour Value:");
                maxContourAreaString = GUILayout.TextField(maxContourAreaString, 10);
                if (float.TryParse(maxContourAreaString, out float parsedMaxContour))
                {
                    maxContourArea = parsedMaxContour;
                }

                GUILayout.EndArea();

            }


        }



        private void DisplayArea(UnityEngine.Rect textureRect, Area area, bool isSelected)
        {
            if (area.points.Count == 0) return;

            Color displayColor = isSelected ? Color.yellow : area.color; // Highlight the selected area
                                                                         //Texture2D pointTexture = new Texture2D(1, 1);
            pointTexture.SetPixel(0, 0, displayColor);
            pointTexture.Apply();

            for (int i = 0; i < area.points.Count; i++)
            {
                Vector2 screenPoint = new Vector2(
                    textureRect.x + area.points[i].x * textureRect.width,
                    textureRect.y + area.points[i].y * textureRect.height
                );
                GUI.DrawTexture(new UnityEngine.Rect(screenPoint.x - 5, screenPoint.y - 5, 10, 10), pointTexture);

                if (i > 0)
                {
                    Vector2 prevPoint = new Vector2(
                        textureRect.x + area.points[i - 1].x * textureRect.width,
                        textureRect.y + area.points[i - 1].y * textureRect.height
                    );
                    DrawLine(prevPoint, screenPoint, displayColor);
                }
            }

            if (area.points.Count == 4)
            {
                Vector2 lastPoint = new Vector2(
                    textureRect.x + area.points[3].x * textureRect.width,
                    textureRect.y + area.points[3].y * textureRect.height
                );
                Vector2 firstPoint = new Vector2(
                    textureRect.x + area.points[0].x * textureRect.width,
                    textureRect.y + area.points[0].y * textureRect.height
                );
                DrawLine(lastPoint, firstPoint, displayColor);
            }

            // Display area custom ID
            if (area.points.Count > 0)
            {
                Vector2 labelPos = new Vector2(
                    textureRect.x + area.points[0].x * textureRect.width,
                    textureRect.y + area.points[0].y * textureRect.height
                );
                GUI.Label(new UnityEngine.Rect(labelPos.x, labelPos.y - 20, 100, 20), $"ID: {area.customId}");
            }
        }

        private void ResetSelectedArea()
        {
            if (selectedAreaIndex != -1)
            {
                areas[selectedAreaIndex].points.Clear();
                areas[selectedAreaIndex].customId = -1;  // Reset customId or keep it (optional)
                areas[selectedAreaIndex].normalizedRect = new UnityEngine.Rect(); // Reset the normalized rectangle

                selectedAreaIndex = -1;

                AutoSave();

            }
        }


        private void CalculateNormalizedArea(int areaIndex)
        {
            Area area = areas[areaIndex];
            float minX = Mathf.Min(area.points[0].x, area.points[1].x, area.points[2].x, area.points[3].x);
            float minY = Mathf.Min(area.points[0].y, area.points[1].y, area.points[2].y, area.points[3].y);
            float maxX = Mathf.Max(area.points[0].x, area.points[1].x, area.points[2].x, area.points[3].x);
            float maxY = Mathf.Max(area.points[0].y, area.points[1].y, area.points[2].y, area.points[3].y);

            area.normalizedRect = new UnityEngine.Rect(minX, minY, maxX - minX, maxY - minY);

        }

        private Vector2 NormalizePointToArea(Vector2 point, Area area)
        {
            return new Vector2(
                Mathf.InverseLerp(area.normalizedRect.xMin, area.normalizedRect.xMax, point.x),
                Mathf.InverseLerp(area.normalizedRect.yMin, area.normalizedRect.yMax, point.y)
            );
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {

            lineTex.SetPixel(0, 0, color);
            lineTex.Apply();

            Vector2 direction = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            Matrix4x4 matrixBackup = GUI.matrix;
            GUI.color = color;

            GUIUtility.RotateAroundPivot(
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                start
            );

            GUI.DrawTexture(new UnityEngine.Rect(start.x, start.y, distance, 2), lineTex);
            GUI.matrix = matrixBackup;
        }

        private int GetNextEmptyAreaIndex()
        {
            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].points.Count == 0)
                    return i;
            }
            return -1;
        }

        private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        void OnDestroy()
        {
            // Dispose of OpenCV Mats
            inputMat?.Dispose();
            grayMat?.Dispose();
            binaryMat?.Dispose();
            outputMat?.Dispose();

            if (lineTex != null)
            {
                Destroy(lineTex);
            }

            if (pointTexture != null)
            {
                Destroy(pointTexture);
            }

            // Release textures
            if (tempInputTexture != null)
            {
                Destroy(tempInputTexture);
            }

            if (tempOutputTexture != null)
            {
                Destroy(tempOutputTexture);
            }

            if (outputTexture != null && outputTexture.IsCreated())
            {
                outputTexture.Release();
            }
        }
    }
}
