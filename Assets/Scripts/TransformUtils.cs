using UnityEngine;

public static class TransformUtils
{
    public static void LookAt2D(this Transform transform, Vector3 targetPos, float degreeRevision = 0f)
    {
        Vector3 diff = targetPos - transform.position;
        diff.Normalize();
        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rot_z-degreeRevision);
    }
}
