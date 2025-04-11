using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(AspectRatioFitter))]
public class FullscreenCameraController : MonoBehaviour
{
    [Tooltip("Set to true to enable debug information in the console")]
    [SerializeField] private bool debugMode = true;
    
    [Tooltip("When enabled, the camera will fill the entire screen regardless of aspect ratio")]
    [SerializeField] private bool fillEntireScreen = true;
    
    [Tooltip("What camera device to use (leave empty for default)")]
    [SerializeField] private string specificCameraDevice = "";
    
    [Tooltip("Camera resolution - higher values may impact performance")]
    [SerializeField] private Vector2 targetResolution = new Vector2(1280, 720);
    
    [Tooltip("Use front-facing (selfie) camera")]
    [SerializeField] private bool useFrontCamera = false;
    
    [Tooltip("Camera feed framerate")]
    [Range(15, 60)]
    [SerializeField] private int frameRate = 30;
    
    // References to required components
    private RawImage displayImage;
    private AspectRatioFitter aspectFitter;
    private WebCamTexture cameraTexture;
    private RectTransform rectTransform;
    public bool allowReadAccess = true;
    
    // Event to notify when camera is switched
    public delegate void CameraSwitchEvent(bool isFrontCamera);
    public event CameraSwitchEvent OnCameraSwitch;
    
    // Status properties
    public bool IsCameraInitialized => cameraTexture != null && cameraTexture.isPlaying;
    public bool IsUsingFrontCamera() => useFrontCamera;
    
    // Initial setup in Awake
    private void Awake()
    {
        // Get components
        displayImage = GetComponent<RawImage>();
        aspectFitter = GetComponent<AspectRatioFitter>();
        rectTransform = GetComponent<RectTransform>();
        
        // Configure RectTransform for fullscreen
        MakeFullscreen();
        
        // Configure aspect ratio fitter
        ConfigureAspectRatioFitter();
    }
    
    private void Start()
    {
        // Initialize the camera
        InitializeCamera();
    }
    
    private void OnEnable()
    {
        // Start camera when component is enabled
        if (cameraTexture != null && !cameraTexture.isPlaying)
        {
            cameraTexture.Play();
        }
    }
    
