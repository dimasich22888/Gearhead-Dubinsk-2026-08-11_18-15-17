using UnityEngine;
using System.Collections.Generic;

namespace CarContollingScripts
{
    public class CarController : MonoBehaviour
    {
        public enum ControlMode { Keyboard, Buttons }
         [Header("Mobile Optimization")]
    public float mobileSteerSensitivity = 2f;
        public float mobileSteerAssist = 0.7f;
        public enum Axel { Front, Rear }
        public enum DriveType { FWD, RWD, AWD }

        [System.Serializable]
        public struct Wheel
        {
            public GameObject wheelModel;
            public WheelCollider wheelCollider;
            public Axel axel;
            public TrailRenderer trailRenderer;
            public bool isPowered;
            public bool isSteered;
        }

        [System.Serializable]
        public class SuspensionSettings
        {
            [Range(100f, 100000f)] public float springRate = 35000f;
            [Range(1000f, 50000)] public float damperRate = 4500f;
            [Range(0.01f,20f)] public float suspensionTravel = 0.2f;
            [Range(-10f, 10f)] public float wheelPositionY = -0.05f;
            [Range(0.1f, 0.9f)] public float antiRollEffect = 0.5f;
        }

        [System.Serializable]
        public class DrivetrainSettings
        {
            public DriveType driveType = DriveType.RWD;
            [Range(1f, 20f)] public float finalDriveRatio = 4.5f;
            [Range(0.1f, 1f)] public float tractionControl = 0.8f;
            public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 1, 1, 0.8f);
        }

        [System.Serializable]
        public class AerodynamicsSettings
        {
            [Range(0f, 5f)] public float downforceCoefficient = 1.5f;
            [Range(0f, 0.1f)] public float dragCoefficient = 0.05f;
        }

        [Header("Core Settings")]
        public ControlMode control;
        public Transform centerOfMass;

        [Header("Movement")]
        [Range(50f, 10000f)] public float maxMotorTorque = 650f;
        [Range(50f, 10000f)] public float maxBrakeTorque = 300f;
        [Range(50f, 1000f)] public float maxSpeed = 160f;
        [Range(10f, 200f)] public float maxReverseSpeed = 30f;
        [Range(0.1f, 5f)] public float brakeBalance = 0.7f;

        [Header("Steering")]
        [Range(5f, 45f)] public float maxSteerAngle = 30f;
        [Range(0.1f, 1f)] public float steerSensitivity = 0.8f;
        [Range(0.1f, 1f)] public float steerAssist = 0.5f;
        public AnimationCurve speedSensitiveSteering = new AnimationCurve(
            new Keyframe(0, 1), 
            new Keyframe(80, 0.7f), 
            new Keyframe(160, 0.4f)
        );

        [Header("Physics")]
        [Range(0.1f, 10f)] public float stability = 4f;
        [Range(0.01f, 0.5f)] public float minSlipForTrail = 0.2f;
        [Range(0.1f, 5f)] public float flipRecoverySpeed = 2f;

        [Header("Subsystems")]
        public SuspensionSettings suspension = new SuspensionSettings();
        public DrivetrainSettings drivetrain = new DrivetrainSettings();
        public AerodynamicsSettings aerodynamics = new AerodynamicsSettings();

        [Header("Wheels")]
        public List<Wheel> wheels;

        private Rigidbody carRb;
        private float moveInput;
        private float steerInput;
        private bool isBraking;
        private float currentSteerAngle;
        private float currentSpeed;
        private float engineRPM;
        private bool isUpright = true;
        private Vector3 lastPosition;

        void Start()
        {
            carRb = GetComponent<Rigidbody>();
            if (centerOfMass) carRb.centerOfMass = centerOfMass.localPosition;

            lastPosition = transform.position;
            ConfigureWheelColliders();
            InitializeWheelEffects();
             #if UNITY_ANDROID || UNITY_IOS
        steerSensitivity = mobileSteerSensitivity;
        steerAssist = mobileSteerAssist;
        #endif
        }

