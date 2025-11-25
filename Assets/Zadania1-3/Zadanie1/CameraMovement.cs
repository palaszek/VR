using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    // --- USTAWIENIA (regulowane w Inspectorze) ---
    [Header("Mysz")]
    [SerializeField] float mouseSensitivity = 2.0f;

    [Header("Ruch")]
    [SerializeField] float baseMoveSpeed = 5.0f;     // prêdkoœæ bazowa
    [SerializeField] float sprintMultiplier = 1.8f;  // mno¿nik gdy Shift
    [SerializeField] float slowMultiplier = 0.5f;    // mno¿nik gdy Ctrl
    [SerializeField] float minSpeedScale = 0.2f;     // minimalna skala prêdkoœci (rolka)
    [SerializeField] float maxSpeedScale = 5.0f;     // maksymalna skala prêdkoœci (rolka)
    [SerializeField] float scrollScaleStep = 0.2f;   // o ile zmienia rolka

    // --- Stan wewnêtrzny ---
    float rotationX;
    float rotationY;
    float speedScale = 1.0f; // dynamiczna skala prêdkoœci (rolka myszy)

    void Start()
    {
        rotationX = transform.localEulerAngles.y;
        rotationY = transform.localEulerAngles.x;
    }

    void Update()
    {
        // --- ROTACJA KAMERY (z czu³oœci¹) ---
        rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationY += Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);
        transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0f);

        // --- SKALA PRÊDKOŒCI POD ROLK¥ ---
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            speedScale = Mathf.Clamp(speedScale + scroll * scrollScaleStep, minSpeedScale, maxSpeedScale);
        }

        // --- WYZNACZENIE PRÊDKOŒCI KOÑCOWEJ ---
        float speed = baseMoveSpeed * speedScale;

        // sprint (Shift) i powolny ruch (Ctrl)
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= sprintMultiplier;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            speed *= slowMultiplier;

        // --- RUCH (znormalizowany, ¿eby po skosie nie by³o szybciej) ---
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        float dx = Input.GetAxisRaw("Horizontal");
        float dz = Input.GetAxisRaw("Vertical");

        Vector3 move = (forward * dz + right * dx);
        if (move.sqrMagnitude > 1e-6f)
            move = move.normalized;

        transform.position += move * speed * Time.deltaTime;
    }

    // (opcjonalnie) ma³y HUD do debugowania skali prêdkoœci
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Speed scale: {speedScale:0.0}x");
    }
}
