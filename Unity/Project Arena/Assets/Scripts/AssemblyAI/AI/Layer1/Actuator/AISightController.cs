using System;
using UnityEngine;

namespace AssemblyAI.AI.Layer1.Actuator
{
    public class AISightController
    {
        private readonly Transform bodyTransform;
        private readonly GameObject head;
        private readonly float maxSpeed;
        private readonly float maxAcceleration;
        private float inputPenalty = 1f;
        private float currentBodySpeed;
        private float currentHeadSpeed;

        public AISightController(
            AIEntity entity,
            GameObject head,
            float maxSpeed,
            float maxAcceleration
        )
        {
            bodyTransform = entity.transform;
            this.head = head;
            this.maxSpeed = maxSpeed;
            this.maxAcceleration = maxAcceleration;
        }

        public void Prepare() { /* Nothing to do */ }

        /// <summary>
        /// This functions rotates head and body of the entity in order to look at the provided target.
        /// The rotation is subjected to the limitation given by the sensibility of the camera, so it might
        /// not be possible to immediately look at the target
        /// </summary>
        /// <param name="target">The point to look</param>
        /// <param name="sensibilityAdjustment">The speed change to use when moving</param>ù
        /// <param name="forceLookStraightWhenClose"></param>
        /// <returns>The angle between the new look direction and the target</returns>
        /// 
        public float LookAtPoint(
            Vector3 target,
            float sensibilityAdjustment = 1f,
            bool forceLookStraightWhenClose = true
        )
        {
            var headTransform = head.transform;
            var position = headTransform.position;

            var distance = (target - position).magnitude;

            if (distance < 10 && forceLookStraightWhenClose) target.y = position.y;

            var direction = (target - position).normalized;
            var rotation = Quaternion.LookRotation(direction);

            var angles = rotation.eulerAngles;
            angles = ConvertTo180Based(angles);

            var desiredHeadRotation = Quaternion.Euler(angles.x, 0, 0);
            var desiredBodyRotation = Quaternion.Euler(0, angles.y, 0);

            var currentHeadRotation = headTransform.localRotation;
            var currentBodyRotation = bodyTransform.localRotation;

            var currentHeadAngles = head.transform.rotation.eulerAngles;
            currentHeadAngles = ConvertTo180Based(currentHeadAngles);

            currentBodySpeed = CalculateNewSpeed(angles.y, currentHeadAngles.y, currentBodySpeed);
            currentHeadSpeed = CalculateNewSpeed(angles.x, currentHeadAngles.x, currentHeadSpeed);

            // Debug.Log(gameObject.name + " new speeds: " + currentBodySpeed + ", " + currentHeadSpeed);

            var maxAngleBody = Math.Abs(currentBodySpeed * Time.deltaTime);
            var maxAngleHead = Math.Abs(currentHeadSpeed * Time.deltaTime);

            var newHeadRotation = Quaternion.RotateTowards(currentHeadRotation, desiredHeadRotation, maxAngleHead);
            var newBodyRotation = Quaternion.RotateTowards(currentBodyRotation, desiredBodyRotation, maxAngleBody);

            headTransform.localRotation = newHeadRotation;
            bodyTransform.localRotation = newBodyRotation;

            // Debug.DrawRay(head.transform.position, headTransform.forward, Color.green);
            // Debug.DrawLine(head.transform.position, target, Color.blue);
            return Vector3.Angle(headTransform.forward, direction);
        }
        
        private float CalculateNewSpeed(float target, float actual, float previousSpeed)
        {
            var clampedAcceleration = maxAcceleration * inputPenalty;
            var angleToPerform = target - actual;
            var timeDeceleration = previousSpeed / clampedAcceleration;
            var angleDuringDeceleration = previousSpeed * timeDeceleration +
                1f / 2f * clampedAcceleration * timeDeceleration * timeDeceleration;

            if (Math.Abs(angleDuringDeceleration) < Math.Abs(angleToPerform))
            {
                var newSpeed = previousSpeed + Mathf.Sign(angleToPerform) * clampedAcceleration * Time.deltaTime;
                if (Mathf.Abs(newSpeed) > maxSpeed) newSpeed = maxSpeed * Mathf.Sign(newSpeed);
                return newSpeed;
            }

            return previousSpeed + Mathf.Sign(-previousSpeed) * clampedAcceleration * Time.deltaTime;
        }

        private static Vector3 ConvertTo180Based(Vector3 angles)
        {
            if (angles.x > 180f) angles.x -= 360f;
            if (angles.y > 180f) angles.y -= 360f;
            if (angles.z > 180f) angles.z -= 360f;
            return angles;
        }

        public void SetInputPenalty(float inputPenalty)
        {
            this.inputPenalty = inputPenalty;
        }
    }
}