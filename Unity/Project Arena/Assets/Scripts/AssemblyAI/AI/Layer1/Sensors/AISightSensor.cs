using UnityEngine;
using Utils;

namespace AssemblyAI.AI.Layer1.Sensors
{
    public class AISightSensor
    {
        private readonly GameObject head;
        private readonly float maxSightRange;
        private readonly float fov;
        
        public AISightSensor(GameObject head, float maxSightRange, float fov)
        {
            this.head = head;
            this.maxSightRange = maxSightRange;
            this.fov = fov;
        }
        
        public void Prepare() { /* Nothing to do */ }

        public float GetAngleFromLookDirection(Vector3 direction)
        {
            return Vector3.Angle(head.transform.forward, direction);
        }

        public bool CanSeeObject(Transform obj, int ignoreLayers)
        {
            var visibility = VisibilityUtils.CanSeeTarget(head.transform, obj, ignoreLayers);
            if (!visibility.isVisible) return false;
            if (visibility.distance > maxSightRange) return false;
            if (visibility.angle > fov) return false;
            return true;
        }

        public bool CanSeePosition(Vector3 position, int ignoreLayers = Physics.DefaultRaycastLayers)
        {
            var visibility = VisibilityUtils.CanSeePosition(head.transform, position, ignoreLayers);
            if (!visibility.isVisible) return false;
            if (visibility.distance > maxSightRange) return false;
            if (visibility.angle > fov) return false;
            return true;
        }

        public float GetObstacleDistanceInDirection(Vector3 position, Vector3 direction, int ignoreLayers)
        {
            return !Physics.Raycast(position, direction, out var hit, maxSightRange, ignoreLayers)
                ? maxSightRange
                : hit.distance;
        }

        public Vector3 GetLookDirection()
        {
            return head.transform.forward;
        }
    }
}