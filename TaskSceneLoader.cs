using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NamPhuThuy.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NamPhuThuy.SceneManage
{
    public class TaskSceneLoader : MonoBehaviour
    {
        [Header("Flags")]
        [Tooltip("If true, use asynchronous scene loading.")]
        public bool useAsyncLoad = true;
        [SerializeField] private SceneManageConst.SceneName targetScene = SceneManageConst.SceneName.None;

        [Tooltip("Drag components here that implement ITaskProvider (you can drag the component from a GameObject).")]
        [SerializeField]
        private List<MonoBehaviour> taskProviders = new List<MonoBehaviour>();

        private readonly List<Task> _tasks = new List<Task>();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private readonly object _lock = new object();

        // Register an existing Task
        public void Register(Task t)
        {
            if (t == null) return;
            lock (_lock) { _tasks.Add(t); }
        }

        // Register a task factory and start it immediately
        public void Register(Func<Task> taskFactory)
        {
            if (taskFactory == null) return;
            Register(taskFactory());
        }

        // Call this to wait for all registered tasks (at the time of call) then load scene.
        // This now also collects tasks from the inspector list of ITaskProvider components.
        public void WaitAllAndLoadScene(string sceneName)
        {
            // Collect tasks from providers currently in the inspector list
            foreach (var mb in taskProviders)
            {
                if (mb is ITaskProvider provider)
                {
                    try
                    {
                        Register(provider.CreateTask());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Provider threw when creating task: {ex}");
                    }
                }
            }

            Task[] tasksToWait;
            lock (_lock)
            {
                tasksToWait = _tasks.ToArray();
                _tasks.Clear();
            }

            _ = WaitAndEnqueueSceneLoad(tasksToWait, sceneName);
        }

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }

        private async Task WaitAndEnqueueSceneLoad(Task[] tasks, string sceneName)
        {
            if (tasks != null && tasks.Length > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"One or more tasks failed: {ex}");
                    // still proceed to load the scene
                }
            }

            _mainThreadActions.Enqueue(() =>
            {
                if (useAsyncLoad)
                    SceneManager.LoadSceneAsync(sceneName);
                else
                    SceneManager.LoadScene(sceneName);
            });
        }

        #region Editor Methods

        public void ResetValues()
        {
            
        }

        #endregion
    }

    /*#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TaskSceneLoader))]
    public class TaskSceneLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (TaskSceneLoader)target;
            if (script == null) return;

            // Inspect the provider list and warn about invalid entries
            SerializedProperty prop = serializedObject.FindProperty("taskProviders");
            if (prop != null)
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(prop, true);
                serializedObject.ApplyModifiedProperties();
            }

            // Validate entries at runtime and show warnings
            var so = serializedObject.targetObject;
            var loader = (TaskSceneLoader)so;
            if (loader != null)
            {
                if (loader is TaskSceneLoader)
                {
                    foreach (var mb in GetTaskProviders(loader))
                    {
                        if (mb != null && !(mb is ITaskProvider))
                        {
                            EditorGUILayout.HelpBox(
                                $"Assigned component '{mb.GetType().Name}' does not implement ITaskProvider. Only components implementing ITaskProvider will be used.",
                                MessageType.Warning);
                        }
                    }
                }
            }
        }

        private MonoBehaviour[] GetTaskProviders(TaskSceneLoader loader)
        {
            // Use reflection to read the private serialized list (fallback safe access)
            var field = typeof(TaskSceneLoader).GetField("taskProviders", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null) return new MonoBehaviour[0];
            var arr = field.GetValue(loader) as System.Collections.Generic.List<MonoBehaviour>;
            if (arr == null) return new MonoBehaviour[0];
            return arr.ToArray();
        }
    }
    #endif*/
}
