using System;
using System.Linq;
using AI.Behaviours.Variables;
using AI.Layers.Actuators;
using Bonsai;
using Bonsai.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Looks at the specified point.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class LookAtPoint : Task
    {
        public string pathBlackboardKey;
        private NavMeshPath pathInfo;
        private Vector3 lookPoint;
        private SightController sightController;

        public override void OnStart()
        {
            sightController = Actor.GetComponent<AIEntity>().SightController;
        }

        public override void OnEnter()
        {
            pathInfo = Blackboard.Get<NavMeshPath>(pathBlackboardKey);
            lookPoint = pathInfo.corners.Last();
        }


        public override Status Run()
        {
            sightController.LookAtPoint(lookPoint);
            return Status.Running;
        }
    }
}