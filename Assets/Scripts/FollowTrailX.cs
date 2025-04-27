using UnityEngine;

public class FollowTrailX : MonoBehaviour
{
    private Transform ParentTransform;
    public bool IsUpdate;

    private void Awake()
    {
        ParentTransform = transform.parent.gameObject.transform;
    }

    private void OnEnable()
    {
        Follow();
    }

    private void Update()
    {
        if (IsUpdate)
            Follow();
    }

    private void Follow()
    {
        if (ParentTransform.localScale.x > 0)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (ParentTransform.localScale.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }
}
