using UnityEngine;

namespace PupilLabs.Demos
{
    public class MirrorHead : MonoBehaviour
    {
        public Transform target;
        public new Transform transform;

        void Update()
        {
            Vector3 pos = transform.parent.position;
            pos.y = target.position.y - transform.localPosition.y; 
            transform.parent.position = pos;

            transform.LookAt(target);
            
            Vector3 targetDirection = target.TransformDirection(Vector3.forward);
            Vector3 targetOffset = transform.position-target.position;
            
            float angleY = Vector3.SignedAngle(Vector3.ProjectOnPlane(targetOffset,Vector3.up), Vector3.ProjectOnPlane(targetDirection,Vector3.up), Vector3.up);
            float angleX = Vector3.SignedAngle(Vector3.ProjectOnPlane(targetOffset,Vector3.forward), Vector3.ProjectOnPlane(targetDirection,Vector3.forward), Vector3.back);
            
            transform.Rotate(new Vector3(angleX,-angleY,0));
        }
    }
}
