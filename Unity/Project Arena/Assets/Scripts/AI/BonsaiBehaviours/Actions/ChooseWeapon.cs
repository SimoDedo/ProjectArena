using AI.Layers.Actuators;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.Core;
using Entity.Component;
using UnityEngine;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Selects the best weapon to use during a fight.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class ChooseWeapon : Task
    {
        private Entity.Entity enemy;
        private AIEntity entity;
        private GunManager gunManager;
        private SightController sightController;
        private TargetKnowledgeBase _targetKnowledgeBase;

        public override void OnStart()
        {
            entity = Actor.gameObject.GetComponent<AIEntity>();
            enemy = entity.GetEnemy();
            sightController = entity.SightController;
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            gunManager = entity.GunManager;
        }

        public override Status Run()
        {
            var headPos = sightController.GetHeadPosition();
            var enemyPos = enemy.transform.position;
            var enemyDistance = (headPos - enemyPos).magnitude;

            // Choose blast weapon if we cannot see the enemy right now
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var mustChooseBlastWeapon = _targetKnowledgeBase.LastTimeDetected != Time.time;

            var selectedGun = -1;
            var bestGunScore = 0f;
            for (var i = 0; i < gunManager.NumberOfGuns; i++)
            {
                if (!gunManager.IsGunActive(i)) continue;
                if (mustChooseBlastWeapon && !gunManager.IsGunBlastWeapon(i)) continue;

                var gunScore = gunManager.GetGunScore(i, enemyDistance);
                if (gunScore > bestGunScore)
                {
                    bestGunScore = gunScore;
                    selectedGun = i;
                }
            }

            if (selectedGun != -1)
                // We found a good weapon, switch!
                gunManager.TryEquipGun(selectedGun);

            return Status.Running;
        }
    }
}