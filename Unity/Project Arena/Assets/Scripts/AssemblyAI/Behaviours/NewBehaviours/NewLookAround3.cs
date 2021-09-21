using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using Entities.AI.Layer1.Sensors;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

// Current version is this one!
namespace AI.Behaviours.NewBehaviours
{
    [Serializable]
    public class NewLookAround3 : Action
    {
        private AIMovementController movementController;
        private AISightController sightController;
        private AIEntity.CuriosityLevel curiosity;
        private bool mustFindLookPoint;
        private Vector3 lookPoint;

        public override void OnAwake()
        {
            sightController = GetComponent<AISightController>();
            movementController = GetComponent<AIMovementController>();
            curiosity = GetComponent<AIEntity>().GetCuriosity();
        }

        public override void OnStart()
        {
            mustFindLookPoint = true;
        }

        public override TaskStatus OnUpdate()
        {
            var realForward = movementController.GetVelocity().normalized;
            var angleX = 0f;
            
            if (mustFindLookPoint || Physics.Raycast(transform.position, transform.forward, THRESHOLD))
            {
                // TODO Agent is not moving, have it slowly look around?
                // if (realForward == Vector3.zero)
                // {
                //     realForward = sightSensor.GetLookDirection();
                //     angleX = 1;
                // }

                // Score formula: max(0, 10 + distanceScore * 40 + forwardScore * 30)

                var angles = new List<float>();
                var scores = new List<float>();
                var up = transform.up;

                foreach (var angleScore in angleScores)
                {
                    if (curiosity >= angleScore.minimumLevel)
                    {
                        var direction = Quaternion.AngleAxis(angleScore.angle, up) * realForward;
                        var distance =
                            Physics.Raycast(transform.position, direction, out var hit, float.PositiveInfinity)
                                ? hit.distance
                                : float.PositiveInfinity;
                        distance = Mathf.Clamp(distance, 0, 50);
                        scores.Add(distance * distance / 2500 * angleScore.score);
                        angles.Add(angleScore.angle);
                    }
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
                lookPoint = transform.position + (newDirection * 100);
            }

            sightController.LookAtPoint(lookPoint, 0.3f);
            mustFindLookPoint = false;
            return TaskStatus.Running;
        }

        private const float THRESHOLD = 10f;
        private struct AngleScore
        {
            public int angle;
            public int score;
            public AIEntity.CuriosityLevel minimumLevel;
        }

        private static readonly ReadOnlyCollection<AngleScore> angleScores = new ReadOnlyCollection<AngleScore>(new[]
        {
            new AngleScore {angle = 0, score = 100, minimumLevel = AIEntity.CuriosityLevel.Low},
            new AngleScore {angle = -45, score = 60, minimumLevel = AIEntity.CuriosityLevel.Medium},
            new AngleScore {angle = +45, score = 60, minimumLevel = AIEntity.CuriosityLevel.Medium},
            new AngleScore {angle = -90, score = 30, minimumLevel = AIEntity.CuriosityLevel.Medium},
            new AngleScore {angle = +90, score = 30, minimumLevel = AIEntity.CuriosityLevel.Medium},
            new AngleScore {angle = -135, score = 10, minimumLevel = AIEntity.CuriosityLevel.High},
            new AngleScore {angle = +135, score = 10, minimumLevel = AIEntity.CuriosityLevel.High},
            new AngleScore {angle = +180, score = 10, minimumLevel = AIEntity.CuriosityLevel.High},
        });
    }
}