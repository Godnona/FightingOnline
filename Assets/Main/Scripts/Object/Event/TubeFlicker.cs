using UnityEngine;

public class TubeFlicker : MonoBehaviour
{
    [Header("Material")]
    public Material offMaterial;                                  // kéo M_LightOff vào đây
    public string emissiveMaterialName = "M_Emit_Station_Lamps";  // slot sẽ bị thay khi tắt
    public bool offAffectsAllSlots = false;                       // true = thay cả M_Metal_Atlas bằng M_LightOff

    [Header("Light đi kèm (tuỳ chọn)")]
    public Light lightSource;
    [Range(0f, 1f)] public float lightOffLevel = 0.9f;            // trong video tường gần như không đổi khi đèn tắt

    [Header("Thời gian (giây)")]
    public Vector2 onHold = new Vector2(1.0f, 8.0f);              // sáng yên giữa các lần nháy
    public Vector2 offHold = new Vector2(0.4f, 1.3f);             // tắt hẳn
    public Vector2 stutterTime = new Vector2(0.07f, 0.25f);       // đoạn nháy nhanh trước khi tắt / sau khi tắt

    [Header("Nháy nhanh")]
    public Vector2 stutterInterval = new Vector2(0.016f, 0.045f); // khoảng cách giữa 2 lần đổi sáng/tối (~1-3 frame)
    [Range(0f, 1f)] public float blipChance = 0.3f;               // xác suất chỉ nháy vài cái rồi sáng lại, không tắt dài
    [Range(0f, 1f)] public float stutterUpChance = 0.4f;          // xác suất bị giật thêm lúc sáng lại sau khi tắt

    enum State { On, StutterDown, Off, StutterUp }
    State state;
    float timer, nextTick, lightBase;
    bool tickOn, isOn = true, blipOnly;

    Renderer r;
    Material[] onMats, offMats;

    void Awake()
    {
        r = GetComponent<Renderer>();
        onMats = r.sharedMaterials;
        offMats = (Material[])onMats.Clone();

        if (offMaterial == null)
        {
            Debug.LogWarning($"{name}: chưa gán Off Material (M_LightOff)", this);
            enabled = false;
            return;
        }

        int emissiveIndex = -1;
        for (int i = 0; i < onMats.Length; i++)
        {
            if (onMats[i] != null && onMats[i].name.StartsWith(emissiveMaterialName))
            {
                emissiveIndex = i;
                break;
            }
        }
        if (emissiveIndex < 0 && !offAffectsAllSlots)
        {
            Debug.LogWarning($"{name}: không thấy material '{emissiveMaterialName}'", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < offMats.Length; i++)
            if (offAffectsAllSlots || i == emissiveIndex)
                offMats[i] = offMaterial;

        if (lightSource) lightBase = lightSource.intensity;

        state = State.On;
        timer = Random.Range(0f, onHold.y);   // lệch pha giữa các đèn
    }

    void Update()
    {
        timer -= Time.deltaTime;
        bool on = true;

        switch (state)
        {
            case State.On:
                if (timer <= 0f)
                {
                    blipOnly = Random.value < blipChance;
                    Go(State.StutterDown, stutterTime);
                }
                break;

            case State.StutterDown:
                on = Tick();
                if (timer <= 0f)
                {
                    if (blipOnly) Go(State.On, onHold);          // chỉ nháy vài cái rồi sáng lại
                    else          Go(State.Off, offHold);
                }
                break;

            case State.Off:
                on = false;
                if (timer <= 0f)
                {
                    if (Random.value < stutterUpChance) Go(State.StutterUp, stutterTime);
                    else                                Go(State.On, onHold);
                }
                break;

            case State.StutterUp:
                on = Tick();
                if (timer <= 0f) Go(State.On, onHold);
                break;
        }
        SetOn(on);
    }

    void Go(State s, Vector2 range)
    {
        state = s;
        timer = Random.Range(range.x, range.y);

        if (s == State.StutterDown || s == State.StutterUp)
        {
            nextTick = Time.time;                    // đổi trạng thái ngay frame đầu tiên
            tickOn = (s == State.StutterDown);       // StutterDown: sáng -> tắt trước, StutterUp: tắt -> sáng trước
        }
    }

    // Đảo sáng/tối liên tục (không random 50%) để thấy rõ kiểu strobe từng frame
    bool Tick()
    {
        if (Time.time >= nextTick)
        {
            tickOn = !tickOn;
            nextTick = Time.time + Random.Range(stutterInterval.x, stutterInterval.y);
        }
        return tickOn;
    }

    void SetOn(bool on)
    {
        if (on == isOn) return;
        isOn = on;
        r.sharedMaterials = on ? onMats : offMats;
        if (lightSource) lightSource.intensity = on ? lightBase : lightBase * lightOffLevel;
    }

    void OnDisable()
    {
        if (r != null && onMats != null) r.sharedMaterials = onMats;
        if (lightSource) lightSource.intensity = lightBase;
    }
}