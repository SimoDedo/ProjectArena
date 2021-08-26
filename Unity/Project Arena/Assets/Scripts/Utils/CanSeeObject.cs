using BehaviorDesigner.Runtime.Tasks.Unity.UnityRenderer;
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
        public static VisibilityTestResult CanSeeTarget(Transform user, Transform target, int mask)
        {
            var rtn = new VisibilityTestResult();
            var direction = target.position - user.position;
            rtn.angle = Vector3.Angle(user.forward, direction);

            if (Physics.Raycast(user.position, direction, out var hit, mask))
            {
                if (hit.collider.transform == target)
                {
                    rtn.isVisible = true;
                    rtn.distance = hit.distance;
                }
            }

            return rtn;
        }
    }
}