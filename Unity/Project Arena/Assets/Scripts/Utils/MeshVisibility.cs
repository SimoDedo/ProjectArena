using UnityEngine;

namespace Utils
{
    public static class MeshVisibility
    {
        // Hides/shows the mesh.
        public static void SetMeshVisible(Transform father, bool isVisible)
        {
            // Avoid expensive operation in server build, since there is no rendering in any case
            #if !UNITY_SERVER || UNITY_EDITOR
            foreach (Transform children in father)
            {
                if (children.GetComponent<MeshRenderer>()) children.GetComponent<MeshRenderer>().enabled = isVisible;

                SetMeshVisible(children, isVisible);
            }
            #endif
        }
    }
}