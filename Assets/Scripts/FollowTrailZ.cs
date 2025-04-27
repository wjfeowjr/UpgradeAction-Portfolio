using UnityEngine;

public class FollowTrailZ : MonoBehaviour
{
    private Transform ParentTransform;
    public bool IsUpdate;
    
    private void Awake()
    {
        ParentTransform = transform.parent.gameObject.transform;
    }

    private void OnEnable()
    {
        if (ParentTransform.localScale.x > 0)
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Mathf.Abs(transform.localScale.z));
        else if (ParentTransform.localScale.x < 0)
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, -Mathf.Abs(transform.localScale.z));
    }
    
    private void Update()
    {
        if (IsUpdate)
            Follow();
    }

    private void Follow()
    {
        if (ParentTransform.localScale.x > 0)
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Mathf.Abs(transform.localScale.z));
        else if (ParentTransform.localScale.x < 0)
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, -Mathf.Abs(transform.localScale.z));
    }
}