    private void OnDisable()
    {
        // Stop camera when component is disabled
        if (cameraTexture != null && cameraTexture.isPlaying)
        {
            cameraTexture.Stop();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up when destroyed
        if (cameraTexture != null)
        {
            cameraTexture.Stop();
        }
    }
    
    private void Update()
    {
        // Only process if we have a valid camera texture that's playing
        if (cameraTexture != null && cameraTexture.isPlaying && cameraTexture.didUpdateThisFrame)
        {
            UpdateCameraDisplay();
        }
    }
    
    // Make the RawImage fill the entire parent
    private void MakeFullscreen()
    {
        if (rectTransform != null)
        {
            // Stretch to fill parent
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Set color to transparent white (RGBA: 255,255,255,0)
            // This allows the camera texture to be processed but not displayed
            displayImage.color = new Color(1, 1, 1, 0);
            
            if (debugMode)
            {
                Debug.Log("[FullscreenCameraController] RawImage configured for fullscreen (transparent)");
            }
            
            // Make sure parent Canvas is properly configured
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                CanvasScaler scaler = parentCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    // Configure for proper scaling on all devices
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 0.5f;
                    
                    if (debugMode)
                    {
                        Debug.Log("[FullscreenCameraController] Canvas configured for proper scaling");
                    }
                }
            }
        }
    }
    
    // Configure the AspectRatioFitter based on settings
    private void ConfigureAspectRatioFitter()
    {
        if (aspectFitter != null)
        {
            if (fillEntireScreen)
            {
                // Fill entire screen (might crop some of camera view)
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            }
            else
            {
                // Preserve aspect ratio (might show letterboxing)
                aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
            
            // Initial aspect ratio (will be updated when camera starts)
            aspectFitter.aspectRatio = 16f / 9f;
        }
    }
    
    // Initialize the camera
    private void InitializeCamera()
    {
        // Get all available camera devices
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("[FullscreenCameraController] No camera devices found!");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FullscreenCameraController] Found {devices.Length} camera devices:");
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"  Device {i}: {devices[i].name}, Front-facing: {devices[i].isFrontFacing}");
            }
        }
        
        // Select appropriate camera device
        WebCamDevice selectedDevice = default;
        bool deviceFound = false;
        
        // First try to find by name if specified
        if (!string.IsNullOrEmpty(specificCameraDevice))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name == specificCameraDevice)
                {
                    selectedDevice = devices[i];
                    deviceFound = true;
                    break;
                }
            }
        }
        
        // If not found by name, try by front/back preference
        if (!deviceFound)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].isFrontFacing == useFrontCamera)
                {
                    selectedDevice = devices[i];
                    deviceFound = true;
                    break;
                }
            }
        }
        
        // If still not found, use first available
        if (!deviceFound && devices.Length > 0)
        {
            selectedDevice = devices[0];
            deviceFound = true;
        }
        
        if (!deviceFound)
        {
            Debug.LogError("[FullscreenCameraController] Could not find suitable camera device");
            return;
        }
        
        // Stop any existing camera
        if (cameraTexture != null && cameraTexture.isPlaying)
        {
            cameraTexture.Stop();
        }
        
        // Calculate optimal resolution
        Vector2 resolution = GetOptimalResolution(targetResolution);
        
        if (debugMode)
        {
            Debug.Log($"[FullscreenCameraController] Using camera: {selectedDevice.name} with resolution {resolution.x}x{resolution.y}");
        }
        
        // Create new camera texture
        cameraTexture = new WebCamTexture(
            selectedDevice.name,
            (int)resolution.x,
            (int)resolution.y,
            frameRate
        );
        
        // Add this line to enable texture read access:
        cameraTexture.requestedWidth = (int)resolution.x;
        cameraTexture.requestedHeight = (int)resolution.y;

        // Assign to display
        displayImage.texture = cameraTexture;
        
        // Start the camera
        cameraTexture.Play();
        
        // Log camera status
        if (debugMode)
        {
            StartCoroutine(LogCameraStatus());
        }
    }
    
    // Periodically log camera status (for debugging)
    private System.Collections.IEnumerator LogCameraStatus()
    {
        yield return new WaitForSeconds(1.0f);
        
        if (cameraTexture != null && cameraTexture.isPlaying)
        {
            Debug.Log($"[FullscreenCameraController] Camera is active: {cameraTexture.width}x{cameraTexture.height}, " +
                      $"Device: {cameraTexture.deviceName}, Front-facing: {useFrontCamera}");
        }
        else
        {
            Debug.LogWarning("[FullscreenCameraController] Camera is not playing after initialization");
        }
    }
    
    // Calculate optimal resolution based on device capabilities and screen
    private Vector2 GetOptimalResolution(Vector2 targetRes)
    {
        // Start with the requested resolution
        Vector2 optimalRes = targetRes;
        
        // On mobile, try to match screen aspect ratio while keeping reasonable quality
        if (Application.isMobilePlatform)
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float targetHeight = Mathf.Min(targetRes.y, Screen.height);
            float targetWidth = targetHeight * screenAspect;
            
            // Round to nearest even numbers (some cameras require this)
            targetWidth = Mathf.Round(targetWidth / 2) * 2;
            targetHeight = Mathf.Round(targetHeight / 2) * 2;
            
            optimalRes = new Vector2(targetWidth, targetHeight);
        }
        
        return optimalRes;
    }
    
    // Update camera display (orientation, aspect ratio, etc.)
    private void UpdateCameraDisplay()
    {
        if (cameraTexture == null || !cameraTexture.isPlaying || displayImage == null || aspectFitter == null)
            return;
        
        // 1. Update aspect ratio
        float ratio = (float)cameraTexture.width / cameraTexture.height;
        aspectFitter.aspectRatio = ratio;
        
        // Log dimensions periodically
        if (debugMode && Time.frameCount % 300 == 0)
        {
            Debug.Log($"[FullscreenCameraController] Camera: {cameraTexture.width}x{cameraTexture.height}, ratio: {ratio}");
        }
        
        // 2. Handle rotation
        int angle = -cameraTexture.videoRotationAngle;
        rectTransform.localEulerAngles = new Vector3(0, 0, angle);
        
        // 3. Handle mirroring for selfie cameras
        Vector3 scale = rectTransform.localScale;
        bool isFrontFacing = IsFrontFacingCamera(cameraTexture.deviceName);
        
        // Platform-specific handling
        #if UNITY_ANDROID
        // On Android, front camera needs horizontal flipping
        scale.x = isFrontFacing ? -1 : 1;
        #elif UNITY_IOS
        // On iOS, we often need to flip regardless of camera type
        scale.x = isFrontFacing ? -1 : 1;
        #endif
        
        rectTransform.localScale = scale;
    }
    
    // Helper to check if a camera is front-facing
    private bool IsFrontFacingCamera(string deviceName)
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (WebCamDevice device in devices)
        {
            if (device.name == deviceName)
            {
                return device.isFrontFacing;
            }
        }
        return false;
    }
    
    // Public method to toggle front/back camera
    public void ToggleCameraFacing()
    {
        useFrontCamera = !useFrontCamera;
        
        if (debugMode)
        {
            Debug.Log($"[FullscreenCameraController] Switching to {(useFrontCamera ? "front" : "back")} camera");
        }
        
        // Re-initialize camera with new settings
        InitializeCamera();
        
        // Trigger event to notify listeners
        if (OnCameraSwitch != null)
        {
            OnCameraSwitch.Invoke(useFrontCamera);
        }
    }
    
    // Public method to toggle between fill modes
    public void ToggleFillMode()
    {
        fillEntireScreen = !fillEntireScreen;
        ConfigureAspectRatioFitter();
    }
    
    // Provides access to the camera texture for other components like QR scanners
    public WebCamTexture GetCameraTexture()
    {
        return cameraTexture;
    }
    
    // Get camera status information
    public string GetCameraStatusInfo()
    {
        if (cameraTexture == null)
            return "No camera initialized";
            
        if (!cameraTexture.isPlaying)
            return "Camera not playing";
            
        return $"Camera: {cameraTexture.deviceName}\n" +
               $"Resolution: {cameraTexture.width}x{cameraTexture.height}\n" +
               $"Front-facing: {useFrontCamera}\n" +
               $"FPS: {cameraTexture.requestedFPS}";
    }
}