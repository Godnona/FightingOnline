using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;

    private float timer;
    private int frameCount;
    private float fps;
    private bool isVisible = true;

    private float hue; // For color cycling

    void Update()
    {
        // FPS Calculation
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= 1f)
        {
            fps = frameCount / timer;
            fpsText.text = "FPS: " + Mathf.RoundToInt(fps).ToString();
            frameCount = 0;
            timer = 0f;
        }

        // Toggle with Shift + Plus key
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) 
            && Input.GetKeyDown(KeyCode.Equals))
        {
            isVisible = !isVisible;
            fpsText.gameObject.SetActive(isVisible);
        }

        // 🌈 Smooth Color Wheel Cycling
        hue += Time.unscaledDeltaTime * 0.5f; // Speed of color change
        if (hue > 1f) hue -= 1f;

        // Full Saturation & Brightness (EXTREME CORNERS)
        Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
        fpsText.color = rainbowColor;
    }
}
