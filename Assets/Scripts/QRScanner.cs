using System.Collections;
using System;
using UnityEngine;
using TMPro;
using ZXing;
using UnityEngine.UI;
using System.Collections.Generic;

public class ImprovedQRScanner : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to show scan results")]
    public TMP_Text ShiftInfoHolder;
    
    [Tooltip("Optional scanning overlay/target")]
    public GameObject scannerOverlay;
    
    [Tooltip("Text to show successful scan message")]
    public TMP_Text successTextNotification;
    
    [Header("Camera Controls")]
    [Tooltip("Button to switch between front/back cameras")]
    public Button cameraToggleButton;
    
    [Header("Scanning Settings")]
    [Tooltip("Time to pause after successful scan (seconds)")]
    [Range(1, 5)]
    public float pauseAfterScanDuration = 3f;
    
    [Tooltip("Enable debug logging")]
    [SerializeField] private bool debugMode = true;
    
    // Private variables
    private WebCamTexture webcamTexture;
    private string QrCode = string.Empty;
    private Coroutine qrCoroutine;
    private FullscreenCameraController cameraController;
    private int scanCount = 0;
    
    void Start()
    {
        // Initialize text field
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
        else if (debugMode)
        {
            Debug.LogWarning("ShiftInfoHolder not assigned! QR results won't be displayed.");
        }
        
        // Hide success text initially
        if (successTextNotification != null)
        {
            successTextNotification.gameObject.SetActive(false);
        }
        
        // Set up camera toggle button
        if (cameraToggleButton != null)
        {
            cameraToggleButton.onClick.AddListener(ToggleCamera);
            
            // Add text component if needed
            if (cameraToggleButton.GetComponentInChildren<TMP_Text>() == null)
            {
                GameObject textObj = new GameObject("ButtonText");
                textObj.transform.SetParent(cameraToggleButton.transform, false);
                
                RectTransform rt = textObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                
                TMP_Text buttonText = textObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = "Switch Camera";
                buttonText.fontSize = 14;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = Color.white;
            }
        }
        
        // Get reference to the FullscreenCameraController
        cameraController = GetComponent<FullscreenCameraController>();
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<FullscreenCameraController>();
            if (cameraController == null)
            {
                Debug.LogError("FullscreenCameraController not found! QR scanning won't work.");
                return;
            }
        }
        
        if (debugMode)
        {
            Debug.Log("QR Scanner initialized, waiting for camera to start...");
        }
        
        // Start a coroutine to wait for the camera controller to initialize the camera
        StartCoroutine(WaitForCameraInitialization());
    }
    
    private IEnumerator WaitForCameraInitialization()
    {
        // Wait for camera to be initialized (maximum 5 seconds)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Try to get the camera texture
            webcamTexture = cameraController.GetCameraTexture();
            
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                if (debugMode)
                {
                    Debug.Log($"Camera detected: {webcamTexture.deviceName}, resolution: {webcamTexture.width}x{webcamTexture.height}");
                }
                
                // Update camera toggle button text
                UpdateCameraToggleButtonText();
                
                // Start scanning
                StartScanning();
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        // If we got here, camera wasn't initialized within timeout period
        Debug.LogError("Camera initialization timed out. QR scanning won't work.");
    }

    private void OnEnable()
    {
        if (webcamTexture != null && !webcamTexture.isPlaying)
        {
            StartScanning();
        }
    }

    private void OnDisable()
    {
        StopScanning();
    }

    private void StartScanning()
    {
        if (webcamTexture != null && webcamTexture.isPlaying && qrCoroutine == null)
        {
            qrCoroutine = StartCoroutine(GetQRCode());
            if (debugMode) Debug.Log("Started scanning for QR codes");
        }
    }

    private void StopScanning()
    {
        if (qrCoroutine != null)
        {
            StopCoroutine(qrCoroutine);
            qrCoroutine = null;
        }
    }

    IEnumerator GetQRCode()
    {
        // Wait for webcam to be fully initialized
        yield return new WaitForSeconds(0.5f);
        
        if (webcamTexture == null)
        {
            Debug.LogError("WebCamTexture is null. Cannot scan for QR codes.");
            yield break;
        }
        
        IBarcodeReader barCodeReader = new BarcodeReader();
        var snap = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.ARGB32,
            false
        );

        while (true)
        {
            bool decodeAttempted = false;
            try
            {
                if (webcamTexture.isPlaying && webcamTexture.didUpdateThisFrame)
                {
                    // Log camera resolution periodically
                    if (debugMode && Time.frameCount % 300 == 0)
                    {
                        Debug.Log($"Camera resolution: {webcamTexture.width}x{webcamTexture.height}, Scan count: {scanCount}");
                    }
                    
                    scanCount++;
                    
                    // Ensure texture dimensions match webcam dimensions
                    if (snap.width != webcamTexture.width || snap.height != webcamTexture.height)
                    {
                        snap.Reinitialize(webcamTexture.width, webcamTexture.height);
                        if (debugMode) Debug.Log($"Resized snap texture to {webcamTexture.width}x{webcamTexture.height}");
                    }
                    
                    snap.SetPixels32(webcamTexture.GetPixels32());
                    snap.Apply();
                    
                    var Result = barCodeReader.Decode(
                        snap.GetRawTextureData(),
                        webcamTexture.width,
                        webcamTexture.height,
                        RGBLuminanceSource.BitmapFormat.ARGB32
                    );

                    if (Result != null)
                    {
                        QrCode = Result.Text;
                        if (!string.IsNullOrEmpty(QrCode))
                        {
                            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            if (ShiftInfoHolder != null)
                            {
                                ShiftInfoHolder.text += "\n" + currentDateTime + "\n" + QrCode + "\n";
                            }
                            decodeAttempted = true;
                            if (debugMode) Debug.Log($"QR Code detected: {QrCode}");
                            
                            // Display success notification
                            ShowSuccessNotification($"QR Code Scanned! ({QrCode.Length} chars)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"QR Scanner error: {ex.Message}");
            }
            
            if (decodeAttempted)
            {
                // Pause briefly after successfully scanning a code
                webcamTexture.Stop();
                yield return new WaitForSeconds(pauseAfterScanDuration);
                QrCode = string.Empty;
                webcamTexture.Play();
            }
            else
            {
                yield return null;
            }
        }
    }
    
    // Show text notification for successful scan
    private void ShowSuccessNotification(string message)
    {
        // If we have a success text notification component, use it
        if (successTextNotification != null)
        {
            successTextNotification.text = message;
            successTextNotification.gameObject.SetActive(true);
            
            // Hide after delay
            StopAllCoroutines();
            StartCoroutine(HideNotificationAfterDelay(2.0f));
        }
        else if (scannerOverlay != null)
        {
            // Try to create a text notification if it doesn't exist
            GameObject textObject = new GameObject("SuccessText");
            textObject.transform.SetParent(scannerOverlay.transform.parent, false);
            
            RectTransform rt = textObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 50);
            
            // Position below the scanner overlay
            RectTransform overlayRT = scannerOverlay.GetComponent<RectTransform>();
            if (overlayRT != null)
            {
                rt.anchoredPosition = new Vector2(0, overlayRT.anchoredPosition.y - overlayRT.rect.height/2 - 50);
            }
            else
            {
                rt.anchoredPosition = new Vector2(0, -150);
            }
            
            successTextNotification = textObject.AddComponent<TextMeshProUGUI>();
            successTextNotification.fontSize = 24;
            successTextNotification.alignment = TextAlignmentOptions.Center;
            successTextNotification.color = Color.green;
            successTextNotification.text = message;
            
            // Hide after delay
            StartCoroutine(HideNotificationAfterDelay(2.0f));
        }
    }
    
    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (successTextNotification != null)
        {
            successTextNotification.gameObject.SetActive(false);
        }
    }
    
    // Toggle between front and back cameras
    public void ToggleCamera()
    {
        if (cameraController != null)
        {
            // Stop scanning during camera switch
            StopScanning();
            
            // Toggle camera
            cameraController.ToggleCameraFacing();
            
            // Update camera toggle button text
            UpdateCameraToggleButtonText();
            
            // Restart scanning after a delay
            StartCoroutine(RestartScanning(1.0f));
        }
    }
    
    // Update camera toggle button text based on current camera
    private void UpdateCameraToggleButtonText()
    {
        if (cameraToggleButton != null)
        {
            TMP_Text buttonText = cameraToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                bool isFront = cameraController.IsUsingFrontCamera();
                buttonText.text = isFront ? "Switch to Back Camera" : "Switch to Front Camera";
            }
        }
    }
    
    private IEnumerator RestartScanning(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartScanning();
    }

    // Display the QR code on screen
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(QrCode))
            return;
            
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

        string text = QrCode;
        string[] lines = SplitTextIntoLines(text, 20);

        float textHeight = h * 2 / 100;
        float totalHeight = textHeight * lines.Length + 35.0f * (lines.Length - 1);
        float startY = (h - totalHeight) / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            Rect rect = new Rect(0, startY + i * (textHeight + 35.0f), w, textHeight);
            GUI.Label(rect, lines[i], style);
        }
    }

    private string[] SplitTextIntoLines(string text, int maxLineLength)
    {
        List<string> lines = new List<string>();

        for (int i = 0; i < text.Length; i += maxLineLength)
        {
            if (i + maxLineLength < text.Length)
                lines.Add(text.Substring(i, maxLineLength));
            else
                lines.Add(text.Substring(i));
        }

        return lines.ToArray();
    }

    public void ClearShiftLog()
    {
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
        
        scanCount = 0;
    }
}


