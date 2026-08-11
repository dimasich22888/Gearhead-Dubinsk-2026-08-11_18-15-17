using UnityEngine;
using UnityEngine.InputSystem;

public class CarController2 : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider wheelFL;
    [SerializeField] private WheelCollider wheelFR;
    [SerializeField] private WheelCollider wheelRL;
    [SerializeField] private WheelCollider wheelRR;

    [Header("Visual Wheels")]
    [SerializeField] private Transform visualFL;
    [SerializeField] private Transform visualFR;
    [SerializeField] private Transform visualRL;
    [SerializeField] private Transform visualRR;

    [Header("Engine")]
    [SerializeField] private float motorTorque = 1500f;
    [SerializeField] public float throttleSpeed = 1.5f;

    [Header("Steering")]
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("Brakes")]
    [SerializeField] public float brakeTorque = 3000f;

    public float throttle;
    private float steering;
    public bool braking;

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        HandleSteering();
        HandleMotor();
        HandleBrakes();
    }

    private void LateUpdate()
    {
        UpdateWheelVisual(wheelFL, visualFL);
        UpdateWheelVisual(wheelFR, visualFR);
        UpdateWheelVisual(wheelRL, visualRL);
        UpdateWheelVisual(wheelRR, visualRR);
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
            return;

        /*
         * ГАЗ
         *
         * W:
         * throttle постепенно идёт к 1
         *
         * S:
         * throttle постепенно идёт к -1
         *
         * Ничего:
         * throttle СРАЗУ становится 0
         */

        if (Keyboard.current.wKey.isPressed)
        {
            throttle = Mathf.MoveTowards(
                throttle,
                1f,
                throttleSpeed * Time.deltaTime
            );
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            throttle = Mathf.MoveTowards(
                throttle,
                -1f,
                throttleSpeed * Time.deltaTime
            );
        }
        else
        {
            throttle = 0f;
        }

        /*
         * РУЛЬ
         */

        steering = 0f;

        if (Keyboard.current.aKey.isPressed)
        {
            steering = -1f;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            steering = 1f;
        }

        /*
         * ТОРМОЗ
         */

        braking = Keyboard.current.spaceKey.isPressed;
    }

    private void HandleSteering()
    {
        float steerAngle = steering * maxSteerAngle;

        wheelFL.steerAngle = steerAngle;
        wheelFR.steerAngle = steerAngle;
    }

    private void HandleMotor()
    {
        float torque = throttle * motorTorque;

        // Задний привод
        wheelRL.motorTorque = torque;
        wheelRR.motorTorque = torque;
    }

    private void HandleBrakes()
    {
        float brake = braking ? brakeTorque : 0f;

        wheelFL.brakeTorque = brake;
        wheelFR.brakeTorque = brake;
        wheelRL.brakeTorque = brake;
        wheelRR.brakeTorque = brake;
    }

    private void UpdateWheelVisual(
        WheelCollider wheelCollider,
        Transform visualWheel)
    {
        if (wheelCollider == null || visualWheel == null)
            return;

        wheelCollider.GetWorldPose(
            out Vector3 position,
            out Quaternion rotation
        );

        visualWheel.position = position;

        // Если модель колеса повернута относительно WheelCollider
        visualWheel.rotation =
            rotation * Quaternion.Euler(0f, 90f, 0f);
    }
}