using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public enum InputMode { MouseRMB, LegacyAxes }

    [Header("Cel")]
    public Transform target;
    public float distance = 5f;

    [Header("Sterowanie")]
    public InputMode inputMode = InputMode.MouseRMB;

    [Tooltip("Nazwy osi z Input Managera (Edit > Project Settings > Input Manager)")]
    public string axisX = "RightStick X";
    public string axisY = "RightStick Y";

    public float sensitivityX = 150f; // deg/s
    public float sensitivityY = 150f; // deg/s
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public bool invertY = false;

    private float yaw;
    private float pitch;

    void Start()
    {
        if (!target) { Debug.LogWarning("[OrbitCamera] Brak targetu."); enabled = false; return; }

        // wyznacz startowe kąty z aktualnej pozycji
        Vector3 dir = (transform.position - target.position).normalized;
        pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        float cosPitch = Mathf.Max(1e-6f, Mathf.Cos(pitch * Mathf.Deg2Rad));
        yaw = Mathf.Atan2(dir.x / cosPitch, dir.z / cosPitch) * Mathf.Rad2Deg;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        ApplyOrbit();
    }

    void LateUpdate()
    {
        if (!target) return;

        float dx = 0f, dy = 0f;

        if (inputMode == InputMode.MouseRMB)
        {
            if (Input.GetMouseButton(1))
            {
                dx = Input.GetAxis("Mouse X");
                dy = Input.GetAxis("Mouse Y");
            }
        }
        else // LegacyAxes
        {
            dx = SafeGetAxis(axisX);
            dy = SafeGetAxis(axisY);
        }

        if (Mathf.Abs(dx) > Mathf.Epsilon || Mathf.Abs(dy) > Mathf.Epsilon)
        {
            yaw += dx * sensitivityX * Time.deltaTime;
            float dyScaled = (invertY ? dy : -dy) * sensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch + dyScaled, minPitch, maxPitch);
            ApplyOrbit();
        }
    }

    float SafeGetAxis(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0f;
        try { return Input.GetAxis(name); }
        catch (System.ArgumentException)
        {
            // Oś nie istnieje – log tylko raz na jakiś czas żeby nie spamować
            // (możesz to usunąć po ustawieniu osi)
            // Debug.LogWarning($"[OrbitCamera] Brak osi \"{name}\" w Input Managerze.");
            return 0f;
        }
    }

    void ApplyOrbit()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rot * new Vector3(0, 0, -distance);
        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
    }
}
