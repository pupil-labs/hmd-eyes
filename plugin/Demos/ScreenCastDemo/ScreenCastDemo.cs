using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenCastDemo : MonoBehaviour
{

    public PupilLabs.ScreenCast screenCast;
    public new MeshRenderer renderer;

    void OnEnable()
    {
        renderer.material.mainTexture = screenCast.StreamTexture;
    }
}