        void ConfigureWheelColliders()
        {
            foreach (var wheel in wheels)
            {
                var collider = wheel.wheelCollider;
                
                // Configure suspension
                JointSpring spring = collider.suspensionSpring;
                spring.spring = suspension.springRate;
                spring.damper = suspension.damperRate;
                collider.suspensionSpring = spring;
                collider.suspensionDistance = suspension.suspensionTravel;
                
                // Configure wheel position
                Vector3 pos = collider.transform.localPosition;
                pos.y = suspension.wheelPositionY;
                collider.transform.localPosition = pos;
                
                // Configure friction
                WheelFrictionCurve forwardFriction = collider.forwardFriction;
                forwardFriction.asymptoteValue = 5000f;
                collider.forwardFriction = forwardFriction;
                
                WheelFrictionCurve sidewaysFriction = collider.sidewaysFriction;
                sidewaysFriction.stiffness = 1.5f;
                collider.sidewaysFriction = sidewaysFriction;
            }
        }

        void InitializeWheelEffects()
        {
            foreach (var wheel in wheels)
            {
                if (wheel.trailRenderer != null)
                {
                    wheel.trailRenderer.emitting = false;
                    wheel.trailRenderer.autodestruct = false;
                }
            }
        }

        void Update()
        {
            GetInputs();
            UpdateEngineRPM();
            AnimateWheels();
            WheelEffects();
            CheckUprightStatus();
        }

        void FixedUpdate()
        {
            currentSpeed = carRb.linearVelocity.magnitude * 3.6f; // km/h
            
            ApplyAerodynamics();
            ApplyAntiRollBars();
            ApplyStabilization();
            Move();
            Steer();
            Brake();
        }

        void GetInputs()
        {
            if (control == ControlMode.Keyboard)
            {
                moveInput = Input.GetAxis("Vertical");
                steerInput = Input.GetAxis("Horizontal");
                isBraking = Input.GetKey(KeyCode.Space);
            }
            // Button control implementation would go here
        }

        void UpdateEngineRPM()
        {
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
            engineRPM = Mathf.Lerp(800, 7000, speedRatio * drivetrain.torqueCurve.Evaluate(speedRatio));
        }

        void Move()
        {
            float torqueMultiplier = drivetrain.torqueCurve.Evaluate(currentSpeed / maxSpeed);
            float effectiveTorque = moveInput * maxMotorTorque * torqueMultiplier * drivetrain.finalDriveRatio;
            
            foreach (var wheel in wheels)
            {
                if (!wheel.isPowered) continue;
                
                // Traction control
                WheelHit hit;
                if (wheel.wheelCollider.GetGroundHit(out hit))
                {
                    float slip = Mathf.Abs(hit.forwardSlip);
                    if (slip > drivetrain.tractionControl)
                    {
                        effectiveTorque *= 1 - (slip - drivetrain.tractionControl);
                    }
                }
                
                wheel.wheelCollider.motorTorque = effectiveTorque * Time.fixedDeltaTime;
            }
        }

        void Steer()
        {
            // Remove any smoothing - use raw input directly
        float speedFactor = speedSensitiveSteering.Evaluate(currentSpeed);
        float targetAngle = steerInput * maxSteerAngle * speedFactor;
        
        // Apply steering immediately
        foreach (var wheel in wheels)
        {
            if (wheel.isSteered)
            {
                wheel.wheelCollider.steerAngle = targetAngle;
            }
        }
            
        }

        float GetSteeringAssist()
        {
            // Simulate wheel's natural tendency to straighten
            return -currentSteerAngle * (1 - (Mathf.Abs(steerInput)));
        }

        void Brake()
        {
            float frontBrake = isBraking ? maxBrakeTorque * brakeBalance : 0;
            float rearBrake = isBraking ? maxBrakeTorque * (1 - brakeBalance) : 0;
            
            foreach (var wheel in wheels)
            {
                if (wheel.axel == Axel.Front)
                {
                    wheel.wheelCollider.brakeTorque = frontBrake;
                }
                else
                {
                    wheel.wheelCollider.brakeTorque = rearBrake;
                }
            }
        }

