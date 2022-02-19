using UnityEngine;
using UnityEngine.AI;

namespace Others
{
    public static class NavMeshUtils
    {
        public static float Length(this NavMeshPath path)
        {
            var totalLength = 0f;
            var corners = path.corners;
            for (var i = 1; i < corners.Length; i++)
                totalLength += (corners[i - 1] - corners[i]).sqrMagnitude;
            return Mathf.Sqrt(totalLength);
        }

        public static bool IsComplete(this NavMeshPath path)
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        
        public static bool IsValid(this NavMeshPath path)
        {
            return path.status != NavMeshPathStatus.PathInvalid;
        }
    }
}