using UnityEngine;
using Utils;

namespace Entities.AI.Layer1.Sensors
{
    public class AISightSensor : MonoBehaviour
    { 
        private GameObject head;
        private float maxSightRange;
        private float fov;

        public void SetParameters(GameObject head, float maxSightRange, float fov)
        {
            this.head = head;
            this.maxSightRange = maxSightRange;
            this.fov = fov;
        }
        
        public float GetAngleFromLookDirection(Vector3 direction)
        {
            return Vector3.Angle(head.transform.forward, direction);
        }
        
        public bool CanSeeObject(Transform obj, int ignoreLayers)
        {
            var visibility = VisibilityUtils.CanSeeTarget(transform, obj, ignoreLayers);
            if (!visibility.isVisible) return false;
            if (visibility.distance > maxSightRange) return false;
            if (visibility.angle > fov) return false;
            return true;
        }

        public bool CanSeePosition(Vector3 position, int ignoreLayers)
        {
            var visibility = VisibilityUtils.CanSeePosition(transform, position, ignoreLayers);
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
    }
}