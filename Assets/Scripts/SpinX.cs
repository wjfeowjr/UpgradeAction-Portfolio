using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpinX : MonoBehaviour
{
    private bool spinSwitch;
    private float angleX;
    private float firstSpeed;
    [SerializeField] private float spinSpeed;
    [SerializeField] private GameObject[] spinObjects;

    private void Awake()
    {
        firstSpeed = spinSpeed;
    }

    private async void OnEnable()
    {
        spinSwitch = true;
        angleX = 0;

        await UniTask.Yield();
        foreach (var spinObject in spinObjects)
            spinObject.SetActive(true);
    }

    private void OnDisable()
    {
        spinSpeed = firstSpeed;
    }

    private void Update()
    {
        SpinAngle();
    }

    private void SpinAngle()
    {
        if (!spinSwitch)
            return;
        
        if (transform.localScale.x > 0)
            angleX -= spinSpeed * Time.deltaTime;
        else
            angleX += spinSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, angleX);
    }

    public void SpinSwitchOn(bool on)
    {
        spinSwitch = on;
    }
}
