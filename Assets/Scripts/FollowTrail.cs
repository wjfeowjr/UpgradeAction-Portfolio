using UnityEngine;
using UnityEngine.Serialization;

public class FollowTrail : MonoBehaviour
{
    [SerializeField] private Transform parentTransform;

    [SerializeField] private bool followX;
    [SerializeField] private bool followY;
    [SerializeField] private bool followZ;

    private float xScale;
    private float yScale;
    private float zScale;

    private void OnEnable()
    {
        Follow();
    }

    private void Follow()
    {
        xScale = Mathf.Abs(transform.localScale.x);
        yScale = Mathf.Abs(transform.localScale.y);
        zScale = Mathf.Abs(transform.localScale.z);
        
        if (parentTransform.localScale.x < 0)
        {
            if(followX)
                xScale = -Mathf.Abs(transform.localScale.x);
            if(followY)
                yScale = -Mathf.Abs(transform.localScale.y);
            if(followZ)
                zScale = -Mathf.Abs(transform.localScale.z);
        }
        
        transform.localScale = new Vector3(xScale, yScale, zScale);
    }
}
