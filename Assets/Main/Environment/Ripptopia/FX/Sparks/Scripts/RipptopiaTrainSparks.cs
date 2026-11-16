using System.Collections.Generic;
using UnityEngine;

// Rail sparks for the Ripptopia train.
// Put this on Train_Root (the object that moves). On Start it finds the empties
// FX_Spark_Socket_Near_00..03 (and Far_00..03 if enabled), puts one spark emitter on each
// and points it outwards, away from the train. Sparks are emitted only while the train moves.
public class RipptopiaTrainSparks : MonoBehaviour
{
    [Tooltip("Assets/Ripptopia/FX/Sparks/FX_RailSparks.prefab")]
    public ParticleSystem sparkPrefab;

    [Tooltip("Far sockets are on the side away from the game camera. Only needed if the camera changes.")]
    public bool includeFarSockets = false;

    [Tooltip("Base sparks per second per socket at scale 1. Smaller scales automatically emit more sparks to preserve visual density.")]
    [Min(0f)] public float sparksPerSecond = 100f;

    [Tooltip("Uniform scale applied to each spawned spark emitter.")]
    [Min(0f)] public float sparkScale = 1f;

    [Tooltip("Below this speed (Unity units per second) no sparks. 20 = 0.5 m/s at the 40x scene scale.")]
    public float minSpeed = 20f;

    [Tooltip("How much the sparks aim upwards (0 = flat towards the camera side).")]
    [Range(0f, 1f)] public float upTilt = 0.25f;

    readonly List<ParticleSystem> emitters = new List<ParticleSystem>();
    Vector3 lastPosition;

    public bool IsEmitting { get; private set; }

    void Start() => Setup();

    void LateUpdate() => Tick(Time.deltaTime);

    public void Setup()
    {
        if (emitters.Count > 0) return;
        if (sparkPrefab == null) { Debug.LogWarning("RipptopiaTrainSparks: no spark prefab assigned.", this); return; }
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            bool near = t.name.StartsWith("FX_Spark_Socket_Near");
            bool far = t.name.StartsWith("FX_Spark_Socket_Far");
            if (!near && !(far && includeFarSockets)) continue;

            var ps = Instantiate(sparkPrefab, t.position, Quaternion.identity, t);
            ps.name = "FX_RailSparks";
            ps.transform.localScale = Vector3.one * sparkScale;
            // outwards = from the train centre line to the socket, sideways only
            Vector3 side = Vector3.ProjectOnPlane(t.position - transform.position, transform.right);
            side = Vector3.ProjectOnPlane(side, Vector3.up).normalized;
            if (side == Vector3.zero) side = -transform.forward;
            ps.transform.rotation = Quaternion.LookRotation((side + Vector3.up * upTilt).normalized, Vector3.up);
            emitters.Add(ps);
        }
        if (emitters.Count == 0) Debug.LogWarning("RipptopiaTrainSparks: no FX_Spark_Socket_* found under " + name, this);
        lastPosition = transform.position;
        SetEmitting(false);
    }

    public void Tick(float dt)
    {
        if (dt <= 0f) return;
        float speed = (transform.position - lastPosition).magnitude / dt;
        lastPosition = transform.position;
        SetEmitting(speed > minSpeed);
    }

    void SetEmitting(bool on)
    {
        IsEmitting = on;
        float scaledEmissionRate = sparkScale > 0f ? sparksPerSecond / sparkScale : 0f;
        foreach (var ps in emitters)
        {
            var em = ps.emission;
            em.rateOverTime = on ? scaledEmissionRate : 0f; // sparks already in the air finish their life
        }
    }
}
