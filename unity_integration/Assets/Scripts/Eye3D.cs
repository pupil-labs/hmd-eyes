using UnityEngine;
using System.Collections;

public class Eye3D : MonoBehaviour
{

    public PupilListener pupil_listener_;
    Pupil.PupilData3D data_ = new Pupil.PupilData3D();

    Vector3 pos = new Vector3();
    Quaternion q = new Quaternion();
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        pupil_listener_.get_transform(ref pos, ref q);
        transform.position = pos;
        transform.rotation = q;
        Debug.Log(pos);
    }
}