/*
using System.Collections;
using System;
using UnityEngine;
using TMPro;
using ZXing;
using UnityEngine.UI;
using System.Collections.Generic;


public class IntegratedQRScanner : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to show scan results")]
    public TMP_Text ShiftInfoHolder;
    
    [Tooltip("Optional scanning overlay/target")]
    public GameObject scannerOverlay;
    
    [Header("Scanning Settings")]
    [Tooltip("Time to pause after successful scan (seconds)")]
    [Range(1, 5)]
    public float pauseAfterScanDuration = 3f;
    
    [Tooltip("Enable debug logging")]
    [SerializeField] private bool debugMode = true;
    
    // Private variables
    private WebCamTexture webcamTexture;
    private string QrCode = string.Empty;
    private Coroutine qrCoroutine;
    private FullscreenCameraController cameraController;
    
    void Start()
    {
        // Initialize text field
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
        else if (debugMode)
        {
            Debug.LogWarning("ShiftInfoHolder not assigned! QR results won't be displayed.");
        }
        
        // Get reference to the FullscreenCameraController (either on same object or find it)
        cameraController = GetComponent<FullscreenCameraController>();
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<FullscreenCameraController>();
            if (cameraController == null)
            {
                Debug.LogError("FullscreenCameraController not found! QR scanning won't work.");
                return;
            }
        }
        
        if (debugMode)
        {
            Debug.Log("QR Scanner initialized, waiting for camera to start...");
        }
        
        // Start a coroutine to wait for the camera controller to initialize the camera
        StartCoroutine(WaitForCameraInitialization());
    }
    
    private IEnumerator WaitForCameraInitialization()
    {
        // Wait for camera to be initialized (maximum 5 seconds)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Try to get the camera texture
            webcamTexture = cameraController.GetCameraTexture();
            
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                if (debugMode)
                {
                    Debug.Log($"Camera detected: {webcamTexture.deviceName}, resolution: {webcamTexture.width}x{webcamTexture.height}");
                }
                
                // Start scanning
                StartScanning();
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        // If we got here, camera wasn't initialized within timeout period
        Debug.LogError("Camera initialization timed out. QR scanning won't work.");
    }

    private void OnEnable()
    {
        if (webcamTexture != null && !webcamTexture.isPlaying)
        {
            StartScanning();
        }
    }

    private void OnDisable()
    {
        StopScanning();
    }

    private void StartScanning()
    {
        if (webcamTexture != null && webcamTexture.isPlaying && qrCoroutine == null)
        {
            qrCoroutine = StartCoroutine(GetQRCode());
            if (debugMode) Debug.Log("Started scanning for QR codes");
        }
    }

    private void StopScanning()
    {
        if (qrCoroutine != null)
        {
            StopCoroutine(qrCoroutine);
            qrCoroutine = null;
        }
    }

    IEnumerator GetQRCode()
    {
        // Wait for webcam to be fully initialized
        yield return new WaitForSeconds(0.5f);
        
        if (webcamTexture == null)
        {
            Debug.LogError("WebCamTexture is null. Cannot scan for QR codes.");
            yield break;
        }
        
        IBarcodeReader barCodeReader = new BarcodeReader();
        var snap = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.ARGB32,
            false
        );

        while (true)
        {
            bool decodeAttempted = false;
            try
            {
                if (webcamTexture.isPlaying && webcamTexture.didUpdateThisFrame)
                {
                    // Log camera resolution periodically
                    if (debugMode && Time.frameCount % 300 == 0)
                    {
                        Debug.Log($"Camera resolution: {webcamTexture.width}x{webcamTexture.height}");
                    }
                    
                    // Ensure texture dimensions match webcam dimensions
                    if (snap.width != webcamTexture.width || snap.height != webcamTexture.height)
                    {
                        snap.Reinitialize(webcamTexture.width, webcamTexture.height);
                        if (debugMode) Debug.Log($"Resized snap texture to {webcamTexture.width}x{webcamTexture.height}");
                    }
                    
                    snap.SetPixels32(webcamTexture.GetPixels32());
                    snap.Apply();
                    
                    var Result = barCodeReader.Decode(
                        snap.GetRawTextureData(),
                        webcamTexture.width,
                        webcamTexture.height,
                        RGBLuminanceSource.BitmapFormat.ARGB32
                    );

                    if (Result != null)
                    {
                        QrCode = Result.Text;
                        if (!string.IsNullOrEmpty(QrCode))
                        {
                            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            if (ShiftInfoHolder != null)
                            {
                                ShiftInfoHolder.text += "\n" + currentDateTime + "\n" + QrCode + "\n";
                            }
                            decodeAttempted = true;
                            if (debugMode) Debug.Log($"QR Code detected: {QrCode}");
                            
                            // Trigger visual/audio feedback
                            OnQRCodeDetected();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"QR Scanner error: {ex.Message}");
            }
            
            if (decodeAttempted)
            {
                // Pause briefly after successfully scanning a code
                webcamTexture.Stop();
                yield return new WaitForSeconds(pauseAfterScanDuration);
                QrCode = string.Empty;
                webcamTexture.Play();
            }
            else
            {
                yield return null;
            }
        }
    }
    
    // Called when a QR code is successfully detected
    private void OnQRCodeDetected()
    {
        // Enable scanner overlay feedback if available
        if (scannerOverlay != null)
        {
            StartCoroutine(ShowScanSuccessAnimation());
        }
        
        // You can add sound effects or other feedback here
    }
    
    // Simple animation to show scan success
    private IEnumerator ShowScanSuccessAnimation()
    {
        if (scannerOverlay != null)
        {
            // Flash the overlay or change its color
            Image overlayImage = scannerOverlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                Color originalColor = overlayImage.color;
                overlayImage.color = Color.green;
                yield return new WaitForSeconds(0.3f);
                overlayImage.color = originalColor;
            }
            else
            {
                // Just toggle the overlay if it doesn't have an image component
                scannerOverlay.SetActive(true);
                yield return new WaitForSeconds(0.5f);
                scannerOverlay.SetActive(false);
            }
        }
    }

    // Display the QR code on screen
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(QrCode))
            return;
            
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

        string text = QrCode;
        string[] lines = SplitTextIntoLines(text, 20);

        float textHeight = h * 2 / 100;
        float totalHeight = textHeight * lines.Length + 35.0f * (lines.Length - 1);
        float startY = (h - totalHeight) / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            Rect rect = new Rect(0, startY + i * (textHeight + 35.0f), w, textHeight);
            GUI.Label(rect, lines[i], style);
        }
    }

    private string[] SplitTextIntoLines(string text, int maxLineLength)
    {
        List<string> lines = new List<string>();

        for (int i = 0; i < text.Length; i += maxLineLength)
        {
            if (i + maxLineLength < text.Length)
                lines.Add(text.Substring(i, maxLineLength));
            else
                lines.Add(text.Substring(i));
        }

        return lines.ToArray();
    }

    public void ClearShiftLog()
    {
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
    }
}


*/




