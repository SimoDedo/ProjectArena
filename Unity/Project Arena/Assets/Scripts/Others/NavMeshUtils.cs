using UnityEngine;
using UnityEngine.AI;

namespace Others
{
    public static class NavMeshUtils
    {
        // Used to get length without allocating Vector3 every time
        private static readonly Vector3[] corners = new Vector3[1000];

        public static float Length(this NavMeshPath path)
        {
            var totalLength = 0f;
            
            var cornersLength = path.GetCornersNonAlloc(corners);
            for (var i = 1; i < cornersLength; i++)
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



