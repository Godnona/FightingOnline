using UnityEngine;

// Demo only: moves Train_Root along X through the station, waits, and starts again.
// In the game, move the train with your own logic; RipptopiaTrainSparks follows any movement.
public class RipptopiaTrainPassDemo : MonoBehaviour
{
    [Tooltip("Unity units per second. 480 = 12 m/s at the 40x scene scale, same as the approved video.")]
    public float speed = 480f;
    public float startX = -1600f;
    public float endX = 1600f;
    [Tooltip("Seconds the train stays out of view before the next pass (no sparks meanwhile).")]
    public float pauseSeconds = 1.5f;

    float waitUntil;

    void Start()
    {
        var p = transform.position; p.x = startX; transform.position = p;
    }

    void Update()
    {
        if (Time.time < waitUntil) return;
        var p = transform.position;
        p.x += speed * Time.deltaTime;
        if (p.x > endX) { p.x = startX; waitUntil = Time.time + pauseSeconds; }
        transform.position = p;
    }
}
