using UnityEngine;

public class AIMovementController : MonoBehaviour
{
    private float speed;

    private Transform t;
    private Vector3 previousPosition;

    public void SetParameters(float speed)
    {
        this.speed = speed;
    }

    private void Awake()
    {
        t = transform;
        previousPosition = t.position;
    }

    public void MoveToPosition(Vector3 position)
    {
        // TODO Control movement for this frame, prevent moving too fast
        previousPosition = t.position;
        t.position = position;
    }

    public Vector3 GetVelocity()
    {
        return (t.position - previousPosition) / Time.deltaTime;
    }
}