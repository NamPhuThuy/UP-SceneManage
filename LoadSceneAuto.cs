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
        [SerializeField] private SceneManageConst.SceneName targetSceneName = SceneManageConst.SceneName.None;
        [SerializeField] private float delayLoad = 0f;

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            if (activeOn == ActiveOn.AWAKE)
            {
                StartCoroutine(IE_LoadTargetScene());
            }
        }
        
        private void Start()
        {
            if (activeOn == ActiveOn.START)
            {
                StartCoroutine(IE_LoadTargetScene());
            }
        }
        
        private void OnEnable()
        {
            if (activeOn == ActiveOn.ON_ENABLE)
            {
                StartCoroutine(IE_LoadTargetScene());
            }
        }

        #endregion

        #region Private Methods
        
        private IEnumerator IE_LoadTargetScene()
        {
            yield return new WaitForSeconds(delayLoad);
            LoadTargetScene();
        }

        private void LoadTargetScene()
        {
            if (targetSceneName != SceneManageConst.SceneName.None)
            {
                SceneManager.LoadScene(targetSceneName.ToString(), LoadSceneMode.Single);
            }
        }

        #endregion
    }
}