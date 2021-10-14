using System;
using System.Collections;
using System.Collections.Generic;
using AI.Behaviours.NewBehaviours;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

[Serializable]
public class LookAtPoint : Action
{
    private AISightController sightController;

    [SerializeField] private SharedVector3 lookPoint;

    public override void OnAwake()
    {
        sightController = GetComponent<AISightController>();
    }

    public override TaskStatus OnUpdate()
    {
        sightController.LookAtPoint(lookPoint.Value);
        return TaskStatus.Running;
    }
}
