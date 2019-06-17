 using UnityEngine;
 [RequireComponent(typeof(Camera))]

 public class ScreenCastCameraFlipY : MonoBehaviour {
     public bool flip;
     
     new Camera camera;
     
     void Awake () {
         camera = GetComponent<Camera>();
     }
     void OnPreCull() {
         camera.ResetWorldToCameraMatrix();
         camera.ResetProjectionMatrix();
         
         Vector3 scale = new Vector3(1, flip ? -1 : 1, 1);
         camera.projectionMatrix = camera.projectionMatrix * Matrix4x4.Scale(scale);
     }
     void OnPreRender () {
         GL.invertCulling = flip;
     }
     
     void OnPostRender () {
         GL.invertCulling = false;
     }
 }