using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.NewBehaviours
{
    [Serializable]
    public class ChooseWeapon: Action
    {
        private AIEntity entity;
        [SerializeField] private SharedInt chosenGunIndex;
        public override void OnAwake()
        {
            entity = gameObject.GetComponent<AIEntity>();
        }

        public override TaskStatus OnUpdate()
        {
            chosenGunIndex.Value = entity.GetBestGunIndex();
            return TaskStatus.Success;
        }
    }
}