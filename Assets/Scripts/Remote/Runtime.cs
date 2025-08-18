using System.Collections;
using System.Collections.Generic;
using tyme.eightchar;
using tyme.solar;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Remote
{
    public class Runtime 
    {
        public static async void Init()
        {
            Debug.Log("开始热更新");
            SolarTime birthTime = SolarTime.FromYmdHms(1994, 10, 17, 00, 25, 29);
            Debug.Log(birthTime.SolarDay.Month);
            EightChar eightChar=new EightChar("癸酉","庚申","己巳","己巳");
        
            Debug.Log(eightChar);
            SceneManager.GetSceneByName("Main");
            if (Mouse.current.leftButton.wasPressedThisFrame)  
            {        
                Debug.Log("Left mouse button was pressed");  
            }  
        
            GameObject go=new GameObject("GameObject");
            go.AddComponent<UICommont>();
        
            Pointer pointer = Pointer.current;
            if (pointer == null) return;
        
            Debug.Log(pointer.position.ReadValue());
        }
    }
}

