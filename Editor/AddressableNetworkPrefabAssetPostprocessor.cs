using FishNet.Addressable.Runtime;
using FishNet.Configuring;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Scened;
using FishNet.Object;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FishNet.Addressable.Editor
{
    public class AddressableNetworkPrefabAssetPostprocessor : AssetPostprocessor
    {
        private const string AssetPath = "Assets/AddressableNetworkPrefabHandler.asset";

        /// <summary>
        /// Called by Unity when assets are modified.
        /// </summary>
        internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            try
            {
#if PARRELSYNC
                if (ParrelSync.ClonesManager.IsClone())
                    return;
#endif
                if (Application.isPlaying)
                    return;

                /* Don't iterate if updating or compiling as that could cause an infinite loop
                 * due to the prefabs being generated during an update, which causes the update
                 * to start over, which causes the generator to run again, which... you get the idea. */
                if (EditorApplication.isCompiling)
                    return;

                AddressableNetworkPrefabHandler addressableNetworkPrefabHandler = GetOrCreateAddressableNetworkPrefabHandler();
                if (addressableNetworkPrefabHandler != null)
                {
                    if (addressableNetworkPrefabHandler.autoGennerate)
                        addressableNetworkPrefabHandler.Gennerate();
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(addressableNetworkPrefabHandler);
                }
                else
                    Debug.LogWarning($"{AssetPath} is missing");
            }
            catch
            {
                throw;
            }
        }

        [MenuItem("Tools/Fish-Networking/SetupAddressable", validate = false, priority = 50)]
        public static void SetupAddressable()
        {
            AddressableNetworkPrefabHandler addressableNetworkPrefabHandler = GetOrCreateAddressableNetworkPrefabHandler();

            GameObject activeGameObject = UnityEditor.Selection.activeGameObject;
            if (activeGameObject == null)
                throw new Exception($"No GameObject selected, Please select gameobject that contain {nameof(NetworkManager)}");

            if (!activeGameObject.TryGetComponent<NetworkManager>(out NetworkManager networkManager))
                throw new Exception("NetworkManager not found on selected GameObject");
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

        private static AddressableNetworkPrefabHandler GetOrCreateAddressableNetworkPrefabHandler()
        {
            AddressableNetworkPrefabHandler addressableNetworkPrefabHandler = AssetDatabase.LoadAssetAtPath<AddressableNetworkPrefabHandler>(AssetPath);
            if (addressableNetworkPrefabHandler == null)
            {
                addressableNetworkPrefabHandler = ScriptableObject.CreateInstance<AddressableNetworkPrefabHandler>();
                AssetDatabase.CreateAsset(addressableNetworkPrefabHandler, AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("AddressableNetworkPrefabHandler created");
            }

            if (addressableNetworkPrefabHandler.defaultPrefabObjects == null)
            {
                addressableNetworkPrefabHandler.defaultPrefabObjects = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(Path.Combine("Assets", "DefaultPrefabObjects.asset"));
                UnityEditor.EditorUtility.SetDirty(addressableNetworkPrefabHandler);
            }
            return addressableNetworkPrefabHandler;
        }
    }
}
