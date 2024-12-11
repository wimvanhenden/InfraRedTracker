using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;


public class CubeGrid : MonoBehaviour
{
    public int gridSize = 30;
    public float cubeSize = 1f;
    public float animationDuration = 0.3f;
    public float riseHeight = 0.5f;

    public GameObject cubePrefab; // Add a public variable to hold the prefab

    private GameObject[,] cubes;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    private TrackerData trackerdata;

    

    public int defaultWidth = 960;  // Set this to your desired default width
    public int defaultHeight = 600; // Set this to your desired default height
    public bool isFullscreen = false; // Set if you want fullscreen or windowed

    private string configID; // Variable to store the ID

    void Start()
    {
        CreateGrid();

        // Fetch and store the ID from tConfigManager
        configID = tConfigManager.Instance.TrackerConfig.id.ToString();

        PlayerPrefs.DeleteKey("Screenmanager Resolution Width");
        PlayerPrefs.DeleteKey("Screenmanager Resolution Height");
        PlayerPrefs.DeleteKey("Screenmanager Is Fullscreen mode");

        Screen.SetResolution(defaultWidth, defaultHeight, isFullscreen);
    }

    void CreateGrid()
    {
        cubes = new GameObject[gridSize, gridSize];

        // Generate a single random color for all cubes
        Color randomColor = GetRandomColor();

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 position = new Vector3(x * cubeSize, 0, z * cubeSize);

                // Instantiate the prefab instead of using CreatePrimitive
                GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);

                // Adjust the scale according to the cubeSize
                cube.transform.localScale = Vector3.one * cubeSize;

                // Change the color of the prefab's material to the same random color
                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = randomColor;  // Use the same color for all cubes
                }

                cubes[x, z] = cube;
                originalPositions[cube] = position;
            }
        }
    }

    // Method to generate a random color
    Color GetRandomColor()
    {
        return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }

    void Update()
    {
        Vector3 _mousedata = UnityEngine.Input.mousePosition;

        Ray ray = Camera.main.ScreenPointToRay(_mousedata);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.transform.gameObject;
            if (Mathf.Abs(hitObject.transform.position.y - originalPositions[hitObject].y) < 0.01f)
            {
                StartCoroutine(AnimateCube(hitObject, true));
            }
        }

        //Debug.Log(trackerdata == null);

        if (trackerdata != null)
        {
            int screen_width = Screen.width;
            int screen_height = Screen.height;

            float xround = Mathf.Round(trackerdata.x * 100f) / 100f;
            float yround = Mathf.Round(trackerdata.y * 100f) / 100f;

            float newx = math.remap(0, 1, 0, screen_width, xround);
            float newy = math.remap(0, 1, 0, screen_height, yround);

            var _trackerdata = new Vector3()
            {
                x = newx,
                y = newy
            };

            Debug.Log(_trackerdata);

            Ray _ray = Camera.main.ScreenPointToRay(_trackerdata);
            RaycastHit _hit;

            if (Physics.Raycast(_ray, out _hit))
            {
                GameObject hitObject = _hit.transform.gameObject;
                if (Mathf.Abs(hitObject.transform.position.y - originalPositions[hitObject].y) < 0.01f)
                {
                    StartCoroutine(AnimateCube(hitObject, true));
                }
            }
        }
    }

    IEnumerator AnimateCube(GameObject cube, bool rising)
    {
        Vector3 startPos = cube.transform.position;
        Vector3 endPos = rising
            ? new Vector3(originalPositions[cube].x, originalPositions[cube].y + riseHeight, originalPositions[cube].z)
            : originalPositions[cube];
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            cube.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cube.transform.position = endPos;

        if (rising)
        {
            StartCoroutine(WaitAndLower(cube));
        }
    }

    IEnumerator WaitAndLower(GameObject cube)
    {
        yield return new WaitUntil(() => !IsMouseOverCube(cube));
        StartCoroutine(AnimateCube(cube, false));
    }

    bool IsMouseOverCube(GameObject cube)
    {
        Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.transform.gameObject == cube;
        }

        return false;
    }

    // Use OnGUI to display the ID
    void OnGUI()
    {
        // Draw the text in the upper left corner (top 10 pixels down, 10 pixels in from the left)
        GUI.Label(new Rect(10, 10, 200, 30), "ID: " + configID);
    }

    void OnEnable()
    {
        tTrackerWebSocket.OnTrackerDataReceived += handleTrackerData;
    }

    void OnDisable()
    {
        tTrackerWebSocket.OnTrackerDataReceived -= handleTrackerData;
    }

    void handleTrackerData(TrackerData _data)
    {
        trackerdata = _data;
    }
}
