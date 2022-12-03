using System;
using UnityEngine;

namespace AI.Layers.Actuators
{
    /// <summary>
    /// This component deals with rotating the view of the entity.
    /// </summary>
    public class SightController
    {
        private const float LOOK_STRAIGHT_DISTANCE = 10f;
        private readonly Transform bodyTransform;
        private readonly GameObject head;
        private readonly Transform headTransform;
        private readonly float maxAcceleration;
        private readonly float maxSpeed;

        private float CurrentMaxAcceleration => maxAcceleration * inputPenalty;
        private float CurrentMaxSpeed => maxSpeed * inputPenalty;

        private float currentBodySpeed;
        private float currentHeadSpeed;
        private float inputPenalty = 1f;

        public SightController(AIEntity entity, GameObject head, float maxSpeed, float maxAcceleration)
        {
            bodyTransform = entity.transform;
            this.head = head;
            headTransform = head.transform;
            this.maxSpeed = maxSpeed;
            this.maxAcceleration = maxAcceleration;
        }

        /// <summary>
        /// This functions rotates head and body of the entity in order to look at the provided target.
        /// The rotation is subjected to the limitation given by the sensibility of the camera, so it might
        /// not be possible to immediately look at the target
        /// </summary>
        /// <param name="target">The point to look.</param>
        /// <param name="forceLookStraightWhenClose"> HOT FIX: prevents looking up or down when getting close to the
        ///   point we want to look.</param>
        /// <returns>The angle between the new look direction and the target.</returns>
        public float LookAtPoint(Vector3 target, bool forceLookStraightWhenClose = true)
        {
            var position = headTransform.position;
            if (forceLookStraightWhenClose && (target - position).magnitude < LOOK_STRAIGHT_DISTANCE)
                target.y = position.y;

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

            var maxAngleBody = Math.Abs(currentBodySpeed * Time.deltaTime);
            var maxAngleHead = Math.Abs(currentHeadSpeed * Time.deltaTime);

            var newHeadRotation = Quaternion.RotateTowards(currentHeadRotation, desiredHeadRotation, maxAngleHead);
            var newBodyRotation = Quaternion.RotateTowards(currentBodyRotation, desiredBodyRotation, maxAngleBody);

            headTransform.localRotation = newHeadRotation;
            bodyTransform.localRotation = newBodyRotation;

            return Vector3.Angle(headTransform.forward, direction);
        }

        // Calculate the next angular velocity given the target angle, the actual one and the actual speed. 
        private float CalculateNewSpeed(float target, float actual, float previousSpeed)
        {
            var angleToPerform = target - actual;
            var timeDeceleration = previousSpeed / CurrentMaxAcceleration;
            var angleDuringDeceleration = previousSpeed * timeDeceleration +
                                          1f / 2f * CurrentMaxAcceleration * timeDeceleration * timeDeceleration;

            if (Math.Abs(angleDuringDeceleration) < Math.Abs(angleToPerform))
            {
                var newSpeed = previousSpeed + Mathf.Sign(angleToPerform) * CurrentMaxAcceleration * Time.deltaTime;
                if (Mathf.Abs(newSpeed) > CurrentMaxSpeed) newSpeed = CurrentMaxSpeed * Mathf.Sign(newSpeed);
                return newSpeed;
            }

            return previousSpeed + Mathf.Sign(-previousSpeed) * CurrentMaxAcceleration * Time.deltaTime;
        }

        // Converts an angle from (0,360) to (-180, 180).
        private static Vector3 ConvertTo180Based(Vector3 angles)
        {
            if (angles.x > 180f) angles.x -= 360f;
            if (angles.y > 180f) angles.y -= 360f;
            if (angles.z > 180f) angles.z -= 360f;
            return angles;
        }


        // Sets slowdown of view
        public void SetInputPenalty(float inputPenalty)
        {
            this.inputPenalty = inputPenalty;
        }

        // Returns the head position in global space.
        public Vector3 GetHeadPosition()
        {
            return headTransform.position;
        }

        // Returns the current look direction of the head.
        public Vector3 GetHeadForward()
        {
            return headTransform.forward;
        }
    }
}