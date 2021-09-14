using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.NewBehaviours.Variables
{
    [Serializable]
    public class UseChosenGun: Action
    {
        [SerializeField] private SharedInt chosenGunIndex;
        private AIEntity entity;
        private GameObject enemy;
        private PositionTracker enemyPositionTracker;
        private AISightController sightController;
        private Gun gun;
        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
        }

        public override void OnStart()
        {
            // TODO check this is called at the right time
            entity = gameObject.GetComponent<AIEntity>();
            sightController = gameObject.GetComponent<AISightController>();
            enemy = entity.GetEnemy();
            enemyPositionTracker = enemy.GetComponent<PositionTracker>();
            gun = entity.EquipGun(0);
            gun.Wield();
        }

        public override TaskStatus OnUpdate()
        {
            var reflexDelay = Math.Max(0, Random.Range(-3, 10)); // TODO Gaussian
            var position = enemyPositionTracker.GetPositionFromIndex(reflexDelay);
            var angle = sightController.LookAtPoint(position);
            if (angle < 40)
            {
                if (gun.CanShoot())
                    gun.Shoot();
            }
            return TaskStatus.Running;
        }
    }
}