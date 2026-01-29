// csharp
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Tools;
using NamPhuThuy.AudioManage;
using NamPhuThuy.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.ResourceManagement.AsyncOperations;
using Ease = PrimeTween.Ease;
using Sequence = PrimeTween.Sequence;
using Tween = PrimeTween.Tween;

namespace NamPhuThuy.SceneManagement
{
    public class ResourceAndServicesLoader : MonoBehaviour
    {
        [Header("Flags")]
        [SerializeField] private SceneManageConst.SceneName currentScene = SceneManageConst.SceneName.None;
        [SerializeField] private SceneManageConst.SceneName targetScene = SceneManageConst.SceneName.None;
        
        [Header("Components")]
        [SerializeField] private RectTransform loadingScreenContainer;
        [SerializeField] private Image blackBackground;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Image splashScreen;
        [SerializeField] private TMP_Text loadingText;

        [Header("Customize")]
        [SerializeField] private float minExpectedLoadingTime = 1.5f;
        [SerializeField] private float maxExpectedLoadingTime = 3.0f;
        private const float BURST_LOADING_DURATION = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Service gates (placeholders)")]
        [Tooltip("Wait for remote content preparation (e.g., Addressables, configs) before bursting to 100\\%.")]
        [SerializeField] private bool waitForRemoteContent = false;
        [Tooltip("Wait for AppLovin SDK ready before bursting to 100\\%.")]
        [SerializeField] private bool waitForApplovin = false;
        [Tooltip("Wait for AppsFlyer SDK ready before bursting to 100\\%.")]
        [SerializeField] private bool waitForAppsflyer = false;
        [Tooltip("Seconds to wait per service before auto‑continuing.")]
        [SerializeField] private float serviceInitTimeout = 10f;
#if UNITY_EDITOR
        [Tooltip("Editor‑only: simulate services finishing quickly so the loader doesn’t stall during dev.")]
        [SerializeField] private bool simulateServicesInEditor = true;
#endif

        private readonly List<Tween> _tweens = new List<Tween>();
        [SerializeField] private bool _isReadyToBurst; // set when GamePlay is loaded
        private AsyncOperationHandle _menuSceneHandle;


        private float maxLoadingTime = 20f;
        // Service states
        private volatile bool _remoteContentReady;
        private volatile bool _applovinReady;
        private volatile bool _appsflyerReady;

        private bool ServicesReady =>
            (!waitForRemoteContent || _remoteContentReady) &&
            (!waitForApplovin || _applovinReady) &&
            (!waitForAppsflyer || _appsflyerReady);

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            AudioManager.Ins.StopAll(Audio.Type.MUSIC);
            AudioManager.Ins.Play(AudioEnum.MUSIC_BG_NEW);
            
            SceneManager.sceneLoaded += OnSceneLoaded;

            progressBarFill.fillAmount = 0f;
            loadingText.text = "0%";
        }

        private void Start()
        {
            blackBackground.gameObject.SetActive(false);

            // Kick off service placeholders in parallel
            StartCoroutine(RunServicePlaceholders());

            // Start visual loading flow
            StartCoroutine(SlowTransititon());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopAllTweens(_tweens);
        }

        #endregion

        #region Private Methods

        private void StopAllTweens(IList<Tween> tweens, bool complete = false, bool clearList = true)
        {
            if (tweens == null) return;

            for (int i = 0; i < tweens.Count; i++)
            {
                var t = tweens[i];
                t.Complete();
            }

            if (clearList)
                tweens.Clear();
        }

        #endregion

        private IEnumerator SlowTransititon()
        {
            DebugLogger.LogFrog();
            var waitForSeconds = new WaitForSeconds(Time.fixedDeltaTime);

            float progress = 0f;
            float deltaProgress = Time.fixedDeltaTime / Mathf.Max(0.01f, maxExpectedLoadingTime);

            float timePassedAfterSceneLoaded = 0f;

            // SPLASH SCREEN
            bool isSplashScreenHidden = false;

            splashScreen.DOKill();
            _tweens.Add(Tween.Alpha(splashScreen, 0f, 0.3f).OnComplete(() =>
            {
                splashScreen.gameObject.SetActive(false);
                isSplashScreenHidden = true;
            }));
            
            while (!isSplashScreenHidden)
                yield return waitForSeconds;

            // Simulate initial progress up to ~21%
            while (progress < 0.21f)
            {
                UpdateProgress();
                yield return waitForSeconds;
            }   

            // Load target scene additively
            DebugLogger.LogFrog(message:$"About to load the target scene");
            SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Additive);

            // Gate until scene loaded, services ready (if enabled), and min time elapsed
            while ((!_isReadyToBurst || !ServicesReady) || timePassedAfterSceneLoaded < minExpectedLoadingTime)
            {
                UpdateProgress();
                timePassedAfterSceneLoaded += Time.fixedDeltaTime;
                if (timePassedAfterSceneLoaded > maxLoadingTime)
                    break;
                yield return waitForSeconds;
            }

            BurstTransition();

