using UnityEngine;
using UnityEngine.UI;

public class ScanLineAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Speed of the scan line movement in units per second")]
    [Range(1f, 20f)]
    public float scanSpeed = 5f;
    
    [Tooltip("Percentage of vertical space to use (0-1)")]
    [Range(0.1f, 1f)]
    public float scanHeightPercentage = 0.8f;
    
    [Tooltip("Padding from the top of scan area")]
    public float topPadding = 50f;
    
    [Tooltip("Padding from the bottom of scan area")]
    public float bottomPadding = 50f;
    
    [Header("References")]
    [Tooltip("Reference to the camera view (WebcamFeed) RectTransform")]
    public RectTransform cameraViewRect;
    
    // Private variables
    private RectTransform rectTransform;
    private float topPosition = 200f;     // Default values in case calculation fails
    private float bottomPosition = -200f;  // Default values in case calculation fails
    private Canvas parentCanvas;
    private bool isInitialized = false;
    
    void Awake()
    {
        // Get the RectTransform early, but don't rely on its values yet
        rectTransform = GetComponent<RectTransform>();
    }
    
    void Start()
    {
        // Find parent canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("ScanLineAnimation: No parent Canvas found!");
        }
        
        // Find camera view if not assigned
        if (cameraViewRect == null)
        {
            GameObject webcamFeed = GameObject.Find("WebcamFeed");
            if (webcamFeed != null)
            {
                cameraViewRect = webcamFeed.GetComponent<RectTransform>();
                Debug.Log("ScanLineAnimation: Found WebcamFeed automatically");
            }
            else
            {
                // Try to find any object with FullscreenCameraController
                FullscreenCameraController cameraController = FindObjectOfType<FullscreenCameraController>();
                if (cameraController != null)
                {
                    cameraViewRect = cameraController.GetComponent<RectTransform>();
                    Debug.Log("ScanLineAnimation: Found camera through FullscreenCameraController");
                }
            }
            
            if (cameraViewRect == null)
            {
                Debug.LogError("ScanLineAnimation: Camera view RectTransform not found!");
            }
        }
        
        // Set initial size with default values
        if (rectTransform != null)
        {
            float defaultWidth = 400f;
            float defaultHeight = rectTransform.sizeDelta.y > 0 ? rectTransform.sizeDelta.y : 4f;
            rectTransform.sizeDelta = new Vector2(defaultWidth, defaultHeight);
        }
        
        // Wait for a frame to ensure all RectTransforms are initialized
        isInitialized = true;
        
        // Calculate positions after everything is initialized
        CalculateScanBoundaries();
        
        // Start position at the top
        ResetToTop();
    }
    
    void OnRectTransformDimensionsChange()
    {
        // Only respond to dimension changes after initialization
        if (isInitialized)
        {
            CalculateScanBoundaries();
        }
    }
    
    void Update()
    {
        // Recalculate occasionally to handle any orientation changes
        if (Time.frameCount % 60 == 0 && isInitialized)
        {
            CalculateScanBoundaries();
        }
        
        // Get current position
        Vector2 position = rectTransform.anchoredPosition;
        
        // Move down only
        position.y -= scanSpeed * Time.deltaTime * 100;
        
        // If we've reached the bottom, reset to top
        if (position.y <= bottomPosition)
        {
            ResetToTop();
            return;
        }
        
        // Apply the new position
        rectTransform.anchoredPosition = position;
    }
    
    private void ResetToTop()
    {
        Vector2 position = rectTransform.anchoredPosition;
        position.y = topPosition;
        rectTransform.anchoredPosition = position;
    }
    
    private void CalculateScanBoundaries()
    {
        try
        {
            // Safety check
            if (cameraViewRect == null || rectTransform == null)
            {
                Debug.LogWarning("ScanLineAnimation: Missing references for boundary calculation");
                return;
            }
            
            // Get canvas dimensions
            float canvasHeight = 1080f; // Default fallback
            
            if (parentCanvas != null && parentCanvas.GetComponent<RectTransform>() != null)
            {
                canvasHeight = parentCanvas.GetComponent<RectTransform>().rect.height;
            }
            
            // Calculate scan area
            float scanAreaHeight = canvasHeight * scanHeightPercentage;
            topPosition = scanAreaHeight / 2f - topPadding;
            bottomPosition = -scanAreaHeight / 2f + bottomPadding;
            
            // Get the camera view width
            float cameraWidth = cameraViewRect.rect.width;
            if (cameraWidth <= 0)
            {
                // Fallback if camera width isn't available
                cameraWidth = Screen.width * 0.8f;
            }
            
            // Update scan line width
            float lineWidth = cameraWidth - 40f; // 20px padding on each side
            if (lineWidth <= 0) lineWidth = 400f; // Fallback
            
            // Safely get current height without assuming sizeDelta is valid
            float currentHeight = 4f; // Default height
            if (rectTransform.sizeDelta.y > 0)
            {
                currentHeight = rectTransform.sizeDelta.y;
            }
            
            // Update dimensions
            rectTransform.sizeDelta = new Vector2(lineWidth, currentHeight);
            
            // Center horizontally
            Vector2 position = rectTransform.anchoredPosition;
            position.x = 0;
            rectTransform.anchoredPosition = position;
            
            Debug.Log($"ScanLineAnimation: Calculated boundaries - Top: {topPosition}, Bottom: {bottomPosition}, Width: {lineWidth}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ScanLineAnimation: Error in CalculateScanBoundaries: {e.Message}");
            
            // Use default values as fallback
            topPosition = 200f;
            bottomPosition = -200f;
        }
    }
}
/* 
using UnityEngine;
using UnityEngine.UI;

public class ScanLineAnimation : MonoBehaviour
{
    public float scanSpeed = 5f;
    public float topPosition = 300f;
    public float bottomPosition = -1400f;
    
    private RectTransform rectTransform;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // Start position at the top
        ResetToTop();
    }
    
    void Update()
    {
        // Get current position
        Vector2 position = rectTransform.anchoredPosition;
        
        // Move down only
        position.y -= scanSpeed * Time.deltaTime * 100;
        
        // If we've reached the bottom, reset to top
        if (position.y <= bottomPosition)
        {
            ResetToTop();
            return;
        }
        
        // Apply the new position
        rectTransform.anchoredPosition = position;
    }
    
    private void ResetToTop()
    {
        Vector2 position = rectTransform.anchoredPosition;
        position.y = topPosition;
        rectTransform.anchoredPosition = position;
    }
}
*/
