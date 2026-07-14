using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GuardMotor : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float arriveRadius = 1.5f;
    
    private Rigidbody2D rb;
    private Vector2? seekTarget;  //null = no movement order
    
    private void Awake() => rb = GetComponent<Rigidbody2D>();
    
    public void Seek(Vector2 worldPosition) => seekTarget = worldPosition;

    public void Stop() => seekTarget = null;

    private void FixedUpdate()
    {
        if (seekTarget == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        Vector2 toTarget = seekTarget.Value - rb.position;
        float distance = toTarget.magnitude;
        
        // Arrive; ramp speed down inside the radius so the guard
        // doesn't overshoot and orbit the player. Outside it, pure seek.
        float speed = distance < arriveRadius 
            ? maxSpeed * (distance / arriveRadius)
            : maxSpeed;
        
        rb.linearVelocity = toTarget.normalized * speed;
    }
}
