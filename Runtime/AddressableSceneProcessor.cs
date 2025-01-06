using FishNet.Managing.Scened;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace FishNet.Addressable.Runtime
{
    public class AddressableSceneProcessor : DefaultSceneProcessor
    {
        private readonly List<AsyncOperationHandle<SceneInstance>> asyncOperationHandleDict = new List<AsyncOperationHandle<SceneInstance>>();

        public override void BeginLoadAsync(string sceneName, LoadSceneParameters parameters)
        {
            Debug.Log($"Addressables.LoadSceneAsync {sceneName}");
            AsyncOperationHandle<SceneInstance> asyncOperationHandle = Addressables.LoadSceneAsync(sceneName, parameters, false);
            asyncOperationHandleDict.Add(asyncOperationHandle);

            asyncOperationHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    SceneInstance sceneInstance = handle.Result;
                    Debug.Log($"Addressables.LoadSceneAsync {sceneInstance.Scene.name} {sceneInstance.Scene.handle} finished");
                }
                else
                    Debug.LogError($"Addressables.LoadSceneAsync failed {handle.DebugName}");
            };
        }

        private static string GetSceneName(string sceneName)
        {
            Match match = Regex.Match(sceneName, @"(?<g_name>\w+)\.unity");
            if (match.Success)
                sceneName = match.Groups["g_name"].Value;
            return sceneName;
        }

        public override void BeginUnloadAsync(Scene scene)
        {
            Debug.Log($"Addressables.UnloadSceneAsync {scene.name} {scene.handle}");
            AsyncOperationHandle<SceneInstance> sceneInstanceHandle = asyncOperationHandleDict.FirstOrDefault(x => x.Result.Scene.handle == scene.handle);
            if (sceneInstanceHandle.IsValid())
            {
                asyncOperationHandleDict.Remove(sceneInstanceHandle);
                AsyncOperationHandle<SceneInstance> asyncOperationHandle = Addressables.UnloadSceneAsync(sceneInstanceHandle);
                asyncOperationHandle.Completed += UnloadSceneAsyncOperationCompleted;
            }
            else
                Debug.Log("Not found scene to unload " + GetSceneName(scene.name));
            //base.BeginUnloadAsync(scene);
        }

        private void UnloadSceneAsyncOperationCompleted(AsyncOperationHandle<SceneInstance> obj)
        {
            Debug.Log($"Addressables.UnloadSceneAsync {obj.Result.Scene.name} completed");
        }

        public override bool IsPercentComplete()
        {
            return GetPercentComplete() >= 1f;
        }

        public override float GetPercentComplete()
        {
            IEnumerable<AsyncOperationHandle<SceneInstance>> enumerable = asyncOperationHandleDict.Where(x => x.IsValid());
            int count = enumerable.Count();
            return count > 0 ? enumerable.Sum(x => x.PercentComplete) / count : 1f;
        }

        public override IEnumerator AsyncsIsDone()
        {
            yield return new WaitUntil(() => asyncOperationHandleDict.Where(x => x.IsValid()).All(x => x.IsDone));
        }

        public override void ActivateLoadedScenes()
        {
            foreach (AsyncOperationHandle<SceneInstance> item in asyncOperationHandleDict)
            {
                item.Completed -= Scene_Completed;
                item.Completed += Scene_Completed;
            }

            void Scene_Completed(AsyncOperationHandle<SceneInstance> obj)
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    obj.Result.ActivateAsync();
                }
            }
        }
    }
}
