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
        // TODO Improve look by not considering only the center of the object, but also some random other points nearby
        public static VisibilityTestResult CanSeeTarget(Transform user, Transform target, int ignoreLayers)
        {
            var rtn = new VisibilityTestResult();
            var direction = target.position - user.position;
            rtn.angle = Vector3.Angle(user.forward, direction);

            if (Physics.Raycast(user.position, direction, out var hit, float.PositiveInfinity, ignoreLayers))
            {
                if (hit.collider.transform == target)
                {
                    rtn.isVisible = true;
                    rtn.distance = hit.distance;
                }
            }

            return rtn;
        }

        public static VisibilityTestResult CanSeePosition(Transform user, Vector3 position, int ignoreLayers)
        {
            var rtn = new VisibilityTestResult();
            var direction = position - user.position;
            rtn.angle = Vector3.Angle(user.forward, direction);

            if (Physics.Linecast(user.position, position, out var hit, ignoreLayers))
            {
                rtn.isVisible = false;
                rtn.distance = hit.distance;
            }
            else
            {
                rtn.isVisible = true;
                rtn.distance = (position - user.position).magnitude;
            }

            return rtn;
        }
    }
}