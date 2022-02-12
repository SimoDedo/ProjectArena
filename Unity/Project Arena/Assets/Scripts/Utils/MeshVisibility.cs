using UnityEngine;

namespace Utils
{
    public static class MeshVisibility
    {
        // Hides/shows the mesh.
        public static void SetMeshVisible(Transform father, bool isVisible)
        {
            foreach (Transform children in father)
            {
                if (children.GetComponent<MeshRenderer>()) children.GetComponent<MeshRenderer>().enabled = isVisible;

                SetMeshVisible(children, isVisible);
            }
        }
    }
}