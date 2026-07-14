using System;
using UnityEngine;

public class GuardPerception : MonoBehaviour
{
    [SerializeField] private Transform target;          // Player — assigned in Inspector for now
    [SerializeField] private float detectRadius = 6f;
    [SerializeField] private float loseRadius = 8f;     // hysteresis: lose > detect

    public event Action<Transform> TargetSpotted;
    public event Action TargetLost;

    public bool HasTarget { get; private set; }

    private void Update()
    {
        if (target == null) return;

        float d = Vector2.Distance(transform.position, target.position);

        if (!HasTarget && d <= detectRadius)
        {
            HasTarget = true;
            TargetSpotted?.Invoke(target);
        }
        else if (HasTarget && d >= loseRadius)
        {
            HasTarget = false;
            TargetLost?.Invoke();
        }
    }
}