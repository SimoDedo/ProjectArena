using UnityEngine;
using UnityEngine.AI;

namespace Others
{
    public class NavMeshUtils
    {
        public static int GetAgentIDFromName(string name)
        {
            var count = NavMesh.GetSettingsCount();
            for (var i = 0; i < count; i++)
            {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                if (NavMesh.GetSettingsNameFromID(id) == name)
                    return id;
            }
            return -1;
        }

        public static float GetPathLength(NavMeshPath path)
        {
            var totalLength = 0f;
            for (var i = 1; i < path.corners.Length; i++)
            {
                totalLength += (path.corners[i] - path.corners[i - 1]).magnitude;
            }

            return totalLength;
        }
        
        public static void DrawPath(NavMeshPath path, Color color)
        {
            for (var i = 1; i < path.corners.Length; i++)
            {
                Debug.DrawLine(path.corners[i-1], path.corners[i], color);
            }
        }
    }
}