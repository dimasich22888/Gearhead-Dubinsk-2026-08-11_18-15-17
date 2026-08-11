using UnityEngine;
using FreakyDevs.CarSystem;
using CarContollingScripts;

namespace MobileInputForCar
{
    public class MobileInput : MonoBehaviour
    {
        public CarManager carManager;
        
        private CarController CurrentCarController => 
            carManager.cars[carManager.currentCarIndex].GetComponent<CarController>();
        
        private bool isReversing = false;
        private float currentSteerInput = 0f;

        public void OnBrakeOrReverseButtonDown()
        {
            var rb = CurrentCarController.GetComponent<Rigidbody>();
            
            if (rb.linearVelocity.magnitude > 0.5f)
            {
                isReversing = false;
                CurrentCarController.BrakeInput(true);
            }
            else
            {
                isReversing = true;
                CurrentCarController.MoveInput(-1f);
            }
        }

        public void OnBrakeOrReverseButtonUp()
        {
            if (isReversing)
                CurrentCarController.MoveInput(0f);
            else
                CurrentCarController.BrakeInput(false);
        }
        
        public void OnAccelerateButtonDown() => CurrentCarController.MoveInput(1f);
        public void OnAccelerateButtonUp() => CurrentCarController.MoveInput(0f);

        public void OnSteerLeftButtonDown()
        {
            currentSteerInput = -1f;
            CurrentCarController.SteerInput(currentSteerInput);
        }

        public void OnSteerRightButtonDown()
        {
            currentSteerInput = 1f;
            CurrentCarController.SteerInput(currentSteerInput);
        }

        public void OnSteerLeftButtonUp()
        {
            if (currentSteerInput < 0)
            {
                currentSteerInput = 0f;
                CurrentCarController.SteerInput(0f);
            }
        }

        public void OnSteerRightButtonUp()
        {
            if (currentSteerInput > 0)
            {
                currentSteerInput = 0f;
                CurrentCarController.SteerInput(0f);
            }
        }

        public void OnSwitchCarPressed() => carManager.SwitchToNextCar();
    }
}