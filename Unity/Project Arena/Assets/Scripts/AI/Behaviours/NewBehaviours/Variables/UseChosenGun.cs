using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.NewBehaviours.Variables
{
    [Serializable]
    public class UseChosenGun: Action
    {
        [SerializeField] private SharedGameObject chosenGun;
        private AIEntity entity;
        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Failure;
        }
    }
}