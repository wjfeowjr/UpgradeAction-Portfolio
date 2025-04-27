using UnityEngine;

public class FollowTrailY : MonoBehaviour
{
    private Transform ParentTransform;
    public bool IsUpdate;
    
    private void Awake()
    {
        ParentTransform = transform.parent.gameObject.transform;
    }

    private void OnEnable()
    {
        if (ParentTransform.localScale.x > 0 || ParentTransform.localScale.y > 0)
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Abs(transform.localScale.y), transform.localScale.z);
        if (ParentTransform.localScale.x < 0 || ParentTransform.localScale.y < 0)
            transform.localScale = new Vector3(transform.localScale.x, -Mathf.Abs(transform.localScale.y), transform.localScale.z);
    }
    
    private void Update()
    {
        if (IsUpdate)
            Follow();
    }

    private void Follow()
    {
        if (ParentTransform.localScale.x > 0 || ParentTransform.localScale.y > 0)
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Abs(transform.localScale.y), transform.localScale.z);
        if (ParentTransform.localScale.x < 0 || ParentTransform.localScale.y < 0)
            transform.localScale = new Vector3(transform.localScale.x, -Mathf.Abs(transform.localScale.y), transform.localScale.z);
    }
}
