using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        transform.LookAt(target,Vector3.up);
    }
}
