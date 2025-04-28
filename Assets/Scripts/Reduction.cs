using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reduction : MonoBehaviour
{
    public float Delay, Speed;
    public Vector3 StartScale, EndScale;

    private void OnEnable()
    {
        StartCoroutine(Reduction_C());
    }

    private IEnumerator Reduction_C()
    {
        transform.localScale = StartScale;

        float scaleX = StartScale.x;
        float scaleY = StartScale.y;
        float scaleZ = StartScale.z;

        yield return new WaitForSeconds(Delay);

        while(scaleX > EndScale.x || scaleY > EndScale.y || scaleZ > EndScale.z)
        {
            if (scaleX > EndScale.x)
                scaleX -= Speed * Time.deltaTime;
            if (scaleY > EndScale.y)
                scaleY -= Speed * Time.deltaTime;
            if (scaleZ > EndScale.z)
                scaleZ -= Speed * Time.deltaTime;

            transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            yield return null;
        }        
    }
}
