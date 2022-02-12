using UnityEngine;

namespace AI.AI.Layer1
{
    /// <summary>
    /// This component deals with moving the entity around.
    /// </summary>
    public class MovementController
    {
        private readonly Transform transform;
        private Vector3 previousPosition;
        private float speed;

        public MovementController(AIEntity entity, float speed)
        {
            transform = entity.transform;
            this.speed = speed;
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
            // TODO Control movement for this frame, prevent moving too fast
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