using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates a QR scanner overlay with frame guides for the user
/// </summary>
[RequireComponent(typeof(RawImage))]
public class QRScannerOverlayGenerator : MonoBehaviour
{
    [Header("Frame Settings")]
    [Tooltip("Size of the scanning frame (percentage of screen height)")]
    [Range(0.2f, 0.8f)]
    public float frameSize = 0.6f;
    
    [Tooltip("Width of the frame lines")]
    [Range(1, 10)]
    public int lineWidth = 3;
    
    [Tooltip("Color of the frame")]
    public Color frameColor = new Color(0f, 0.8f, 0.8f, 0.8f); // Cyan with alpha
    
    [Tooltip("Corner highlight length (percentage of frame side)")]
    [Range(0.1f, 0.3f)]
    public float cornerSize = 0.2f;
    
    [Header("Text Settings")]
    [Tooltip("Show guidance text")]
    public bool showGuidanceText = true;
    
    [Tooltip("Guidance text")]
    public string guidanceText = "Position QR code in frame";
    
    void Start()
    {
        GenerateOverlay();
    }
    
    public void GenerateOverlay()
    {
        // Get the RawImage component
        RawImage rawImage = GetComponent<RawImage>();
        
        // Calculate size based on screen
        int size = Mathf.RoundToInt(Screen.height * frameSize);
        
        // Make it square
        size = size - (size % 4); // Ensure divisible by 4 for cleaner corners
        
        // Create texture
        Texture2D overlayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        // Clear with transparent background
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        overlayTexture.SetPixels(pixels);
        
        // Draw the frame - outline only
        DrawFrame(overlayTexture, size, lineWidth, frameColor, cornerSize);
        
        // Apply changes
        overlayTexture.Apply();
        
        // Set the texture
        rawImage.texture = overlayTexture;
        
        // Set RawImage color to white to not affect texture colors
        rawImage.color = Color.white;
        
        // Add guidance text if needed
        if (showGuidanceText)
        {
            AddGuidanceText();
        }
        
        // Set proper size
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(size, size);
        }
    }
    
    private void DrawFrame(Texture2D texture, int size, int lineWidth, Color color, float cornerRatio)
    {
        int cornerLength = Mathf.RoundToInt(size * cornerRatio);
        
        // Draw horizontal lines (only at corners)
        for (int x = 0; x < size; x++)
        {
            // Top side corners
            if (x < cornerLength || x > size - cornerLength)
            {
                for (int y = 0; y < lineWidth; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            
            // Bottom side corners
            if (x < cornerLength || x > size - cornerLength)
            {
                for (int y = size - lineWidth; y < size; y++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
        
        // Draw vertical lines (only at corners)
        for (int y = 0; y < size; y++)
        {
            // Left side corners
            if (y < cornerLength || y > size - cornerLength)
            {
                for (int x = 0; x < lineWidth; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            
            // Right side corners
            if (y < cornerLength || y > size - cornerLength)
            {
                for (int x = size - lineWidth; x < size; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }
    
    private void AddGuidanceText()
    {
        // Find the parent canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;
            
        // Create text object
        GameObject textObj = new GameObject("GuidanceText");
        textObj.transform.SetParent(transform, false);
        
        // Add RectTransform
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(300, 50);
        rt.anchoredPosition = new Vector2(0, -20);
        
        // Add text component (works with both regular Text and TMP)
        if (System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null)
        {
            // Try to use TextMeshPro if available
            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = guidanceText;
            tmp.color = frameColor;
            tmp.fontSize = 18;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
        }
        else
        {
            // Fall back to Unity UI Text
            Text text = textObj.AddComponent<Text>();
            text.text = guidanceText;
            text.color = frameColor;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
        }
    }
}