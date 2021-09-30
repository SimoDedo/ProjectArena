using System;
using System.Collections.Generic;
using System.Linq;
using AI.Guns;
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
        private List<Gun> guns;
        private List<GunScorer> gunScorers;
        private Transform enemyTransform;
        private Transform t;
        public override void OnAwake()
        {
            entity = gameObject.GetComponent<AIEntity>();
            guns = entity.GetGuns();
            gunScorers = guns.Select(it => it.gameObject.GetComponent<GunScorer>()).ToList();
            enemyTransform = entity.GetEnemy().transform;
            t = transform;
        }

        public override TaskStatus OnUpdate()
        {
            var distance = (enemyTransform.position - t.position).magnitude;
            var chosenIndex = -1;
            var bestScore = float.MinValue;
            for (var i = 0; i < guns.Count; i++)
            {
                if (guns[i].isActiveAndEnabled)
                {
                    var currentScore = gunScorers[i].GetGunScore(distance);
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
                chosenIndex = guns.FindIndex(g => g.isActiveAndEnabled);
            }

            chosenGunIndex.Value = chosenIndex;
            return TaskStatus.Success;
        }
    }
}