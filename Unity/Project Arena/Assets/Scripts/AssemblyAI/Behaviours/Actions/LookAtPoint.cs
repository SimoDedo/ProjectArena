using System;
using AssemblyAI.AI.Layer1.Actuator;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

[Serializable]
public class LookAtPoint : Action
{
    private AISightController sightController;

    [SerializeField] private SharedVector3 lookPoint;

    public override void OnAwake()
    {
        // TODO this must be set up!
        sightController = GetComponent<AIEntity>().SightController;
    }

    
    public override TaskStatus OnUpdate()
    {
        sightController.LookAtPoint(lookPoint.Value);
        return TaskStatus.Running;
    }
}
