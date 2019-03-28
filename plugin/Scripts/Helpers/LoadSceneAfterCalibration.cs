using UnityEngine;
using UnityEngine.SceneManagement;

namespace PupilLabs
{
    public class LoadSceneAfterCalibration : MonoBehaviour
    {
        public PupilLabs.CalibrationController calibrationController;

        [Tooltip("Specify scene by name (needs to be added to 'BuildSettings/Scenes In Build'")]
        public string sceneToLoad;

        void OnEnable()
        {
            calibrationController.OnCalibrationSucceeded += LoadScene;
        }

        void OnDisable()
        {
            calibrationController.OnCalibrationSucceeded -= LoadScene;
        }

        void LoadScene()
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
