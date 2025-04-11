// Not Using now, using FullscreenCameraController instead for Camera Handling and control
/* 
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(AspectRatioFitter))]
public class CameraHandler : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    private RawImage display;
    private AspectRatioFitter aspectRatioFitter;
    
    [SerializeField] private bool debugMode = true;
    
    // Fullscreen settings
    [SerializeField] private bool forceFullscreen = true;
    [SerializeField] private AspectRatioFitter.AspectMode aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
    
    void Awake()
    {
        // Get required components
        display = GetComponent<RawImage>();
        aspectRatioFitter = GetComponent<AspectRatioFitter>();
        
        // Create AspectRatioFitter if needed
        if (aspectRatioFitter == null)
        {
            aspectRatioFitter = gameObject.AddComponent<AspectRatioFitter>();
        }
        
        // Configure AspectRatioFitter
        aspectRatioFitter.aspectMode = aspectMode;
        
        // Set fullscreen if requested
        if (forceFullscreen)
        {
            MakeFullscreen();
        }
        
        if (debugMode)
        {
            Debug.Log("CameraHandler initialized. Fullscreen: " + forceFullscreen);
        }
    }
    
    // Make this RawImage cover the entire screen
    private void MakeFullscreen()
    {
        if (display != null)
        {
            // Get the RectTransform
            RectTransform rt = display.rectTransform;
            
            // Make it stretch to fill parent
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero; // No additional size
            rt.anchoredPosition = Vector2.zero; // Centered
            
            // Make sure canvas is properly configured for fullscreen
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // For fullscreen camera, Screen Space - Overlay is typically best
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    Debug.LogWarning("For best fullscreen results, set Canvas render mode to Screen Space - Overlay");
                }
                
                // Get the canvas scaler
                UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    // Configure for proper scaling on all devices
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920); // Common mobile resolution
                    scaler.matchWidthOrHeight = 0.5f; // Match both width and height
                }
            }
            
            if (debugMode)
            {
                Debug.Log("Display configured for fullscreen");
            }
        }
    }

    // This method should be called after webcamTexture is initialized
    public void SetWebcamTexture(WebCamTexture webCamTexture)
    {
        this.webcamTexture = webCamTexture;
        
        if (display != null)
        {
            // Apply the texture to the RawImage
            display.texture = webCamTexture;
            
            // Ensure proper color
            display.color = Color.white;
            
            if (debugMode)
            {
                Debug.Log($"Webcam texture assigned. Size: {webCamTexture.width}x{webCamTexture.height}");
            }
        }
        else
        {
            Debug.LogError("RawImage component not found");
        }
    }
    
    void Update()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            // Only update if we have new frames
            if (webcamTexture.didUpdateThisFrame)
            {
                // Set the aspect ratio to match the camera
                float ratio = (float)webcamTexture.width / (float)webcamTexture.height;
                aspectRatioFitter.aspectRatio = ratio;
                
                // Handle rotation based on device orientation
                int angle = -webcamTexture.videoRotationAngle;
                display.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
                
                // Handle mirroring for selfie cameras
                Vector3 scale = display.rectTransform.localScale;
                bool isFrontFacing = false;
                
                #if UNITY_ANDROID || UNITY_IOS
                // Check if current camera is front-facing
                WebCamDevice[] devices = WebCamTexture.devices;
                foreach (WebCamDevice device in devices)
                {
                    if (device.name == webcamTexture.deviceName)
                    {
                        isFrontFacing = device.isFrontFacing;
                        break;
                    }
                }
                #endif
                
                // Apply appropriate scaling based on platform and camera type
                #if UNITY_ANDROID
                scale.x = isFrontFacing ? -1 : 1;
                #elif UNITY_IOS
                scale.x = isFrontFacing ? -1 : 1;
                #endif
                
                display.rectTransform.localScale = scale;
                
                // Log camera state occasionally for debugging
                if (debugMode && Time.frameCount % 300 == 0)
                {
                    Debug.Log($"Camera: {webcamTexture.width}x{webcamTexture.height}, " +
                              $"Rotation: {angle}, Front: {isFrontFacing}");
                }
            }
        }
    }
}
*/
