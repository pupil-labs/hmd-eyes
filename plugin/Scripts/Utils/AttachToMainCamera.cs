using UnityEngine;

public class AttachToMainCamera : MonoBehaviour
{
    void Start()
    {
        this.transform.SetParent(Camera.main.transform,false);
    }
}
