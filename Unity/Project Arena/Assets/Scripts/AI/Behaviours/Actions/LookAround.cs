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
        private const float THRESHOLD = 10f;
        private const float MIN_UPDATE_TIME = 0.3f;
        private const float MAX_UPDATE_TIME = 0.8f;

        private static readonly ReadOnlyCollection<AngleScore> AngleScores = new ReadOnlyCollection<AngleScore>(new[]
        {
            new AngleScore {angle = 0, score = 100, minLevel = CuriosityLevel.Low, allowedIfFocused = true},
            new AngleScore
                {angle = -45, score = 60, minLevel = CuriosityLevel.Medium, allowedIfFocused = true},
            new AngleScore
                {angle = +45, score = 60, minLevel = CuriosityLevel.Medium, allowedIfFocused = true},
            new AngleScore
                {angle = -90, score = 30, minLevel = CuriosityLevel.Medium, allowedIfFocused = false},
            new AngleScore
                {angle = +90, score = 30, minLevel = CuriosityLevel.Medium, allowedIfFocused = false},
            new AngleScore
                {angle = -135, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false},
            new AngleScore
                {angle = +135, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false},
            new AngleScore
                {angle = +180, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false}
        });

        [SerializeField] private bool focused;
        private CuriosityLevel curiosity;
        private Vector3 lookPoint;
        private float maxAngle;
        private MovementController movementController;
        private List<AngleScore> myValidAngles = new List<AngleScore>();
        private float nextUpdateTime;
        private SightController sightController;

        private List<float> angles;
        private List<float> scores;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            sightController = entity.SightController;

            movementController = entity.MovementController;
            curiosity = entity.GetCuriosity();

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
        }
        
        public override TaskStatus OnUpdate()
        {
            var realForward = GetMovementDirection();
            // For some reason we spawn moving up, causing weird issues with look direction
            if (realForward == Vector3.up || realForward == Vector3.zero)
                realForward = transform.forward;
            var angleX = 0f;
            
            angles.Clear();
            scores.Clear();
            
            if (MustUpdate())
            {
                UpdateNextUpdateTime();
                // TODO Agent is not moving, have it slowly look around?
                // if (realForward == Vector3.zero)
                // {
                //     realForward = sightSensor.GetLookDirection();
                //     angleX = 1;
                // }

                // Score formula: max(0, 10 + distanceScore * 40 + forwardScore * 30)

                var up = transform.up;

                foreach (var angleScore in myValidAngles)
                {
                    var direction = Quaternion.AngleAxis(angleScore.angle, up) * realForward;
                    var distance =
                        Physics.Raycast(transform.position, direction, out var hit, 50)
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

                var newDirection = Quaternion.AngleAxis(angleX, transform.up) * realForward;
                // In order to not look down, I should be looking at a point far in the horizon, hence the x100
                lookPoint = transform.position + newDirection * 100;
            }

            sightController.LookAtPoint(lookPoint);
            return TaskStatus.Running;
        }

        private Vector3 GetMovementDirection()
        {
            return movementController.GetVelocity().normalized;
        }

        private bool MustUpdate()
        {
            var position = transform.position;
            var lookDirection = lookPoint - position;
            var movementDirection = GetMovementDirection();

            var angle = Mathf.Abs(Vector3.Angle(lookDirection, movementDirection));
            return angle > maxAngle || nextUpdateTime < Time.time ||
                   Physics.Raycast(position, transform.forward, THRESHOLD);
        }

        private void UpdateNextUpdateTime()
        {
            nextUpdateTime = Time.time + Random.Range(MIN_UPDATE_TIME, MAX_UPDATE_TIME);
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