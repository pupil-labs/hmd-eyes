using System.Collections.Generic;
using UnityEngine;

//TODO ringbuffer

namespace PupilLabs.Demos
{
    public class GazeHistory : MonoBehaviour
    {       
        public Transform gazeOrigin;
        public GazeController gazeController;
        public float sphereCastRadius;
        public bool record;
        public float confidenceThreshold = 0.8f;
        public int maxSamples = 1000;
        public LineRenderer line;
        public Transform linedump;
        [Header("Settings")]
        public float maxWidth;
        public float minLength;

        public class ProjectedGaze
        {
            public Vector3 position;
            public GameObject target;
        }

        private List<ProjectedGaze> gazeHistory = new List<ProjectedGaze>();
        private int processedIndex = 0;
        private GameObject currentTarget = null;
        private LineRenderer currentLine = null;

        void Awake()
        {
            gazeController.OnReceive3dGaze += HandleGaze; 
        }

        void Update()
        {
            // UpdateLineVis();
            UpdateSeperateLines();
        }

        void HandleGaze(GazeData gazeData)
        {
            if (!record)
            {
                return;
            }

            if (gazeData.Confidence < confidenceThreshold)
            {
                return;
            }

            var hit = Project(gazeData.GazeDirection);
            if (hit != null)
            {
                gazeHistory.Add(hit);
            }
        }

        ProjectedGaze Project(Vector3 gazeDirection)
        {
            Vector3 origin = gazeOrigin.position;
            Vector3 direction = gazeOrigin.TransformDirection(gazeDirection);

            if (Physics.SphereCast(origin, sphereCastRadius, direction, out RaycastHit hit, Mathf.Infinity))
            {
                return new ProjectedGaze
                {
                    position = hit.point,
                    target = hit.transform.gameObject
                };
            }
            else
            {
                return null;
            }
        }

        void UpdateLineVis()
        {
            int sampleCount = gazeHistory.Count - processedIndex - 1;
            if(sampleCount <= 0)
            {
                return;
            }

            var samples = gazeHistory.GetRange(processedIndex,sampleCount);

            int lineIdx = line.positionCount;
            line.positionCount += samples.Count;
            foreach (var sample in samples)
            {   
                line.SetPosition(lineIdx,sample.position);
                lineIdx++;
            }

            processedIndex += sampleCount;

        }

        void UpdateSeperateLines()
        {
            int sampleCount = gazeHistory.Count - processedIndex - 1;
            if(sampleCount <= 0)
            {
                return;
            }

            var samples = gazeHistory.GetRange(processedIndex,sampleCount);

            foreach (var sample in samples)
            {   
                if (sample.target != currentTarget)
                {
                    currentLine = Instantiate(line);
                    currentLine.transform.parent = linedump;
                    currentLine.positionCount = 0;
                    currentTarget = sample.target;
                }

                currentLine.positionCount++;
                currentLine.SetPosition(currentLine.positionCount-1,sample.position);
                float width = Mathf.Lerp(0,maxWidth,(float)currentLine.positionCount/minLength);
                currentLine.startWidth = width;
                currentLine.endWidth = width;
            }

            processedIndex += sampleCount;
        }
    }
}
