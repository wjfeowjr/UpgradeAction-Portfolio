using UnityEngine;
using UnityEngine.Serialization;

public class ObjectTimer : MonoBehaviour
{
    public float second;
    public bool ignoreTimeScale;
    private float currentSecond;

    private void OnEnable()
    {
        currentSecond = second;
    }

    private void Update()
    {
        Timer();
    }

    private void Timer()
    {
        if(second > 0)
        {
            currentSecond -= Time.deltaTime;
            
            if (currentSecond <= 0 && gameObject.activeSelf)
                gameObject.SetActive(false);
        }        
    }
}
