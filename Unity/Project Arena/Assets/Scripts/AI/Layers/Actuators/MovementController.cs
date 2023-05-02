using UnityEngine;

namespace AI.Layers.Actuators
{
    /// <summary>
    /// This component deals with moving the entity around.
    /// </summary>
    public class MovementController
    {
        private readonly Transform transform;
        private Vector3 previousPosition;
        
        public MovementController(AIEntity entity)
        {
            transform = entity.transform;
        }

        // Prepares the component
        public void Prepare()
        {
            previousPosition = transform.position;
        }

        /// <summary>
        /// Moves the entity to the position specified.
        /// </summary>
        public void MoveToPosition(Vector3 position)
        {
            previousPosition = transform.position;
            transform.position = position;
        }

        /// <summary>
        /// Returns the current velocity of the entity.
        /// </summary>
        public Vector3 GetVelocity()
        {
            return (transform.position - previousPosition) / Time.deltaTime;
        }
    }
}