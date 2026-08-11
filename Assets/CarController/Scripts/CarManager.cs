using System.Collections.Generic;
using UnityEngine;
using CameraControlsforcar;

namespace FreakyDevs.CarSystem
{
    public class CarManager : MonoBehaviour
    {
        public List<GameObject> cars;
        public int currentCarIndex = 0;
        public CameraFollow cameraFollow;

        void Start()
        {
            ActivateCar(currentCarIndex);
        }

        void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCar(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCar(1);
#endif
        }

        public void SwitchCar(int newIndex)
        {
            if (newIndex < 0 || newIndex >= cars.Count) return;

            cars[currentCarIndex].SetActive(false);
            currentCarIndex = newIndex;
            ActivateCar(currentCarIndex);
        }

        void ActivateCar(int index)
        {
            GameObject car = cars[index];
            car.SetActive(true);
            cameraFollow.SetTarget(car.transform);
            cameraFollow.ResetCameraPosition();
        }
        public void SwitchToNextCar()
{
    int nextIndex = (currentCarIndex + 1) % cars.Count;
    SwitchCar(nextIndex);
}

    }
}