            void UpdateProgress()
            {
                progressBarFill.fillAmount = progress;
                loadingText.text = $"{(int)(progress * 100f)}%";
                if (progress < 1f)
                    progress += deltaProgress;
            }
        }

        // csharp
        private void BurstTransition()
        {
            DebugLogger.Log();
            if (progressBarFill.IsActive())
            {
                progressBarFill.DOKill();
            }

            if (blackBackground.IsActive())
            {
                blackBackground.DOKill();
            }

            var seq = Sequence.Create();
            DebugLogger.Log(message:$"Sequence is created");

            // Progress 0 -> 100% linearly
            seq.Chain(Tween.Custom(
                startValue: progressBarFill.fillAmount,
                endValue: 1f, 
                duration: BURST_LOADING_DURATION,
                onValueChange: val => 
                {
                    progressBarFill.fillAmount = val;
                    loadingText.text = $"{(int)(val * 100f)}%";
                },
                ease: Ease.OutQuad 
            ));
            
            DebugLogger.Log(message:$"Last Step");

            seq.Chain(Tween.Delay(0f, () =>
            {
                // 1. Setup: Activate and reset alpha to 0
                Debug.Log(message: $"Turn onthe black screen");
                blackBackground.gameObject.SetActive(true);
                var c = blackBackground.color;
                c.a = 0f;
                blackBackground.color = c;
            }));
            
            
            // 2. The Fade Tween: Sequence waits for this to finish
            seq.Chain(Tween.Delay(0f, () =>
            {
                Debug.Log(message: $"Fade in the black screen");
                Tween.Alpha(blackBackground, 1f, fadeOutDuration);
            }));
            
            // 3. Finalize: Runs exactly when the fade finishes
            seq.Chain(Tween.Delay(fadeOutDuration + 0.2f, () =>
            {
                Debug.Log(message: $"About to send EGameStarted");
                
                SceneManager.UnloadSceneAsync(currentScene.ToString());
                
                DebugLogger.Log(message: $"About to unload scene: {currentScene}");
                MMEventManager.TriggerEvent(new EGameStarted());
                
                Debug.Log(message: $"Done trigger event EGameStarted");
            }));
            
            /*seq.ChainCallback(() => 
                {
                    // 1. Setup: Activate and reset alpha to 0
                    Debug.Log(message:$"Fade in the black screen");
                    blackBackground.gameObject.SetActive(true);
                    var c = blackBackground.color;
                    c.a = 0f;
                    blackBackground.color = c;
                })
                    // 2. The Fade Tween: Sequence waits for this to finish
                .Chain(Tween.Alpha(blackBackground, 1f, fadeOutDuration)) 
                    // 3. Finalize: Runs exactly when the fade finishes
                .ChainCallback(() => 
                {
                    MMEventManager.TriggerEvent(new EGameStarted());
                    
                    DebugLogger.Log(message: $"About to unload scene: {currentScene}");
                    SceneManager.UnloadSceneAsync(currentScene.ToString());
                    // loadingScreenContainer.gameObject.SetActive(false);
                });*/
        }


        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == targetScene.ToString())
            {
                _isReadyToBurst = true;
            }
        }

        // -------- Service placeholders --------
        private IEnumerator RunServicePlaceholders()
        {
            // Start in parallel
            if (waitForRemoteContent) StartCoroutine(PrepareRemoteContent());
            if (waitForApplovin) StartCoroutine(PrepareApplovin());
            if (waitForAppsflyer) StartCoroutine(PrepareAppsflyer());

            // Wait until all enabled gates are ready
            while (!ServicesReady)
                yield return null;
        }

        #region Service Preparations

        private IEnumerator PrepareRemoteContent()
        {
            float elapsed = 0f;

#if UNITY_EDITOR
            if (simulateServicesInEditor)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                _remoteContentReady = true;
                yield break;
            }
#endif

            // TODO: Replace with real remote content logic:
            // 1) Initialize Addressables (if needed)
            // var initHandle = Addressables.InitializeAsync();
            // yield return initHandle;

            // 2) Check and update catalogs
            // var checkHandle = Addressables.CheckForCatalogUpdates(false);
            // yield return checkHandle;
            // if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result != null && checkHandle.Result.Count > 0) {
            //     var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result);
            //     yield return updateHandle;
            // }

            // 3) Optionally pre‑download critical assets or configs here
            // yield return Addressables.DownloadDependenciesAsync(keysOrLabel).Task;

            // For now, wait until an external system calls SignalRemoteContentReady(), or timeout
            while (!_remoteContentReady && elapsed < serviceInitTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_remoteContentReady)
                Debug.LogWarning("[Loading] Remote content not signaled in time. Continuing.");
            _remoteContentReady = true;
        }

        private IEnumerator PrepareApplovin()
        {
            float elapsed = 0f;

#if UNITY_EDITOR
            if (simulateServicesInEditor)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                _applovinReady = true;
                yield break;
            }
#endif

            // TODO: Initialize AppLovin SDK and wait for callback.
            // Example:
            // MaxSdkCallbacks.OnSdkInitializedEvent += cfg => { SignalApplovinReady(); };

            while (!_applovinReady && elapsed < serviceInitTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_applovinReady)
                Debug.LogWarning("[Loading] AppLovin not signaled in time. Continuing.");
            _applovinReady = true;
        }

        private IEnumerator PrepareAppsflyer()
        {
            float elapsed = 0f;

#if UNITY_EDITOR
            if (simulateServicesInEditor)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                _appsflyerReady = true;
                yield break;
            }
#endif

            // TODO: Initialize AppsFlyer and wait for callback.
            // Example:
            // AppsFlyerSDK.AppsFlyer.OnRequestResponse += (statusCode, msg) => { SignalAppsflyerReady(); };

            while (!_appsflyerReady && elapsed < serviceInitTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_appsflyerReady)
                Debug.LogWarning("[Loading] AppsFlyer not signaled in time. Continuing.");
            _appsflyerReady = true;
        }

        // -------- External signals (call these from your SDK callbacks) --------
        public void SignalRemoteContentReady() => _remoteContentReady = true;
        public void SignalApplovinReady() => _applovinReady = true;
        public void SignalAppsflyerReady() => _appsflyerReady = true;

        #endregion
    }
}
