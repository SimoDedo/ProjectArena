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

namespace AI.Behaviours.NewBehaviours
{
    [Serializable]
    public class NewLookAround2 : Action
    {
        private AIMovementController movementController;
        private AISightController sightController;
        private AIEntity.CuriosityLevel curiosity;
        private bool mustFindAngle;
        private float angleX;
        private Vector3 previousVelocity;
        private float nextTimeToChange;
        private Vector3 lookPoint;

        private const float REFRESH_TIME = 1.3f;

        public override void OnAwake()
        {
            sightController = GetComponent<AISightController>();
            movementController = GetComponent<AIMovementController>();
            curiosity = GetComponent<AIEntity>().GetCuriosity();
        }

        public override void OnStart()
        {
            mustFindAngle = true;
            angleX = 0;
            UpdateAndGetDistances(GetForwardDirection());
        }

        private Vector3 GetForwardDirection()
        {
            var forward = movementController.GetVelocity().normalized;
            if (forward == Vector3.zero)
                forward = sightController.GetLookDirection();
            return forward;
        }

        private List<float> UpdateAndGetDistances(Vector3 forward)
        {
            var differences = new List<float>();
            var up = transform.up;
            foreach (var angleData in angleDataCollection)
            {
                if (curiosity >= angleData.minimumCuriosity)
                {
                    // I can consider this angle
                    var direction = Quaternion.AngleAxis(angleData.angle, up) * forward;
                    float distance;
                    Vector3 angleDataLookPoint;
                    if (Physics.Raycast(transform.position, direction, out var hit))
                    {
                        distance = hit.distance;
                        angleDataLookPoint = hit.point;
                    }
                    else
                    {
                         distance = float.MaxValue;
                         angleDataLookPoint = transform.position + direction * 100;
                    }
                    differences.Add(distance - angleData.previousDistance);
                    angleData.previousDistance = distance;
                    angleData.lookPoint = angleDataLookPoint;
                }
            }

            return differences;
        }

        private bool CanNoLongerSeeLookPoint()
        {
            var rtn = Physics.Linecast(transform.position, lookPoint);
            Debug.Log("Can no longer see? " + rtn);
            return rtn;
        }
        
        private class AngleData
        {
            public float angle;
            public AIEntity.CuriosityLevel minimumCuriosity;
            public float previousDistance;
            public Vector3 lookPoint;
        }

        public override TaskStatus OnUpdate()
        {
            var differences = UpdateAndGetDistances(GetForwardDirection());
            if (nextTimeToChange <= Time.time || CanNoLongerSeeLookPoint())
            {
                // Time to change
                nextTimeToChange = Time.time + REFRESH_TIME;

                var indexes = new List<int>();
                for (var i = 0; i < differences.Count; i++)
                    if (differences[i] > MAX_THRESHOLD)
                        indexes.Add(i);

                
                if (indexes.Count == 0)
                {
                    // Look forward    
                    lookPoint = transform.position + GetForwardDirection() * 100;
                }
                else
                {
                    var chosenIndex = Random.Range(0, indexes.Count);
                    lookPoint = angleDataCollection[chosenIndex].lookPoint;
                }
            }

            sightController.LookAtPoint(lookPoint, 0.6f);
            return TaskStatus.Running;
        }

        private const float MAX_THRESHOLD = 40f;

        private static readonly List<AngleData> angleDataCollection = new List<AngleData>(new[]
        {
            // new AngleData {angle = 0,    minimumCuriosity = AIEntity.CuriosityLevel.Low},
            new AngleData {angle = -45, minimumCuriosity = AIEntity.CuriosityLevel.Medium},
            new AngleData {angle = +45, minimumCuriosity = AIEntity.CuriosityLevel.Medium},
            new AngleData {angle = -90, minimumCuriosity = AIEntity.CuriosityLevel.Medium},
            new AngleData {angle = +90, minimumCuriosity = AIEntity.CuriosityLevel.Medium},
            new AngleData {angle = -135, minimumCuriosity = AIEntity.CuriosityLevel.High},
            new AngleData {angle = +135, minimumCuriosity = AIEntity.CuriosityLevel.High},
        });
    }
}