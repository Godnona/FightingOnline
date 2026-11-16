using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoBackgroundStarter : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage rawImage;
    public RenderTexture renderTexture;

    void Start()
    {
        Debug.Log("VIDEO SCRIPT STARTED");

        videoPlayer.Stop();

        videoPlayer.targetTexture = renderTexture;
        rawImage.texture = renderTexture;

        Debug.Log("PREPARING VIDEO...");
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnPrepared;
    }

    void OnPrepared(VideoPlayer vp)
    {
        Debug.Log("VIDEO PREPARED — PLAYING");
        vp.Play();
    }
}