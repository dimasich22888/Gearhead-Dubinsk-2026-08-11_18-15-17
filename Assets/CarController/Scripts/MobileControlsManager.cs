using UnityEngine;

namespace FreakyDevs.MobileControls // Use your studio or project name
{
    public class MobileControlManager : MonoBehaviour
    {
        public GameObject mobileControls;
        
        void Start()
        {
            #if UNITY_ANDROID || UNITY_IOS
            mobileControls.SetActive(true);
            #else
            mobileControls.SetActive(false);
            #endif
        }
    }
}
