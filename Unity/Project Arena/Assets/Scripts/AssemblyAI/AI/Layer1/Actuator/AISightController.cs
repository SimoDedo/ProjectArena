using UnityEngine;

namespace Entities.AI.Controller
{
    public class AISightController : MonoBehaviour
    {
        private GameObject head;
        private float sensibility;
        private float inputPenalty;

        public void SetParameters(GameObject head, float sensibility, float inputPenalty)
        {
            this.head = head;
            this.sensibility = sensibility;
            this.inputPenalty = inputPenalty;
        }
        
        /// <summary>
        /// This functions rotates head and body of the entity in order to look at the provided target.
        /// The rotation is subjected to the limitation given by the sensibility of the camera, so it might
        /// not be possible to immediately look at the target
        /// </summary>
        /// <param name="target">The point to look
        /// <returns>The angle between the new look direction and the target</returns>
        /// </param>
        public float LookAtPoint(Vector3 target)
        {
            var headTransform = head.transform;
            var position = headTransform.position;
            var direction = (target - position).normalized;

            var rotation = Quaternion.LookRotation(direction);

            var angles = rotation.eulerAngles;

            var desiredHeadRotation = Quaternion.Euler(angles.x, 0, 0);
            var desiredBodyRotation = Quaternion.Euler(0, angles.y, 0);

            var currentHeadRotation = headTransform.localRotation;
            var currentBodyRotation = transform.localRotation;

            var maxAngle = 2 * sensibility * inputPenalty;

            var newHeadRotation = Quaternion.RotateTowards(currentHeadRotation, desiredHeadRotation, maxAngle);
            var newBodyRotation = Quaternion.RotateTowards(currentBodyRotation, desiredBodyRotation, maxAngle);

            headTransform.localRotation = newHeadRotation;
            transform.localRotation = newBodyRotation;

            return Vector3.Angle(headTransform.forward, direction);
        }

        public void SetInputPenalty(float inputPenalty)
        {
            this.inputPenalty = inputPenalty;
        }
    }
}