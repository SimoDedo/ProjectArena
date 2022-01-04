using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AssemblyAI.AI.Layer1.Actuator;
using UnityEngine;

namespace AssemblyAI.Actions
{
    public class ActionLookAround
    {
        private readonly AIEntity entity;
        private readonly Transform transform;
        private readonly AISightController sightController;
        private readonly AIMovementController movementController;
        private Vector3 lookPoint;
        private float nextUpdateTime;
        private readonly float maxAngle;
        private readonly List<AngleScore> myValidAngles = new List<AngleScore>();

        public ActionLookAround(AIEntity entity)
        {
            this.entity = entity;
            transform = entity.transform;
            sightController = entity.SightController;

            movementController = entity.MovementController;
            var curiosity = entity.GetCuriosity();

            nextUpdateTime = float.MinValue;

            foreach (var angleScore in AngleScores)
            {
                if (curiosity >= angleScore.minLevel &&
                    (!entity.BotState.lookAroundIsFocused || angleScore.allowedIfFocused))
                {
                    maxAngle = Mathf.Max(maxAngle, Mathf.Abs(angleScore.angle));
                    myValidAngles.Add(angleScore);
                }
            }
        }

        public void Perform()
        {
            var realForward = GetMovementDirection();
            // For some reason we spawn moving up, causing weird issues with look direction
            if (realForward == Vector3.up) realForward = entity.transform.forward;
            var angleX = 0f;
            if (MustUpdate())
            {
                UpdateNextUpdateTime();
                // TODO Agent is not moving, have it slowly look around?

                // Score formula: max(0, 10 + distanceScore * 40 + forwardScore * 30)

                var angles = new List<float>();
                var scores = new List<float>();
                var up = transform.up;

                foreach (var angleScore in myValidAngles)
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

            sightController.LookAtPoint(lookPoint, 0.3f);
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

        private const float THRESHOLD = 10f;
        private const float MIN_UPDATE_TIME = 0.3f;
        private const float MAX_UPDATE_TIME = 0.8f;


        private struct AngleScore
        {
            public int angle;
            public int score;
            public CuriosityLevel minLevel;
            public bool allowedIfFocused;
        }

        private static readonly ReadOnlyCollection<AngleScore> AngleScores = new ReadOnlyCollection<AngleScore>(
            new[]
            {
                new AngleScore {angle = 0, score = 100, minLevel = CuriosityLevel.Low, allowedIfFocused = true},
                new AngleScore {angle = -45, score = 60, minLevel = CuriosityLevel.Medium, allowedIfFocused = true},
                new AngleScore {angle = +45, score = 60, minLevel = CuriosityLevel.Medium, allowedIfFocused = true},
                new AngleScore
                {
                    angle = -90, score = 30, minLevel = CuriosityLevel.Medium, allowedIfFocused = false
                },
                new AngleScore
                {
                    angle = +90, score = 30, minLevel = CuriosityLevel.Medium, allowedIfFocused = false
                },
                new AngleScore {angle = -135, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false},
                new AngleScore {angle = +135, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false},
                new AngleScore {angle = +180, score = 10, minLevel = CuriosityLevel.High, allowedIfFocused = false}
            }
        );
    }
}

namespace BotSpace
{
    public partial class BotState
    {
        public bool lookAroundIsFocused = false;
    }
}