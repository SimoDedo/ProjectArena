using System;
using System.Collections.Generic;
using System.Linq;
using AI.Guns;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class ChooseWeapon: Action
    {
        private AIEntity entity;
        [SerializeField] private SharedInt chosenGunIndex;
        private GunManager gunManager;
        private int gunsCount;
        private Transform enemyTransform;
        private Transform t;
        public override void OnAwake()
        {
            entity = gameObject.GetComponent<AIEntity>();
            gunManager = gameObject.GetComponent<GunManager>();
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
            {
                if (gunManager.IsGunActive(i))
                {
                    var currentScore = gunManager.GetGunScore(i, distance);
                    if (currentScore > bestScore)
                    {
                        bestScore = currentScore;
                        chosenIndex = i;
                    }
                }
            }

            if (chosenIndex == -1)
            {
                // Find first active weapon
                chosenIndex = gunManager.FindLowestActiveGun();
            }

            chosenGunIndex.Value = chosenIndex;
            return TaskStatus.Success;
        }
    }
}