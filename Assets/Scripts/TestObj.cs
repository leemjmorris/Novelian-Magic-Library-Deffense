using UnityEngine;
using NovelianMagicLibraryDefense.Managers;
public class TestObj : MonoBehaviour
{
    public GameManager gameManager;
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            gameManager.StageState.SetStageState(StageState.Cleared);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            gameManager.StageState.SetStageState(StageState.Failed);
        }
    }
#endif
}