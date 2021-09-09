using UnityEngine;
using UnityEngine.AI;

namespace Utils
{
    public static class NavigationUtils
    {
        public static float GetPathLenght(NavMeshPath path)
        {
            var totalLength = 0f;
            var corners = path.corners;
            for (var i = 1; i < corners.Length; i++)
                totalLength += (corners[i - 1] - corners[i]).sqrMagnitude;
            return Mathf.Sqrt(totalLength);
        }
    }
}