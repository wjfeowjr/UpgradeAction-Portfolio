using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public partial class Utils
{
    /// target 에 있는 name의 transform을 가져온다
    public static T Search<T>(GameObject target, string name)
    {
        var list = target.GetComponentsInChildren<Transform>(true).ToList();
        Transform objTransform = (from item in list where item.name == name select item).FirstOrDefault();
        if (objTransform == null)
            return default(T);

        return objTransform.GetComponent<T>();
    }
    
    public static void AddClickAction(Button btn, Action action, bool isReset = true)
    {
        if (null == btn)
        {
            Debug.LogError("버튼이 Null값 입니다");
            return;
        }

        if (isReset == true)
            btn.onClick.RemoveAllListeners();
        
        btn.onClick.AddListener(() => { action();});
    }
}