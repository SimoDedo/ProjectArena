using UnityEngine;

[CreateAssetMenu(
    fileName = "<name>.asset",
    menuName = "RuleAction/" + "<name>")]
public abstract class RuleAction : ScriptableObject
{
    public abstract bool Rule(AIEntity entity);
    public abstract void Action(AIEntity entity);
}
