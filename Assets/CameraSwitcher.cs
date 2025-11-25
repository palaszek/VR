using System.Linq;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Tooltip("Indeks kamery w≥πczonej na starcie (jeúli øadna nie jest w≥πczona).")]
    [SerializeField] private int initialIndex = 0;

    private Camera[] cameras;
    private int activeIndex = -1;

    void Awake()
    {
        // Znajdü wszystkie kamery w dzieciach z tagiem "Camera"
        cameras = GetComponentsInChildren<Camera>(true)
                 .Where(c => c.CompareTag("Camera"))
                 .ToArray();

        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogWarning("[CameraSwitcher] Nie znaleziono kamer z tagiem \"Camera\" w dzieciach.");
            return;
        }

        // Jeúli jakaú jest juø w≥πczona, przyjmij jπ jako aktywnπ.
        // W razie wielu w≥πczonych ñ zostaw pierwszπ, resztÍ wy≥πcz.
        int firstEnabled = System.Array.FindIndex(cameras, c => c.enabled);
        if (firstEnabled >= 0)
        {
            SetActive(firstEnabled);
        }
        else
        {
            // Brak w≥πczonych ñ uøyj initialIndex (zabezpiecz zakres).
            int idx = Mathf.Clamp(initialIndex, 0, cameras.Length - 1);
            SetActive(idx);
        }
    }

    void Update()
    {
        if (cameras == null || cameras.Length == 0) return;

        // Prze≥πczanie pod klawiszem V z klasycznego Input
        if (Input.GetKeyDown(KeyCode.V))
        {
            int next = (activeIndex + 1) % cameras.Length;
            SetActive(next);
        }
    }

    private void SetActive(int index)
    {
        activeIndex = Mathf.Clamp(index, 0, cameras.Length - 1);

        for (int i = 0; i < cameras.Length; i++)
        {
            bool makeActive = (i == activeIndex);
            cameras[i].enabled = makeActive;

            // Opcjonalnie: zadbaj o pojedynczy AudioListener
            var listener = cameras[i].GetComponent<AudioListener>();
            if (listener) listener.enabled = makeActive;
        }

        // (opcjonalnie) log informacyjny
        // Debug.Log($"[CameraSwitcher] Aktywna kamera: {cameras[activeIndex].name}");
    }
}
