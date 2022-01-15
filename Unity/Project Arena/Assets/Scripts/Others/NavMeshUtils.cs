using UnityEngine;
using UnityEngine.AI;

namespace Others
{
    public static class NavMeshUtils
    {
        public static int GetAgentIDFromName(string name)
        {
            var count = NavMesh.GetSettingsCount();
            for (var i = 0; i < count; i++)
            {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                if (NavMesh.GetSettingsNameFromID(id) == name) return id;
            }

            return -1;
        }
        
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
            if (path.status == NavMeshPathStatus.PathComplete && path.corners.Length == 0)
            {
                Debug.LogError("What is this?");
            }
            return path.status == NavMeshPathStatus.PathComplete;
        }
        
        
    }
}