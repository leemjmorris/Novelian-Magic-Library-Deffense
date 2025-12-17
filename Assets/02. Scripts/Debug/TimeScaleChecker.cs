using UnityEngine;

public class TimeScaleChecker : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[TimeScaleChecker] Current Time.timeScale = {Time.timeScale}");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"[TimeScaleChecker] Current Time.timeScale = {Time.timeScale}");
        }
    }
}