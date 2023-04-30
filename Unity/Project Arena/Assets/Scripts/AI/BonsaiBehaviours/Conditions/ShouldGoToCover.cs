using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Bonsai;
using Bonsai.CustomNodes;
using Entity.Component;
using Random = UnityEngine.Random;

namespace AI.BonsaiBehaviours.Conditions
{
    /// <summary>
    /// Return Success if we are better of searching for cover, Failure otherwise.
    /// </summary>
    [BonsaiNode("Conditional/")]
    public class ShouldGoToCover : TimedEvaluationConditionalAbort
    {
        private const float TIMEOUT = 1.5f;
        private const float CHARGER_PERCENTAGE = 0.4f;

        public ShouldGoToCover() : base(TIMEOUT)
        {
            
        }
        
        // If true, we must now consider the random failure probability until we find out that we no longer need to
        // cover

        public string isGoingForCoverKey;
        private bool IsGoingToCover
        {
            get => Blackboard.Get<bool>(isGoingForCoverKey);
            set => Blackboard.Set(isGoingForCoverKey, value);
        }
        
        private Entity.Entity enemy;
        private AIEntity entity;
        private float canSelectCoverProbability;
        private GunManager gunManager;
        private float avoidCoverProbability = 0.3f;

        public override void OnStart()
        {
            entity = Actor.GetComponent<AIEntity>();
            canSelectCoverProbability = entity.Characteristics.CanSelectCoverProbability;
            enemy = entity.GetEnemy();
            gunManager = entity.GunManager;
            var recklessness = entity.Characteristics.Recklessness;
            switch (recklessness)
            {
                case Recklessness.High:
                    avoidCoverProbability *= 3.0f;
                    break;
                case Recklessness.Low:
                    avoidCoverProbability /= 2f;
                    break;
                case Recklessness.Neutral:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Condition()
        {
            if (Random.value > canSelectCoverProbability)
            {
                // Too noob to consider cover.
                return false;
            }

            var currentGun = gunManager.CurrentGunIndex;
            // TODO Do not evaluate by percentage but by speed of depletion of ammo
            var currentAmmo = gunManager.GetAmmoInChargerForGun(currentGun);
            var chargerSize = gunManager.GetChargerSizeForGun(currentGun);
            if (currentAmmo / (float) chargerSize > CHARGER_PERCENTAGE)
            {
                IsGoingToCover = false;
                // It is too soon to search for cover
                return false;
            }

            var enemyDistance = (Actor.transform.position - enemy.transform.position).magnitude;
            var bestGunScore = float.MinValue;
            var bestGunIndex = -1;
            for (var i = 0; i < gunManager.NumberOfGuns; i++)
            {
                if (!gunManager.IsGunActive(i)) continue;
                var currentScore = gunManager.GetGunScore(i, enemyDistance, i == currentGun);
                if (currentScore > bestGunScore)
                {
                    bestGunScore = currentScore;
                    bestGunIndex = i;
                }
            }

            if (bestGunIndex != currentGun)
            {
                // We do not care about reloading the current weapon since we have a better one ready
                IsGoingToCover = false;
                return false;
            }

            if (!IsGoingToCover && Random.value < avoidCoverProbability)
            {
                // Avoid always rushing towards a cover point to not be predictable
                return false;
            }

            IsGoingToCover = true;
            return true;
        }

        public override Status Run()
        {
            return Condition() ? Status.Success : Status.Failure;
        }
    }
}