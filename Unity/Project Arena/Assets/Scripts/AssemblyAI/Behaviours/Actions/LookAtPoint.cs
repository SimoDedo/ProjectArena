using System;
using System.Linq;
using AssemblyAI.AI.Layer1.Actuator;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

[Serializable]
public class LookAtPoint : Action
{
    private AISightController sightController;

    [SerializeField] private SharedSelectedPathInfo pathInfo;
    private Vector3 lookPoint;

    public override void OnAwake()
    {
        sightController = GetComponent<AIEntity>().SightController;
    }

    public override void OnStart()
    {
        lookPoint = pathInfo.Value.corners.Last();
    }


    public override TaskStatus OnUpdate()
    {
        sightController.LookAtPoint(lookPoint);
        return TaskStatus.Running;
    }
}