        void ApplyAerodynamics()
        {
            // Downforce increases with speed squared
            float downforce = aerodynamics.downforceCoefficient * currentSpeed * currentSpeed / 1000f;
            carRb.AddForce(-transform.up * downforce);
            
            // Drag
            Vector3 drag = -carRb.linearVelocity.normalized * aerodynamics.dragCoefficient * carRb.linearVelocity.sqrMagnitude;
            carRb.AddForce(drag);
        }

        void ApplyAntiRollBars()
        {
            for (int i = 0; i < wheels.Count; i += 2)
            {
                if (i + 1 >= wheels.Count) continue;
                
                WheelCollider left = wheels[i].wheelCollider;
                WheelCollider right = wheels[i + 1].wheelCollider;
                
                WheelHit leftHit, rightHit;
                bool leftGrounded = left.GetGroundHit(out leftHit);
                bool rightGrounded = right.GetGroundHit(out rightHit);
                
                float leftForce = leftGrounded ? leftHit.force : 0;
                float rightForce = rightGrounded ? rightHit.force : 0;
                float forceDifference = Mathf.Abs(leftForce - rightForce);
                
                if (leftGrounded && rightGrounded)
                {
                    float antiRollForce = forceDifference * suspension.antiRollEffect;
                    
                    if (leftForce > rightForce)
                    {
                        carRb.AddForceAtPosition(left.transform.up * -antiRollForce, left.transform.position);
                        carRb.AddForceAtPosition(left.transform.up * antiRollForce, right.transform.position);
                    }
                    else
                    {
                        carRb.AddForceAtPosition(right.transform.up * -antiRollForce, right.transform.position);
                        carRb.AddForceAtPosition(right.transform.up * antiRollForce, left.transform.position);
                    }
                }
            }
        }

        void ApplyStabilization()
        {
            // Only apply stabilization when not upright
            if (isUpright) return;
            
            // Calculate righting torque
            Vector3 torqueDirection = Vector3.Cross(transform.up, Vector3.up);
            float angle = Vector3.Angle(transform.up, Vector3.up);
            
            carRb.AddTorque(
                torqueDirection * angle * stability * Time.fixedDeltaTime,
                ForceMode.VelocityChange
            );
            
            // Add vertical lift when upside down
            if (Vector3.Dot(transform.up, Vector3.down) > 0.7f)
            {
                carRb.AddForce(Vector3.up * 9.81f * 2f, ForceMode.Acceleration);
            }
        }

        void CheckUprightStatus()
        {
            isUpright = Vector3.Angle(transform.up, Vector3.up) < 70f;
        }

        void AnimateWheels()
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
                wheel.wheelModel.transform.SetPositionAndRotation(pos, rot);
            }
        }

        void WheelEffects()
        {
            foreach (var wheel in wheels)
            {
                if (wheel.trailRenderer == null) continue;
                
                WheelHit hit;
                if (!wheel.wheelCollider.GetGroundHit(out hit)) 
                {
                    wheel.trailRenderer.emitting = false;
                    continue;
                }
                
                bool shouldEmit = (Mathf.Abs(hit.sidewaysSlip) > minSlipForTrail || 
                                 Mathf.Abs(hit.forwardSlip) > minSlipForTrail) && 
                                 currentSpeed > 5f;
                
                wheel.trailRenderer.emitting = shouldEmit;
                
                // Adjust trail length based on slip intensity
                if (shouldEmit)
                {
                    float slipIntensity = Mathf.Max(
                        Mathf.Abs(hit.sidewaysSlip), 
                        Mathf.Abs(hit.forwardSlip)
                    );
                    wheel.trailRenderer.time = Mathf.Clamp(slipIntensity * 0.5f, 0.1f, 1f);
                }
            }
        }

        // Public methods for UI controls
        public void MoveInput(float input) => moveInput = input;
        public void SteerInput(float input) => steerInput = input;
        public void BrakeInput(bool input) => isBraking = input;
    }
}