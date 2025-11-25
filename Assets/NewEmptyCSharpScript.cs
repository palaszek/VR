using UnityEngine;

/// <summary>
/// Pisk opon sterowany maksymalnym uœlizgiem z czterech WheelColliderów.
/// Podepnij JEDEN AudioSource z zapêtlonym klipem „squeal” (loop = true, playOnAwake = false).
/// </summary>
public class CarSkidAudio : MonoBehaviour
{
    [Header("Ko³a (4x WheelCollider)")]
    [SerializeField] private WheelCollider frontLeft;
    [SerializeField] private WheelCollider frontRight;
    [SerializeField] private WheelCollider rearLeft;
    [SerializeField] private WheelCollider rearRight;

    [Header("Audio (jeden wspólny)")]
    [SerializeField] private AudioSource skidAudio;

    [Header("Progi/histereza")]
    [Tooltip("Uœlizg, od którego zaczynamy graæ")]
    [SerializeField, Range(0f, 2f)] private float slipStart = 0.25f;

    [Tooltip("Uœlizg, przy którym pozwalamy siê wyciszyæ/wy³¹czyæ")]
    [SerializeField, Range(0f, 2f)] private float slipStop = 0.15f;

    [Header("Dynamika brzmienia")]
    [Tooltip("Uœlizg odpowiadaj¹cy maksymalnej g³oœnoœci")]
    [SerializeField, Range(0.2f, 5f)] private float slipForMaxVolume = 0.9f;

    [Tooltip("Maksymalna g³oœnoœæ")]
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.85f;

    [Tooltip("Maksymalny pitch przy du¿ym uœlizgu")]
    [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.15f;

    [Tooltip("Szybkoœæ wyg³adzania zmian")]
    [SerializeField, Range(1f, 30f)] private float smoothing = 12f;

    private void Reset()
    {
        if (skidAudio == null) skidAudio = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (skidAudio == null)
            return;

        float maxSlip;
        bool anyGrounded = TryGetMaxSlip(out maxSlip);

        bool shouldPlay = anyGrounded && maxSlip >= slipStart;
        bool shouldStop = !anyGrounded || maxSlip <= slipStop;

        float slip01 = Mathf.InverseLerp(slipStop, slipForMaxVolume, maxSlip);
        float targetVolume = shouldPlay ? Mathf.Clamp01(slip01) * maxVolume : 0f;

        skidAudio.volume = Mathf.Lerp(skidAudio.volume, targetVolume, Time.deltaTime * smoothing);

        float pitchTarget = Mathf.Lerp(0.9f, maxPitch, maxVolume > 0f ? skidAudio.volume / maxVolume : 0f);
        skidAudio.pitch = Mathf.Lerp(skidAudio.pitch, pitchTarget, Time.deltaTime * smoothing);

        if (shouldPlay && !skidAudio.isPlaying)
        {
            skidAudio.Play();
        }
        else if (shouldStop && skidAudio.isPlaying && skidAudio.volume < 0.02f)
        {
            skidAudio.Stop();
            skidAudio.volume = 0f;
        }
    }
    private bool TryGetMaxSlip(out float maxSlip)
    {
        maxSlip = 0f;
        bool anyGrounded = false;

        AccumulateSlip(frontLeft, ref maxSlip, ref anyGrounded);
        AccumulateSlip(frontRight, ref maxSlip, ref anyGrounded);
        AccumulateSlip(rearLeft, ref maxSlip, ref anyGrounded);
        AccumulateSlip(rearRight, ref maxSlip, ref anyGrounded);

        return anyGrounded;
    }

    private static void AccumulateSlip(WheelCollider wheel, ref float maxSlip, ref bool anyGrounded)
    {
        if (wheel == null) return;

        WheelHit hit;
        if (wheel.GetGroundHit(out hit))
        {
            anyGrounded = true;
            float slip = Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip);
            if (slip > maxSlip) maxSlip = slip;
        }
    }
}
