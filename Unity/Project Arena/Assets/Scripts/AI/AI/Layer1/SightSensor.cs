using UnityEngine;
using Utils;

namespace AI.AI.Layer1
{
    /// <summary>
    /// This component is used to query whether any object is in view or not.
    /// </summary>
    public class SightSensor
    {
        private readonly float fov;
        private readonly GameObject head;
        private readonly float maxSightRange;

        public SightSensor(GameObject head, float maxSightRange, float fov)
        {
            this.head = head;
            this.maxSightRange = maxSightRange;
            this.fov = fov;
        }

        /// <summary>
        /// Returns whether the position specified by that transform can be seen or not.
        /// Can additionally specify a mask of layers to ignore.
        /// </summary>
        public bool CanSeeObject(Transform transform, int layerMask = Physics.DefaultRaycastLayers)
        {
            var visibility = VisibilityUtils.CanSeeTarget(head.transform, transform, layerMask);
            if (!visibility.isVisible) return false;
            if (visibility.distance > maxSightRange) return false;
            if (visibility.angle > fov) return false;
            return true;
        }
    }
}