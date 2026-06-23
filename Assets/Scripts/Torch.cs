using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private GameObject onObject;
    [SerializeField] private GameObject offObject;
    [SerializeField] private bool on;
    
    void Start()
    {
        onObject.SetActive(on);
        offObject.SetActive(!on);
    }
}
