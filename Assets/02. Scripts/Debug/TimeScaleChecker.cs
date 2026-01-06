using UnityEngine;

public class TimeScaleChecker : MonoBehaviour
{
    void Start()
    {
        GameLog.Log($"[TimeScaleChecker] Current Time.timeScale = {Time.timeScale}");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            GameLog.Log($"[TimeScaleChecker] Current Time.timeScale = {Time.timeScale}");
        }
    }
}