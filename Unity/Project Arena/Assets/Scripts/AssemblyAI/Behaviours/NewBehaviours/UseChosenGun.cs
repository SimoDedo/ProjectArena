using System;
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.NewBehaviours.Variables
{
    [Serializable]
    public class UseChosenGun : Action
    {
        [SerializeField] private SharedInt chosenGunIndex;
        private AIEntity entity;
        private GameObject enemy;
        private PositionTracker enemyPositionTracker;
        private AISightController sightController;
        private Gun gun;

        private NormalDistribution distribution;
        private float nextUpdateDelayTime;
        private float reflexDelay;
        private const float UPDATE_DELAY = 0.1f;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            // TODO Calculate mean and stdDev based on bot ability
            var skill = entity.GetAimingSkill();
            var mean = 0.4f - 0.6f * skill; // [-0.2, 0.4]
            var stdDev = 0.3f - 0.1f * skill; // [0.2, 0.3]
            distribution = new NormalDistribution(mean, stdDev);
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
            nextUpdateDelayTime = Time.time;
        }

        public override TaskStatus OnUpdate()
        {
            if (Time.time > nextUpdateDelayTime)
            {
                nextUpdateDelayTime = Time.time + UPDATE_DELAY;
                reflexDelay = (float) distribution.Generate();
            }
            var position = enemyPositionTracker.GetPositionFromDelay(reflexDelay);
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

public static class RandomUtils
{
    public static float GetRandomNormal(float mean, float stdDev)
    {
        var u1 = 1.0f - Random.value;
        var u2 = 1.0f - Random.value;
        var randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                            Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
        return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
    }
}