/*
using System.Collections;
using System;
using UnityEngine;
using TMPro;
using ZXing;
using UnityEngine.UI;
using System.Collections.Generic;


public class DebugEnabledQRScanner : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to show scan results")]
    public TMP_Text ShiftInfoHolder;
    
    [Tooltip("Optional scanning overlay/target")]
    public GameObject scannerOverlay;
    
    [Tooltip("Optional debug info panel")]
    public GameObject debugPanel;
    
    [Tooltip("Text component to show debug info")]
    public TMP_Text debugInfoText;
    
    [Header("Scanning Settings")]
    [Tooltip("Time to pause after successful scan (seconds)")]
    [Range(1, 5)]
    public float pauseAfterScanDuration = 3f;
    
    [Tooltip("Enable debug logging")]
    [SerializeField] private bool debugMode = true;
    
    [Tooltip("Show on-screen debug info")]
    [SerializeField] private bool showOnScreenDebug = true;
    
    // Private variables
    private WebCamTexture webcamTexture;
    private string QrCode = string.Empty;
    private Coroutine qrCoroutine;
    private FullscreenCameraController cameraController;
    private int scanAttempts = 0;
    private int framesCaptured = 0;
    
    // Debug info
    private string cameraStatus = "Initializing...";
    private string scanStatus = "Not scanning";
    private Vector2 cameraResolution = Vector2.zero;
    private string lastError = "";
    
    void Start()
    {
        // Create debug panel if it doesn't exist
        CreateDebugUIIfNeeded();
        
        // Initialize text field
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
        else
        {
            UpdateDebugInfo("ShiftInfoHolder not assigned! QR results won't be displayed.");
        }
        
        // Get reference to the FullscreenCameraController
        cameraController = GetComponent<FullscreenCameraController>();
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<FullscreenCameraController>();
            if (cameraController == null)
            {
                UpdateDebugInfo("FullscreenCameraController not found! QR scanning won't work.");
                return;
            }
        }
        
        UpdateDebugInfo("QR Scanner initialized, waiting for camera to start...");
        
        // Start a coroutine to wait for the camera controller to initialize the camera
        StartCoroutine(WaitForCameraInitialization());
    }
    
    private void CreateDebugUIIfNeeded()
    {
        if (showOnScreenDebug && debugPanel == null)
        {
            // Find Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas found in scene! Cannot create debug UI.");
                return;
            }
            
            // Create debug panel
            debugPanel = new GameObject("DebugInfoPanel");
            debugPanel.transform.SetParent(canvas.transform, false);
            
            // Add panel components
            RectTransform panelRect = debugPanel.AddComponent<RectTransform>();
            Image panelImage = debugPanel.AddComponent<Image>();
            
            // Set panel properties
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.25f);
            panelRect.offsetMin = new Vector2(10, 10);
            panelRect.offsetMax = new Vector2(-10, -10);
            panelImage.color = new Color(0, 0, 0, 0.7f);
            
            // Create text object
            GameObject textObj = new GameObject("DebugInfoText");
            textObj.transform.SetParent(debugPanel.transform, false);
            
            // Add text components
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            debugInfoText = textObj.AddComponent<TextMeshProUGUI>();
            
            // Set text properties
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            debugInfoText.fontSize = 14;
            debugInfoText.color = Color.white;
            debugInfoText.alignment = TextAlignmentOptions.TopLeft;
            debugInfoText.text = "QR Scanner Debug Info\n------------------";
            
            // Make sure panel is in front
            debugPanel.transform.SetAsLastSibling();
        }
    }
    
    private void UpdateDebugInfo(string message = null)
    {
        if (message != null)
        {
            Debug.Log(message);
        }
        
        if (showOnScreenDebug && debugInfoText != null)
        {
            string debugText = "QR Scanner Debug Info\n------------------\n";
            debugText += $"Camera Status: {cameraStatus}\n";
            debugText += $"Scan Status: {scanStatus}\n";
            debugText += $"Resolution: {cameraResolution.x}x{cameraResolution.y}\n";
            debugText += $"Scan Attempts: {scanAttempts}\n";
            debugText += $"Frames Captured: {framesCaptured}\n";
            
            if (!string.IsNullOrEmpty(lastError))
            {
                debugText += $"Last Error: {lastError}\n";
            }
            
            if (!string.IsNullOrEmpty(QrCode))
            {
                debugText += $"Last QR Code: {QrCode}\n";
            }
            
            if (message != null)
            {
                debugText += $"Message: {message}\n";
            }
            
            debugInfoText.text = debugText;
        }
    }
    
    private IEnumerator WaitForCameraInitialization()
    {
        // Wait for camera to be initialized (maximum 5 seconds)
        float timeout = 5f;
        float elapsed = 0f;
        
        cameraStatus = "Waiting for camera to initialize...";
        UpdateDebugInfo();
        
        while (elapsed < timeout)
        {
            // Try to get the camera texture
            webcamTexture = cameraController.GetCameraTexture();
            
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                cameraStatus = $"Active: {webcamTexture.deviceName}";
                cameraResolution = new Vector2(webcamTexture.width, webcamTexture.height);
                UpdateDebugInfo($"Camera detected: {webcamTexture.deviceName}, resolution: {webcamTexture.width}x{webcamTexture.height}");
                
                // Start scanning
                StartScanning();
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
            
            // Update progress in debug
            cameraStatus = $"Initializing... ({Mathf.Round(elapsed / timeout * 100)}%)";
            UpdateDebugInfo();
        }
        
        // If we got here, camera wasn't initialized within timeout period
        cameraStatus = "Failed to initialize!";
        lastError = "Camera initialization timed out";
        UpdateDebugInfo("Camera initialization timed out. QR scanning won't work.");
    }

    private void OnEnable()
    {
        if (webcamTexture != null && !webcamTexture.isPlaying)
        {
            StartScanning();
        }
    }

    private void OnDisable()
    {
        StopScanning();
    }

    private void StartScanning()
    {
        if (webcamTexture != null && webcamTexture.isPlaying && qrCoroutine == null)
        {
            qrCoroutine = StartCoroutine(GetQRCode());
            scanStatus = "Active";
            UpdateDebugInfo("Started scanning for QR codes");
        }
    }

    private void StopScanning()
    {
        if (qrCoroutine != null)
        {
            StopCoroutine(qrCoroutine);
            qrCoroutine = null;
            scanStatus = "Stopped";
            UpdateDebugInfo("Stopped scanning for QR codes");
        }
    }

    IEnumerator GetQRCode()
    {
        // Wait for webcam to be fully initialized
        yield return new WaitForSeconds(0.5f);
        
        if (webcamTexture == null)
        {
            scanStatus = "Error: No camera";
            lastError = "WebCamTexture is null";
            UpdateDebugInfo("WebCamTexture is null. Cannot scan for QR codes.");
            yield break;
        }
        
        // Create barcode reader
        IBarcodeReader barCodeReader = new BarcodeReader();
        
        // Create snapshot texture
        var snap = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.ARGB32,
            false
        );

        // Initialize debug
        scanAttempts = 0;
        framesCaptured = 0;
        scanStatus = "Scanning";
        UpdateDebugInfo("QR scanner ready");
        
        // Show scanning overlay if available
        if (scannerOverlay != null)
        {
            scannerOverlay.SetActive(true);
        }

        if (scannerOverlay != null)
        {
            RawImage rawImage = scannerOverlay.GetComponent<RawImage>();

            if (rawImage != null) {

                if (rawImage.texture != null)
                {
                    RectTransform rt = scannerOverlay.GetComponent<RectTransform>();
                    float aspect = (float)rawImage.texture.width / rawImage.texture.height;
                    rt.sizeDelta = new Vector2(200, 150 / aspect); // Example: fixed width, adjust height
                }
            }
        }



        while (true)
        {
            bool decodeAttempted = false;
            try
            {
                if (webcamTexture.isPlaying && webcamTexture.didUpdateThisFrame)
                {
                    framesCaptured++;
                    
                    // Update every ~5 seconds
                    if (framesCaptured % 150 == 0)
                    {
                        cameraResolution = new Vector2(webcamTexture.width, webcamTexture.height);
                        UpdateDebugInfo($"Camera active: {cameraResolution.x}x{cameraResolution.y}");
                    }
                    
                    // Ensure texture dimensions match webcam dimensions
                    if (snap.width != webcamTexture.width || snap.height != webcamTexture.height)
                    {
                        snap.Reinitialize(webcamTexture.width, webcamTexture.height);
                        UpdateDebugInfo($"Resized snapshot to {webcamTexture.width}x{webcamTexture.height}");
                    }
                    
                    // Take a snapshot of the camera view
                    snap.SetPixels32(webcamTexture.GetPixels32());
                    snap.Apply();
                    
                    // Try to decode QR/barcode
                    scanAttempts++;
                    if (scanAttempts % 30 == 0) // Update progress every ~1 second
                    {
                        scanStatus = $"Scanning ({scanAttempts} attempts)";
                        UpdateDebugInfo();
                    }
                    
                    var Result = barCodeReader.Decode(
                        snap.GetRawTextureData(),
                        webcamTexture.width,
                        webcamTexture.height,
                        RGBLuminanceSource.BitmapFormat.ARGB32
                    );

                    if (Result != null)
                    {
                        QrCode = Result.Text;
                        if (!string.IsNullOrEmpty(QrCode))
                        {
                            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            if (ShiftInfoHolder != null)
                            {
                                ShiftInfoHolder.text += "\n" + currentDateTime + "\n" + QrCode + "\n";
                            }
                            decodeAttempted = true;
                            scanStatus = "QR CODE FOUND!";
                            UpdateDebugInfo($"QR Code detected: {QrCode}");
                            
                            // Trigger visual/audio feedback
                            OnQRCodeDetected();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                scanStatus = "Error scanning";
                UpdateDebugInfo($"QR Scanner error: {ex.Message}");
            }
            
            if (decodeAttempted)
            {
                // Pause briefly after successfully scanning a code
                webcamTexture.Stop();
                cameraStatus = "Paused (QR found)";
                UpdateDebugInfo("Pausing after successful scan");
                yield return new WaitForSeconds(pauseAfterScanDuration);
                QrCode = string.Empty;
                webcamTexture.Play();
                cameraStatus = "Active";
                scanStatus = "Scanning";
                scanAttempts = 0;
                UpdateDebugInfo("Resuming scanning");
            }
            else
            {
                yield return null;
            }
        }
    }
    
    // Called when a QR code is successfully detected
    private void OnQRCodeDetected()
    {
        // Enable scanner overlay feedback if available
        if (scannerOverlay != null)
        {
            StartCoroutine(ShowScanSuccessAnimation());
        }
        
        // You can add sound effects or other feedback here
    }
    
    // Simple animation to show scan success
    private IEnumerator ShowScanSuccessAnimation()
    {
        if (scannerOverlay != null)
        {
            // Try to get RawImage component instead of Image
            RawImage overlayImage = scannerOverlay.GetComponent<RawImage>();
            if (overlayImage != null)
            {
                Color originalColor = overlayImage.color;
                overlayImage.color = Color.green; // Flash green on success
                yield return new WaitForSeconds(0.3f);
                overlayImage.color = originalColor; // Back to original
            }
            else
            {
                // Fallback if no RawImage is found (e.g., toggle active state)
                scannerOverlay.SetActive(true);
                yield return new WaitForSeconds(0.5f);
                scannerOverlay.SetActive(false);
            }
        }
    }




    // Display the QR code on screen
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(QrCode))
            return;
            
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

        string text = QrCode;
        string[] lines = SplitTextIntoLines(text, 20);

        float textHeight = h * 2 / 100;
        float totalHeight = textHeight * lines.Length + 35.0f * (lines.Length - 1);
        float startY = (h - totalHeight) / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            Rect rect = new Rect(0, startY + i * (textHeight + 35.0f), w, textHeight);
            GUI.Label(rect, lines[i], style);
        }
    }

    private string[] SplitTextIntoLines(string text, int maxLineLength)
    {
        List<string> lines = new List<string>();

        for (int i = 0; i < text.Length; i += maxLineLength)
        {
            if (i + maxLineLength < text.Length)
                lines.Add(text.Substring(i, maxLineLength));
            else
                lines.Add(text.Substring(i));
        }

        return lines.ToArray();
    }

    public void ClearShiftLog()
    {
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
    }
}



*/









