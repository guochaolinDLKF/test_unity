using System.Collections;
using System.Collections.Generic;
using tyme.eightchar;
using tyme.solar;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Runtime 
{
    public static async void Init()
    {
        Debug.Log("开始热更新");
        SolarTime birthTime = SolarTime.FromYmdHms(1994, 10, 17, 00, 25, 29);
        Debug.Log(birthTime.SolarDay.Month);
        EightChar eightChar=new EightChar("癸酉","庚申","己巳","己巳");
        
        SceneManager.GetSceneByName("Main");
    }
}
