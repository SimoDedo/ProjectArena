using UnityEngine;

namespace Utils
{
    public struct VisibilityTestResult
    {
        public float distance;
        public float angle;
        public bool isVisible;
    }

    public static class VisibilityUtils
    {
        /// <summary>
        /// Calculates distance, visibility and angle of deviation for the given target, starting from the given
        /// Transform and considering the given layerMask.
        /// </summary>
        // TODO Improve look by not considering only the center of the object, but also some random other points nearby
        public static VisibilityTestResult CanSeeTarget(Transform user, Transform target, int layerMask,
            float maxDistance)
        {
            var rtn = new VisibilityTestResult();
            var direction = target.position - user.position;
            rtn.angle = Vector3.Angle(user.forward, direction);

            if (Physics.Raycast(user.position, direction, out var hit, maxDistance, layerMask))
                if (hit.collider.transform == target)
                {
                    rtn.isVisible = true;
                    rtn.distance = hit.distance;
                }

            return rtn;
        }
    }
}