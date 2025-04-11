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

