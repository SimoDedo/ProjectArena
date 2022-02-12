using UnityEngine;

namespace AI.AI.Layer1
{
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

        public void Prepare()
        {
            previousPosition = transform.position;
        }


        public void MoveToPosition(Vector3 position)
        {
            // TODO Control movement for this frame, prevent moving too fast
            previousPosition = transform.position;
            transform.position = position;
        }

        public Vector3 GetVelocity()
        {
            return (transform.position - previousPosition) / Time.deltaTime;
        }
    }
}