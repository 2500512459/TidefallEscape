using UnityEngine;

public class BoatDynamics : MonoBehaviour
{
    [SerializeField] private float finalSpeed = 100f;
    [SerializeField] private float inertiaFactor = 0.005f;
    [SerializeField] private float turningFactor = 2.0f;
    [SerializeField] private float accelerationTorqueFactor = 35f;
    [SerializeField] private float turningTorqueFactor = 35f;

    private float verticalImpetus = 0f;
    private float horizontalImpetus = 0f;
    private Rigidbody rigidbodyComponent;
    private Vector2 androidInputInit;

    private float acceleration = 0f;
    private float accelerationBreak;

    public float FinalSpeed { set { finalSpeed = value; } get { return finalSpeed; } }

    void Start()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();

        accelerationBreak = finalSpeed * 0.3f;
    }

    public void SetImpetus(float verticalImpetus, float horizontalImpetus)
    {
        this.verticalImpetus = verticalImpetus;
        this.horizontalImpetus = horizontalImpetus;
    }

    void FixedUpdate()
    {
        if (verticalImpetus > 0)
        {
            if (acceleration < finalSpeed)
            {
                acceleration += (finalSpeed * inertiaFactor);
                acceleration *= verticalImpetus;
            }
        }
        else if (verticalImpetus == 0)
        {
            if (acceleration > 0)
            {
                acceleration -= finalSpeed * inertiaFactor;
            }
            if (acceleration < 0)
            {
                acceleration += finalSpeed * inertiaFactor;
            }
        }
        else if (verticalImpetus < 0)
        {
            if (acceleration > -accelerationBreak)
            {
                acceleration -= finalSpeed * inertiaFactor * 2;
            }
        }

        rigidbodyComponent.AddRelativeForce(Vector3.forward * acceleration);

        rigidbodyComponent.AddRelativeTorque(
            verticalImpetus * -accelerationTorqueFactor,
            horizontalImpetus * turningFactor,
            horizontalImpetus * -turningTorqueFactor
        );
    }

    static float Lerp(float from, float to, float value)
    {
        if (value < 0.0f) return from;
        else if (value > 1.0f) return to;
        return (to - from) * value + from;
    }
}