/* 
using System.Collections;
using System;
using UnityEngine;
using TMPro;
using ZXing;
using UnityEngine.UI;
using System.Collections.Generic;

public class QRScanner : MonoBehaviour
{
    public TMP_Text ShiftInfoHolder;
    private WebCamTexture webcamTexture;
    private string QrCode = string.Empty;
    private Coroutine qrCoroutine;
    
    // Reference to the CameraHandler
    public CameraHandler cameraHandler;
    
    // Camera settings
    public Vector2 cameraResolution = new Vector2(1280, 720);
    public bool useFrontCamera = false;
    public bool highestResolution = true;
    
    // QR scanning UI elements
    public GameObject scannerOverlay; // Optional overlay UI
    
    // Debug mode for troubleshooting
    [SerializeField] private bool debugMode = true;
    
    void Start()
    {
        // Initialize text field
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
        
        // Find CameraHandler if not assigned
        if (cameraHandler == null)
        {
            // Try to find WebcamFeed GameObject
            GameObject webcamFeedObj = GameObject.Find("WebcamFeed");
            if (webcamFeedObj != null)
            {
                cameraHandler = webcamFeedObj.GetComponent<CameraHandler>();
                
                // Add CameraHandler if needed
                if (cameraHandler == null)
                {
                    cameraHandler = webcamFeedObj.AddComponent<CameraHandler>();
                    if (debugMode) Debug.Log("Added CameraHandler to WebcamFeed");
                }
            }
            else
            {
                Debug.LogError("Could not find WebcamFeed GameObject");
            }
        }

        // Initialize camera
        InitializeCamera();
    }
    
    private void InitializeCamera()
    {
        // Get available camera devices
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("No camera devices found!");
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"Found {devices.Length} camera devices:");
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"  Device {i}: {devices[i].name}, Front-facing: {devices[i].isFrontFacing}");
            }
        }
        
        // Select the appropriate camera
        int selectedCamera = 0;
        bool foundPreferredCamera = false;
        
        for (int i = 0; i < devices.Length; i++)
        {
            #if UNITY_ANDROID || UNITY_IOS
            if (devices[i].isFrontFacing == useFrontCamera)
            {
                selectedCamera = i;
                foundPreferredCamera = true;
                break;
            }
            #else
            // On desktop, just use the first camera
            selectedCamera = 0;
            foundPreferredCamera = true;
            break;
            #endif
        }
        
        // If preferred camera not found, use first available
        if (!foundPreferredCamera && devices.Length > 0)
        {
            selectedCamera = 0;
            if (debugMode) Debug.Log($"Preferred camera not found, using: {devices[selectedCamera].name}");
        }
        
        // Determine optimal resolution
        Vector2 resolution = cameraResolution;
        
        // For Android/iOS, try to find optimal resolution if requested
        if (highestResolution)
        {
            #if UNITY_ANDROID || UNITY_IOS
            resolution = GetOptimalResolution(devices[selectedCamera]);
            #endif
        }
        
        Debug.Log($"Using camera: {devices[selectedCamera].name} at {resolution.x}x{resolution.y}");
        
        // Stop any existing webcam
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
        
        // Initialize webcam with selected resolution
        webcamTexture = new WebCamTexture(
            devices[selectedCamera].name,
            (int)resolution.x,
            (int)resolution.y,
            30  // FPS
        );
        
        // Connect to CameraHandler
        if (cameraHandler != null)
        {
            cameraHandler.SetWebcamTexture(webcamTexture);
            if (debugMode) Debug.Log("Webcam texture assigned to CameraHandler");
        }
        else
        {
            Debug.LogWarning("CameraHandler not assigned, camera display may be incorrect");
            
            // Try to assign to RawImage on this object as fallback
            RawImage rawImage = GetComponent<RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = webcamTexture;
                if (debugMode) Debug.Log("Webcam texture assigned to local RawImage");
            }
        }
        
        // Start scanning
        StartScanning();
    }
    
    // Helper to find optimal camera resolution based on device capabilities
    private Vector2 GetOptimalResolution(WebCamDevice device)
    {
        // Default resolution if we can't determine optimal
        Vector2 resolution = new Vector2(1280, 720);
        
        #if UNITY_ANDROID || UNITY_IOS
        // Try to get device screen resolution and match aspect ratio
        float screenAspect = (float)Screen.width / (float)Screen.height;
        int targetHeight = Mathf.Min(1080, Screen.height); // Cap at 1080p
        int targetWidth = Mathf.RoundToInt(targetHeight * screenAspect);
        
        resolution = new Vector2(targetWidth, targetHeight);
        
        if (debugMode)
        {
            Debug.Log($"Optimal resolution: {resolution.x}x{resolution.y} (screen: {Screen.width}x{Screen.height})");
        }
        #endif
        
        return resolution;
    }

    private void OnEnable()
    {
        StartScanning();
    }

    private void OnDisable()
    {
        StopScanning();
    }

    private void StartScanning()
    {
        if (webcamTexture != null && !webcamTexture.isPlaying)
        {
            webcamTexture.Play();
            if (qrCoroutine == null)
            {
                qrCoroutine = StartCoroutine(GetQRCode());
                if (debugMode) Debug.Log("Started scanning for QR codes");
            }
        }
    }

    private void StopScanning()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
        if (qrCoroutine != null)
        {
            StopCoroutine(qrCoroutine);
            qrCoroutine = null;
        }
    }

    IEnumerator GetQRCode()
    {
        // Wait for webcam to start
        yield return new WaitForSeconds(0.5f);
        
        IBarcodeReader barCodeReader = new BarcodeReader();
        var snap = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.ARGB32,
            false
        );

        while (true)
        {
            bool decodeAttempted = false;
            try
            {
                if (webcamTexture.isPlaying && webcamTexture.didUpdateThisFrame)
                {
                    // Ensure texture dimensions match webcam dimensions
                    if (snap.width != webcamTexture.width || snap.height != webcamTexture.height)
                    {
                        snap.Reinitialize(webcamTexture.width, webcamTexture.height);
                        if (debugMode) Debug.Log($"Resized snap texture to {webcamTexture.width}x{webcamTexture.height}");
                    }
                    
                    snap.SetPixels32(webcamTexture.GetPixels32());
                    snap.Apply();
                    
                    var Result = barCodeReader.Decode(
                        snap.GetRawTextureData(),
                        webcamTexture.width,
                        webcamTexture.height,
                        RGBLuminanceSource.BitmapFormat.ARGB32
                    );

                    if (Result != null)
                    {
                        QrCode = Result.Text;
                        if (!string.IsNullOrEmpty(QrCode))
                        {
                            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            if (ShiftInfoHolder != null)
                            {
                                ShiftInfoHolder.text += "\n" + currentDateTime + "\n" + QrCode + "\n";
                            }
                            decodeAttempted = true;
                            if (debugMode) Debug.Log($"QR Code detected: {QrCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"QR Scanner error: {ex.Message}");
            }
            
            if (decodeAttempted)
            {
                // Show a success indication
                OnQRCodeDetected();
                
                // Pause briefly after successfully scanning a code
                webcamTexture.Stop();
                yield return new WaitForSeconds(3);
                QrCode = string.Empty;
                webcamTexture.Play();
            }
            else
            {
                yield return null;
            }
        }
    }
    
    // Called when a QR code is successfully detected
    private void OnQRCodeDetected()
    {
        // Play a sound or show a visual indicator
        // You can add your own feedback here
    }

    // Display the QR code on screen
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(QrCode))
            return;
            
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

        string text = QrCode;
        string[] lines = SplitTextIntoLines(text, 20);

        float textHeight = h * 2 / 100;
        float totalHeight = textHeight * lines.Length + 35.0f * (lines.Length - 1);
        float startY = (h - totalHeight) / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            Rect rect = new Rect(0, startY + i * (textHeight + 35.0f), w, textHeight);
            GUI.Label(rect, lines[i], style);
        }
    }

    private string[] SplitTextIntoLines(string text, int maxLineLength)
    {
        List<string> lines = new List<string>();

        for (int i = 0; i < text.Length; i += maxLineLength)
        {
            if (i + maxLineLength < text.Length)
                lines.Add(text.Substring(i, maxLineLength));
            else
                lines.Add(text.Substring(i));
        }

        return lines.ToArray();
    }

    public void ClearShiftLog()
    {
        if (ShiftInfoHolder != null)
        {
            ShiftInfoHolder.text = "Shift logs: \n";
        }
    }
}

*/
