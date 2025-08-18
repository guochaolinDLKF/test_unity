using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[RequireComponent(typeof(Animation))]
public class UICommont : MonoBehaviour,IPointerClickHandler
{
    class MyClass
    {
        public string name;
    }
    void Start()
    {
        Debug.Log("UICommont");
        MyClass myClass = new MyClass();
        myClass.name = "MyClass";
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        
    }
}
