using UnityEngine;

public class SurfaceOrbitCamera : MonoBehaviour
{
    public Vector3 target = new Vector3(4.5f, 4.5f, 4.5f);
    public float distance = 18f;
    public float minDistance = 7f;
    public float maxDistance = 30f;
    public float yaw = -38f;
    public float pitch = 28f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public float rotateSpeed = 0.18f;
    public float zoomSpeed = 2f;
    public int rotateMouseButton = 1;

    private void Start()
    {
        ApplyTransform();
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButton(rotateMouseButton))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed * 100f;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * 100f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        ApplyTransform();
    }

    public void Focus(Vector3 newTarget, float newDistance)
    {
        target = newTarget;
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        transform.position = target + offset;
        transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
    }
}
