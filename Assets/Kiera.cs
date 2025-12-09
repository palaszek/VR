using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DrivingWheel : XRBaseInteractable
{
    [SerializeField] private Transform wheelTransform;
    private float currentAngle = 0.0f;
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        currentAngle = FindWheelAngle();
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        currentAngle = FindWheelAngle();
    }
    public override void
    ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected)
                RotateWheel();
        }
    }
    private void RotateWheel()
    {
        float totalAngle = FindWheelAngle();
        float angleDifference = currentAngle - totalAngle;
        wheelTransform.Rotate(transform.up, angleDifference, Space.World);
        currentAngle = totalAngle;
    }
    private float FindWheelAngle()
    {
        float totalAngle = 0;
        foreach (UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor in interactorsSelecting)
        {
            Vector3 direction =
            FindLocalPoint(interactor.transform.position);
            totalAngle += ConvertToAngle(direction);
        }
        return totalAngle;
    }
    private Vector3 FindLocalPoint(Vector3 position)
    {
        return transform.InverseTransformPoint(position).normalized;
    }
    private float ConvertToAngle(Vector3 direction)
    {
        return Vector2.SignedAngle(Vector2.right, new Vector2(direction.x,
        direction.z));
    }
}
