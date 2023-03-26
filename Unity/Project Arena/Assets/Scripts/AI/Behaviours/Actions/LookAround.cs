using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AI.Layers.Actuators;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Lets the entity look around while moving. How much it can rotate the view depends on the
    /// <see cref="CuriosityLevel"/>
    /// </summary>
    [Serializable]
    public class LookAround : Action
    {
        private const float THRESHOLD = 5f;
        private const float MIN_UPDATE_TIME = 1.5f;
        private const float MAX_UPDATE_TIME = 2.5f;

        private static readonly ReadOnlyCollection<AngleScore> AngleScores = new(new[]
        {
            new AngleScore {angle = 0, score = 100, minLevel = CuriosityLevel.Low, allowedIfFocused = true},
            new AngleScore
                {angle = -20, score = 60, minLevel = CuriosityLevel.Medium, allowedIfFocused = true},
            new AngleScore
                {angle = +20, score = 60, minLevel = CuriosityLevel.Medium, allowedIfFocused = true},
            new AngleScore
                {angle = -45, score = 30, minLevel = CuriosityLevel.Medium, allowedIfFocused = false},
            new AngleScore
                {angle = +45, score = 30, minLevel = CuriosityLevel.Medium, allowedIfFocused = false},
            new AngleScore
                {angle = -90, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false},
            new AngleScore
                {angle = +90, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false},
        });

        [SerializeField] private bool focused;
        private CuriosityLevel curiosity;
        private float lookAngle;
        private float maxAngle;
        private MovementController movementController;
        private List<AngleScore> myValidAngles = new();
        private float nextUpdateTime;
        private float nextWallUpdateTime;
        private SightController sightController;

        private List<float> angles;
        private List<float> scores;
        private Transform _transform;
        private int layerMask = LayerMask.GetMask("Default", "Wall");

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            sightController = entity.SightController;

            movementController = entity.MovementController;
            curiosity = entity.Characteristics.Curiosity;

            nextUpdateTime = float.MinValue;

            // Avoid recomputing and adding again all valid angles if already computed
            if (myValidAngles.Count == 0)
                foreach (var angleScore in AngleScores)
                    if (curiosity >= angleScore.minLevel && (!focused || angleScore.allowedIfFocused))
                    {
                        maxAngle = Mathf.Max(maxAngle, Mathf.Abs(angleScore.angle));
                        myValidAngles.Add(angleScore);
                    }
            
            angles = new List<float>(myValidAngles.Count);
            scores = new List<float>(myValidAngles.Count);
            
            _transform = transform;
        }
        
        public override TaskStatus OnUpdate()
        {
            var realForward = GetMovementDirection();
            // For some reason we spawn moving up, causing weird issues with look direction
            if (realForward == Vector3.up || realForward == Vector3.zero)
                realForward = _transform.forward;
            
            angles.Clear();
            scores.Clear();
            
            var up = _transform.up;
            if (MustUpdate())
            {
                var angleX = 0f;
                UpdateNextUpdateTime();

                // Score formula: max(0, 10 + distanceScore * 40 + forwardScore * 30)
                
                foreach (var angleScore in myValidAngles)
                {
                    var direction = Quaternion.AngleAxis(angleScore.angle, up) * realForward;
                    var distance =
                        Physics.Raycast(_transform.position, direction, out var hit, 50, layerMask)
                            ? hit.distance
                            : 50;
                    distance = Mathf.Clamp(distance, 0, 50);
                    scores.Add(distance * distance / 2500 * angleScore.score);
                    angles.Add(angleScore.angle);
                }

                var scoreSum = scores.Sum();
                if (scoreSum != 0)
                {
                    var random = Random.value;
                    var activationTreshold = 0f;
                    for (var i = 0; i < scores.Count; i++)
                    {
                        if (scores[i] == 0) continue;
                        var newThreshold = scores[i] / scoreSum + activationTreshold;
                        if (random <= newThreshold)
                        {
                            angleX = angles[i];
                            // Debug.LogWarning("Chosen angle " + angleX);
                            break;
                        }

                        activationTreshold = newThreshold;
                    }
                }

                lookAngle = angleX;
                // var newDirection = Quaternion.AngleAxis(angleX, _transform.up) * realForward;
                // In order to not look down, I should be looking at a point far in the horizon, hence the x100
                // lookPoint = _transform.position + newDirection * 100;
            }

            var lookPoint = _transform.position + Quaternion.AngleAxis(lookAngle, up) * realForward;
            sightController.LookAtPoint(lookPoint);
            return TaskStatus.Running;
        }

        private Vector3 GetMovementDirection()
        {
            return movementController.GetVelocity().normalized;
        }

        private float totalInvalidLookTime;
        private bool MustUpdate()
        {
            if (nextUpdateTime < Time.time)
            {
                return true;
            }
            
            var position = _transform.position;
            var lookDirection =  sightController.GetHeadForward();
            if (Physics.Raycast(position, lookDirection,  THRESHOLD, layerMask))
            {
                totalInvalidLookTime += Time.deltaTime;
            }
            else
            {
                totalInvalidLookTime -= Mathf.Min(totalInvalidLookTime, Time.deltaTime * 2);
            }
            
            return totalInvalidLookTime > 0.2f;
        }

        private void UpdateNextUpdateTime()
        {
            nextUpdateTime = Time.time + Random.Range(MIN_UPDATE_TIME, MAX_UPDATE_TIME);
            totalInvalidLookTime = 0;
        }

        private struct AngleScore
        {
            public int angle;
            public int score;
            public CuriosityLevel minLevel;
            public bool allowedIfFocused;
        }
    }
}