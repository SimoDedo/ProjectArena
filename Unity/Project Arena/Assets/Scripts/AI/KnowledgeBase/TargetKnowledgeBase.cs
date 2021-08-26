using System;
using UnityEngine;
using Utils;

namespace AI.KnowledgeBase
{
    public class TargetKnowledgeBase : MonoBehaviour
    {
        private enum DetectionType
        {
            SEEN_FOR_N_CONSECUTIVE_FRAMES,
            SEEN_FOR_N_FRAMES_IN_LAST_M_FRAMES
        }
        [SerializeField] private GameObject target;
        
        [Header("Entity vision properties")]
        [SerializeField] private float fov;
        [SerializeField] private float maxDistance;
        [SerializeField] private DetectionType detectionType;

        [Header("Detection type: seen consecutively in last N frames")]
        [SerializeField] private int consecutiveFramesBeforeDetection;

        [Header("Detection type: seen non-consecutively N times in last M frames")]
        [SerializeField] private int nonConsecutiveFramesBeforeDetection;
        [SerializeField] private int numRememberedFrames;

        [Header("Loss of sight parameters")]
        [SerializeField] private int consecutiveFramesBeforeLossOfSight;

        private readonly CircularQueue<VisibilityTestResult> results = new CircularQueue<VisibilityTestResult>(60);

        private void Update()
        {
            results.Put(VisibilityUtils.CanSeeTarget(transform, target.transform, Physics.IgnoreRaycastLayer));
        }

        public bool IsTargetVisible()
        {
            return results.GetElem(0).isVisible;
        }

        private bool CanSeeTarget(VisibilityTestResult results)
        {
            return results.isVisible && results.distance < maxDistance && results.angle < fov;
        }
        
        public bool CanReactToTarget()
        {
            switch (detectionType)
            {
                case DetectionType.SEEN_FOR_N_CONSECUTIVE_FRAMES:
                    if (results.NumElems() < consecutiveFramesBeforeDetection) return false;
                    for (var i = 0; i < consecutiveFramesBeforeDetection; i++)
                    {
                        var detection = results.GetElem(i);
                        if (!CanSeeTarget(detection)) return false;
                    }
                    return true;
                case DetectionType.SEEN_FOR_N_FRAMES_IN_LAST_M_FRAMES:
                    var maxFrames = Math.Min(numRememberedFrames, results.NumElems());
                    if (maxFrames < nonConsecutiveFramesBeforeDetection) return false;
                    var numFramesSeen = 0;
                    for (var i = 0; i < maxFrames; i++)
                    {
                        var detection = results.GetElem(i);
                        if (CanSeeTarget(detection)) numFramesSeen++;
                    }
                    return numFramesSeen >= nonConsecutiveFramesBeforeDetection; 
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool HasLostTarget()
        {
            if (results.NumElems() < consecutiveFramesBeforeLossOfSight) return false;
            for (var i = 0; i < consecutiveFramesBeforeLossOfSight; i++)
            {
                var detection = results.GetElem(i);
                if (CanSeeTarget(detection)) return false;
            }
            return true;
        }
    }
}