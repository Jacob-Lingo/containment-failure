using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;      // drag Player here
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 followPosition;

    private void Awake()
    {
        followPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        // SmoothDamp against the un-shaken position, then add the shake on top,
        // so the offset never feeds back into the follow and smears the camera.
        followPosition = Vector3.SmoothDamp(followPosition, desired, ref velocity, smoothTime);
        transform.position = followPosition + Juice.CameraOffset;
    }
}