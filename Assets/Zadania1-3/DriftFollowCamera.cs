using UnityEngine;

public class DriftFollowCamera : MonoBehaviour
{
    [Header("Cel")]
    public Transform target;
    public Rigidbody targetRb; // opcjonalnie – lepsze źródło pozycji/prędkości

    [Header("Ustawienie kamery")]
    public Vector3 offsetLocal = new Vector3(0f, 2.0f, -6.0f); // lokalny offset względem yaw/pitch kamery
    public float basePitch = 10f;       // stałe pochylenie w dół
    public float distanceScaleWithSpeed = 0.015f; // delikatne oddalenie przy dużej prędkości (0 = wyłącz)

    [Header("Wygładzanie obrotu (yaw)")]
    public float yawSmoothTime = 0.12f; // im większe, tym bardziej miękko
    public float maxYawSpeed = 360f;    // maks. prędkość zmiany yaw (°/s) – anty-flip
    public float lookAheadVelocityMin = 3.0f; // dopiero powyżej tej prędkości bierzemy kierunek prędkości

    [Header("Filtr prędkości (anty-drgania)")]
    public float velocityEasing = 0.15f; // 0..1 – im większe, tym mocniejszy filtr EMA

    [Header("Pozycja (wygładzanie opcjonalne)")]
    public float positionLerp = 20f; // 0 = natychmiast; 10–30 = miękko

    // wewnętrzne
    private float _currentYaw;     // aktualny kąt kamery (°)
    private float _yawVel;         // prędkość dla SmoothDampAngle
    private Vector3 _smoothedVel;  // przefiltrowana prędkość (EMA)

    void Reset()
    {
        positionLerp = 20f;
    }

    void Start()
    {
        if (!target && targetRb) target = targetRb.transform;
        if (!target) { Debug.LogWarning("[DriftFollowCamera] Brak targetu."); enabled = false; return; }

        // Startowy yaw z aktualnej rotacji kamery
        _currentYaw = transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- 1) Pozycja/vel źródłowa ---
        Vector3 targetPos = targetRb ? targetRb.position : target.position;
        Vector3 rawVel = targetRb ? targetRb.linearVelocity : (target.forward * 0f); // bez RB trudno o dobrą vel; zostanie użyty forward

        // Exponential Moving Average prędkości (anty-drgania)
        _smoothedVel = Vector3.Lerp(_smoothedVel, rawVel, 1f - Mathf.Exp(-velocityEasing * Time.deltaTime));

        // --- 2) Wyznacz docelowy yaw bez przeskoków 180° ---
        float desiredYaw;

        if (_smoothedVel.sqrMagnitude > lookAheadVelocityMin * lookAheadVelocityMin)
        {
            // Celuj w kierunek ruchu (stabilniejsze w poślizgu)
            Vector3 v = _smoothedVel;
            v.y = 0f;
            if (v.sqrMagnitude < 1e-6f)
                desiredYaw = target.eulerAngles.y; // awaryjnie – orientacja auta
            else
                desiredYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }
        else
        {
            // Wolno – trzymaj się obrotu auta (poziomy kąt)
            desiredYaw = target.eulerAngles.y;
        }

        // SmoothDampAngle + limit prędkości (anty-flip gdy auto obróci się gwałtownie)
        float smoothYaw = Mathf.SmoothDampAngle(_currentYaw, desiredYaw, ref _yawVel, yawSmoothTime, maxYawSpeed);
        _currentYaw = smoothYaw;

        // --- 3) Odległość rośnie delikatnie z prędkością (czytelniej w drifcie) ---
        float dynamicDistanceMul = 1f + _smoothedVel.magnitude * distanceScaleWithSpeed;
        Vector3 localOffsetNow = offsetLocal;
        localOffsetNow.z *= dynamicDistanceMul;

        // --- 4) Złóż rotację kamery (pitch stały, yaw wygładzany) ---
        Quaternion camRot = Quaternion.Euler(basePitch, _currentYaw, 0f);

        // --- 5) Pozycja kamery: target + obracany offset; z delikatnym lerpem pozycji (anty-drgania) ---
        Vector3 desiredPos = targetPos + camRot * localOffsetNow;
        if (positionLerp > 0f)
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-positionLerp * Time.deltaTime));
        else
            transform.position = desiredPos;

        // --- 6) Patrz na target (ale z ustalonym „up” – stabilne) ---
        transform.rotation = Quaternion.LookRotation((targetPos - transform.position).normalized, Vector3.up);
    }
}
