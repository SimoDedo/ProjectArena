using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entity.Component;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

// TODO If enemy cannot be seen, then choose blast weapon
// TODO Also equip the weapon if possible

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class ChooseWeapon : Action
    {
        [SerializeField] private SharedInt chosenGunIndex;
        private Transform enemyTransform;
        private AIEntity entity;
        private GunManager gunManager;
        private int gunsCount;
        private Transform t;

        public override void OnAwake()
        {
            entity = gameObject.GetComponent<AIEntity>();
            gunManager = entity.GunManager;
            gunsCount = gunManager.NumberOfGuns;
            enemyTransform = entity.GetEnemy().transform;
            t = transform;
        }

        public override TaskStatus OnUpdate()
        {
            var distance = (enemyTransform.position - t.position).magnitude;
            var chosenIndex = -1;
            var bestScore = float.MinValue;
            for (var i = 0; i < gunsCount; i++)
                if (gunManager.IsGunActive(i))
                {
                    var currentScore = gunManager.GetGunScore(i, distance);
                    if (currentScore > bestScore)
                    {
                        bestScore = currentScore;
                        chosenIndex = i;
                    }
                }

            if (chosenIndex == -1)
                // Find first active weapon
                chosenIndex = gunManager.FindLowestActiveGun();

            chosenGunIndex.Value = chosenIndex;
            return TaskStatus.Success;
        }
    }
}