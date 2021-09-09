using System;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.NewBehaviours
{
    [Serializable]
    public class LookAround : Action
    {
        private AIMovementController movementController;
        private AISightController sightController;
        private float curiosity;
        private bool mustFindAngle;
        private float angleX;

        public override void OnAwake()
        {
            sightController = GetComponent<AISightController>();
            movementController = GetComponent<AIMovementController>();
            curiosity = GetComponent<AIEntity>().GetCuriosity();
        }

        public override void OnStart()
        {
            mustFindAngle = true;
        }

        public override TaskStatus OnUpdate()
        {
            var realForward = movementController.GetVelocity().normalized;
            if (mustFindAngle)
            {
                // TODO Agent is not moving, have it slowly look around?
                if (realForward == Vector3.zero)
                {
                    realForward = transform.forward;
                    angleX = 1;
                }
                // TODO Find better distribution. I should be looking forward most of the time.
                else if (curiosity < 0.3 || Random.value > 0.25)
                    angleX = 0;
                else
                {
                    if (curiosity < 0.8)
                        angleX = Random.Range(-1, 2) * 45f;
                    else
                        angleX = Random.Range(-3, 4) * 45f;
                }
            }

            var newDirection = Quaternion.AngleAxis(angleX, transform.up) * realForward;
            // In order to not look down, I should be looking at a point far in the horizon, hence the x100
            var lookPoint = transform.position + (newDirection * 100);
            sightController.LookAtPoint(lookPoint);
            mustFindAngle = false;
            return TaskStatus.Running;
        }
    }
}