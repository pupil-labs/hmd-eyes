using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PupilLabs
{
    public class Origin : MonoBehaviour
    {
        public Transform vrRigOrigin;

        void Start()
        {
            if (vrRigOrigin == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("No VR rig origin assinged and no main camera found.");
                }
                else
                {
                    vrRigOrigin = mainCamera.transform.parent;
                }
            }

            transform.parent = vrRigOrigin;
        }

        void Update()
        {
            transform.localPosition = InputTracking.GetLocalPosition(XRNode.CenterEye);
            transform.localRotation = InputTracking.GetLocalRotation(XRNode.CenterEye);
        }
    }
}
