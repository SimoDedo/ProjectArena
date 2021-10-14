using System;
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
        /// <param name="sensibilityAdjustement">The speed change to use when moving
        /// <returns>The angle between the new look direction and the target</returns>
        /// </param>
        public float LookAtPoint(Vector3 target, float sensibilityAdjustement = 1f)
        {
            var headTransform = head.transform;
            var position = headTransform.position;
            var direction = (target - position).normalized;

            Debug.DrawLine(position, target, Color.green, 0, false);
            
            var rotation = Quaternion.LookRotation(direction);

            var angles = rotation.eulerAngles;
            angles = ConvertTo180Based(angles);

            var desiredHeadRotation = Quaternion.Euler(angles.x, 0, 0);
            var desiredBodyRotation = Quaternion.Euler(0, angles.y, 0);

            var currentHeadRotation = headTransform.localRotation;
            var currentBodyRotation = transform.localRotation;

            var currentHeadAngles = head.transform.rotation.eulerAngles;
            currentHeadAngles = ConvertTo180Based(currentHeadAngles);

            currentBodySpeed = CalculateNewSpeed(angles.y, currentHeadAngles.y, currentBodySpeed);
            currentHeadSpeed = CalculateNewSpeed(angles.x, currentHeadAngles.x, currentHeadSpeed);
            
            Debug.Log(gameObject.name + " new speeds: " + currentBodySpeed + ", " + currentHeadSpeed);
            
            var maxAngleBody = Math.Abs(currentBodySpeed * Time.deltaTime);
            var maxAnglehead = Math.Abs(currentHeadSpeed * Time.deltaTime);
            // var maxAngle = 2 * sensibility * inputPenalty * sensibilityAdjustement;

            var newHeadRotation = Quaternion.RotateTowards(currentHeadRotation, desiredHeadRotation, maxAnglehead);
            var newBodyRotation = Quaternion.RotateTowards(currentBodyRotation, desiredBodyRotation, maxAngleBody);

            headTransform.localRotation = newHeadRotation;
            transform.localRotation = newBodyRotation;

            return Vector3.Angle(headTransform.forward, direction);
        }

        private const float maxAcceleration = 2000f;
        private const float maxSpeed = 1000f;
        private float currentBodySpeed;
        private float currentHeadSpeed;

        private float CalculateNewSpeed(float target, float actual, float previousSpeed)
        {
            var angleToPerform = target - actual;
            var timeDeceleration = previousSpeed / maxAcceleration;
            var angleDuringDeceleration = previousSpeed * timeDeceleration +
                                          1f / 2f * maxAcceleration * timeDeceleration * timeDeceleration;

            if (Math.Abs(angleDuringDeceleration) < Math.Abs(angleToPerform))
            {
                var newSpeed = previousSpeed + Mathf.Sign(angleToPerform) * maxAcceleration * Time.deltaTime;
                if (Mathf.Abs(newSpeed) > maxSpeed)
                    newSpeed = maxSpeed * Mathf.Sign(newSpeed);
                return newSpeed;
            }

            return previousSpeed + Mathf.Sign(-previousSpeed) * maxAcceleration * Time.deltaTime;
        }

        private static Vector3 ConvertTo180Based(Vector3 angles)
        {
            if (angles.x > 180f)
                angles.x -= 360f;
            if (angles.y > 180f)
                angles.y -= 360f;
            if (angles.z > 180f)
                angles.z -= 360f;
            return angles;
        }

        public void SetInputPenalty(float inputPenalty)
        {
            this.inputPenalty = inputPenalty;
        }

        public Vector3 GetLookDirection()
        {
            return head.transform.forward;
        }

        private void LateUpdate()
        {
        }
    }
}