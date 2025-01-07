#if UNITY_EDITOR
using FishNet.Addressable.Runtime;
using FishNet.Managing;
using FishNet.Managing.Scened;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FishNet.Addressable.Editor
{
    public class AddressableNetworkPrefabAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// Use this to filter out the file extensions that you want to monitor for changes.
        /// </summary>
        internal static string[] FileExtensions = new string[] {
            ".prefab",
            ".asset", //For detecting addressable group changes and defaultObject changes
        };

        /// <summary>
        /// Called by Unity when assets are modified.
        /// </summary>
        internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            try
            {
                if (!AddressableNetworkPrefabHandler.IsValidEditorState())
                    return;

                Debug.Log($"importedAssets = {string.Join("\n", importedAssets)}\ndeletedAssets = {string.Join("\n", deletedAssets)}\nmovedAssets = {string.Join("\n", movedAssets)}\nmovedFromAssetPaths = {string.Join("\n", movedFromAssetPaths)}");

                if (!IsContainChanges())
                {
                    Debug.Log("No asset changes detected");
                    return;
                }

                AddressableNetworkPrefabHandler addressableNetworkPrefabHandler = AddressableNetworkPrefabHandler.Instance;
                if (addressableNetworkPrefabHandler != null)
                {
                    if (addressableNetworkPrefabHandler.autoGennerate)
                        AddressableNetworkPrefabHandler.Gennerate();
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(addressableNetworkPrefabHandler);
                }
                else
                    Debug.LogWarning($"{nameof(AddressableNetworkPrefabHandler)} is missing");
            }
            catch
            {
                throw;
            }

            bool IsContainChanges()
            {
                foreach (string extension in FileExtensions)
                {
                    if (importedAssets.Any(x => x.EndsWith(extension)))
                        return true;
                    if (deletedAssets.Any(x => x.EndsWith(extension)))
                        return true;
                    if (movedAssets.Any(x => x.EndsWith(extension)))
                        return true;
                    if (movedFromAssetPaths.Any(x => x.EndsWith(extension)))
                        return true;
                }

                return false;
            }
        }

        [MenuItem("Tools/Fish-Networking/Addressable/Setup", validate = false)]
        public static void SetupAddressable()
        {
            if (!AddressableNetworkPrefabHandler.IsValidEditorState())
                return;

            GameObject activeGameObject = UnityEditor.Selection.activeGameObject;
            if (activeGameObject == null)
                throw new Exception($"No GameObject selected, Please select gameobject that contain {nameof(NetworkManager)} component");

            if (!activeGameObject.TryGetComponent<NetworkManager>(out NetworkManager networkManager))
                throw new Exception("NetworkManager not found on selected GameObject");

            AddressableNetworkPrefabHandler addressableNetworkPrefabHandler = AddressableNetworkPrefabHandler.Instance;
            if (networkManager.SpawnablePrefabs != addressableNetworkPrefabHandler)
            {
                networkManager.SpawnablePrefabs = addressableNetworkPrefabHandler;
                UnityEditor.EditorUtility.SetDirty(networkManager);
                Debug.LogWarning($"{nameof(networkManager.SpawnablePrefabs)} has been set to {nameof(AddressableNetworkPrefabHandler)}", networkManager);
            }

            if (!activeGameObject.TryGetComponent<SceneManager>(out SceneManager sceneManager))
            {
                sceneManager = activeGameObject.AddComponent<SceneManager>();
                UnityEditor.EditorUtility.SetDirty(networkManager);
                Debug.LogWarning($"{nameof(SceneManager)} has been added to {activeGameObject.name}", activeGameObject);
            }

            if (!activeGameObject.TryGetComponent<AddressableSceneProcessor>(out AddressableSceneProcessor addressableSceneProcessor))
            {
                addressableSceneProcessor = activeGameObject.AddComponent<AddressableSceneProcessor>();
                UnityEditor.EditorUtility.SetDirty(networkManager);
                Debug.LogWarning($"{nameof(AddressableSceneProcessor)} has been added to {activeGameObject.name}", activeGameObject);
            }

            SceneProcessorBase sceneProcessorBase = sceneManager.GetSceneProcessor();
            if (sceneProcessorBase == null || sceneProcessorBase != addressableSceneProcessor)
            {
                sceneManager.SetSceneProcessor(addressableSceneProcessor);
                UnityEditor.EditorUtility.SetDirty(networkManager);
            }

            UnityEditor.AssetDatabase.SaveAssetIfDirty(networkManager);
        }
    }
}

#endif