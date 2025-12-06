using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NamPhuThuy.SceneManagement
{
    public class LoadSceneAuto : MonoBehaviour
    {
        enum ActiveOn
        {
            NONE = 0,
            START = 1,
            AWAKE = 2,
            ON_ENABLE = 3
        }  
        [SerializeField] private ActiveOn activeOn = ActiveOn.AWAKE;
        [SerializeField] private SceneConst.SceneName targetSceneName = SceneConst.SceneName.None;

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            if (activeOn == ActiveOn.AWAKE)
            {
                LoadTargetScene();
            }
        }
        
        private void Start()
        {
            if (activeOn == ActiveOn.START)
            {
                LoadTargetScene();
            }
        }
        
        private void OnEnable()
        {
            if (activeOn == ActiveOn.ON_ENABLE)
            {
                LoadTargetScene();
            }
        }

        #endregion

        #region Private Methods

        private void LoadTargetScene()
        {
            if (targetSceneName != SceneConst.SceneName.None)
            {
                SceneManager.LoadScene(targetSceneName.ToString(), LoadSceneMode.Single);
            }
        }

        #endregion
    }
}