using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CarDriverController : MonoBehaviour
{
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable outerHandle;
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable innerHandle;
    [SerializeField] Transform driverPosition;
    [SerializeField] Transform xrOrigin;
    [SerializeField] Vector3 exitOffset = new Vector3(0, 0, -1f);

    private bool isPlayerInside = false;

    // Wszystkie collidery XR Rigu
    private Collider[] xrColliders;

    private void Start()
    {
        outerHandle.selectEntered.AddListener(OnHandleUsed);
        innerHandle.selectEntered.AddListener(OnHandleUsed);

        // Pobierz wszystkie collidery XR Origin
        xrColliders = xrOrigin.GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            xrOrigin.position = driverPosition.position;
            xrOrigin.rotation = driverPosition.rotation;
        }
    }

    private void OnHandleUsed(SelectEnterEventArgs args)
    {
        if (isPlayerInside)
            ExitCar();
        else
            EnterCar();
    }

    private void EnterCar()
    {
        isPlayerInside = true;

        xrOrigin.position = driverPosition.position;
        xrOrigin.rotation = driverPosition.rotation;

        SetXRCollisions(false);
    }

    private void ExitCar()
    {
        isPlayerInside = false;

        xrOrigin.position = driverPosition.position + exitOffset;

        SetXRCollisions(true);
    }

    private void SetXRCollisions(bool enabled)
    {
        if (xrColliders == null) return;

        foreach (var col in xrColliders)
        {
            col.enabled = enabled;
        }
    }
}
