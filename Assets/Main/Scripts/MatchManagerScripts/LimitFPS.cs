using UnityEngine;

public class LimitFPS : MonoBehaviour
{
    private const int TargetFrameRate = 60;

    private void Awake()
    {
        // VSync overrides Application.targetFrameRate on desktop platforms.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }
}
