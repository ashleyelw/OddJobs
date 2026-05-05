using UnityEngine;
using UnityEngine.SceneManagement;

public class tetsscene : MonoBehaviour
{
    public string targetSceneName = "Level2";

    /// <summary>
    /// 跳转到目标场景
    /// </summary>
    public void JumpToTargetScene()
    {
        // 检查场景是否已添加到 Build Settings
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("跳转失败！请检查：\n1. 场景名是否正确\n2. 场景是否添加到 File-Build Settings");
        }
    }


}
