using UnityEngine;

namespace AssemblyAI.AI.Layer1.Actuator
{
    public class AIMovementController
    {
        private float speed;

        private readonly Transform transform;
        private Vector3 previousPosition;

        public AIMovementController(AIEntity entity, float speed)
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