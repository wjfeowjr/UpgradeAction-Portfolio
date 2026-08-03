using UnityEngine;

public class MerchantObject : MonoBehaviour
{
    [SerializeField] private GameObject minimapObject;
    
    public GameObject MinimapObject => minimapObject;
    
    public void SetParents(Transform targetTransform)
    {
        minimapObject.transform.SetParent(targetTransform);
    }
}
