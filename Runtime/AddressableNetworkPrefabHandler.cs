using FishNet.Managing.Object;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace FishNet.Addressable.Runtime
{
    [CreateAssetMenu(fileName = "AddressableNetworkPrefabHandler", menuName = "FishNet/Spawnable Prefabs/AddressableNetworkPrefabHandler")]
    public class AddressableNetworkPrefabHandler : SinglePrefabObjects
    {
        [Tooltip("Assign default defaultPrefabObjects from FishNet, Which usually at Assets/DefaultPrefabObjects.asset")]
        public DefaultPrefabObjects defaultPrefabObjects;
        public bool autoGennerate = true;

        [Tooltip("Auto preload addressable prefab when spawnable prefab get initilize")]
        /// <summary>
        /// Auto preload addressable prefab when <see cref="InitializePrefabRange"/> is called
        /// </summary>
        public bool autoPreloadPrefab = true;
        [Space]
        public List<EntryWrapper> assetReferences = new List<EntryWrapper>();
        private Task preloadAssetTask;

        public override NetworkObject GetObject(bool asServer, int id)
        {
            EntryWrapper entry = assetReferences.FirstOrDefault(x => x.PrefabId == id);
            if (entry == null)
                Debug.LogException(new Exception($"PrefabId {id} not found"));
            return entry?.GetNetworkObject();
        }

        public override int GetObjectCount() => assetReferences.Count;

        public override async void InitializePrefabRange(int startIndex)
        {
            Debug.LogWarning($"InitializePrefabRange {startIndex}");
            base.InitializePrefabRange(startIndex);
            if (autoPreloadPrefab)
                await PreloadAsset();
        }

        public async Task PreloadAsset()
        {
            try
            {
                if (preloadAssetTask == null || preloadAssetTask.IsFaulted)
                    preloadAssetTask = PreloadAssetInternal();

                await preloadAssetTask;
            }
            catch
            {
                throw;
            }
        }

        private async Task PreloadAssetInternal()
        {
            try
            {
                if (Application.isPlaying)
                {
                    Debug.Log($"PreloadAddressableAsset - Start\n{string.Join("\n", assetReferences)}");
                    bool isPararell = Application.platform != RuntimePlatform.WebGLPlayer;
                    if (isPararell)
                        await Task.WhenAll(assetReferences.Select(x => x.PreloadAddressableAsset()));
                    else
                    {
                        foreach (EntryWrapper item in assetReferences)
                            await item.PreloadAddressableAsset();
                    }

                    Debug.Log($"PreloadAddressableAsset - Complete\n{string.Join("\n", assetReferences)}");
                }
                else
                {
                    Debug.LogError("PreloadAsset should be done in play mode");
                }
            }
            catch
            {
                throw;
            }
        }

        public override void RemoveNull()
        {
            Debug.LogWarning($"RemoveNull");
            base.RemoveNull();

            for (int i = 0; i < assetReferences.Count; i++)
            {
                if (assetReferences[i] == null)
                {
                    assetReferences.RemoveAt(i);
                    i--;
                }
            }
        }

        [ContextMenu(nameof(Gennerate))]
        public void Gennerate()
        {
#if UNITY_EDITOR
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

                if (this == null)
                    return;

                if (defaultPrefabObjects == null)
                    throw new NullReferenceException("defaultPrefabObjects not exist");

                for (int i = 0; i < assetReferences.Count; i++)
                {
                    if (assetReferences[i] == null || assetReferences[i].NonAddressableNob == null)
                    {
                        assetReferences.RemoveAt(i);
                        i--;
                    }
                }
                foreach (NetworkObject nob in defaultPrefabObjects.Prefabs)
                {
                    EntryWrapper item = assetReferences.Find(x => x.NonAddressableNob == nob);
                    if (item == null)
                    {
                        item = new EntryWrapper(nob, CollectionId);
                        assetReferences.Add(item);
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
                foreach (EntryWrapper item in assetReferences)
                {
                    AssetDatabase.SaveAssetIfDirty(item.NonAddressableNob);
                }
                Debug.Log($"{nameof(AddressableNetworkPrefabHandler)} found {assetReferences.Count} prefabs");
            }
            catch
            {
                throw;
            }
#endif
        }

        [Serializable]
        public class EntryWrapper
        {
            [SerializeField] private ushort prefabId;
            [SerializeField] private string assetPath;
            [SerializeField] private bool isAddressable;
            [SerializeField] private AssetReferenceNetworkObject assetReference;
            [SerializeField] private AsyncOperationHandle<NetworkObject> handle;
            [SerializeField] private AsyncOperationStatus status = AsyncOperationStatus.None;
            [SerializeField] private NetworkObject nonAddressableNob;

            public EntryWrapper(NetworkObject nonAddressableNob, ushort collectionId)
            {
                CollectionId = collectionId;
                NonAddressableNob = nonAddressableNob;

                EditorValidate();
            }

            public bool IsAddressable { get => isAddressable; private set => isAddressable = value; }
            public ushort CollectionId { get; private set; }
            public ushort PrefabId { get => prefabId; private set => prefabId = value; }
            public string Guid { get; private set; }
            public NetworkObject NonAddressableNob { get => nonAddressableNob; private set => nonAddressableNob = value; }
            public AssetReferenceNetworkObject AssetReference { get => assetReference; private set => assetReference = value; }
            public string AssetPath { get => assetPath; private set => assetPath = value; }

            public NetworkObject GetNetworkObject()
            {
                return IsAddressable ? handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null : NonAddressableNob;
            }

            public Task PreloadAddressableAsset()
            {
                try
                {
                    Debug.Log($"PreloadAddressableAsset {PrefabId} {AssetPath}");
                    if (!IsAddressable)
                        return Task.CompletedTask;
                    if (!handle.IsValid())
                    {
                        handle = AssetReference.LoadAssetAsync();
                        handle.Completed += Handle_Completed;
                    }
                    return handle.Task;

                    void Handle_Completed(AsyncOperationHandle<NetworkObject> handle)
                    {
                        try
                        {
                            status = handle.Status;
                            if (handle.Status == AsyncOperationStatus.Succeeded)
                                ManagedObjects.InitializePrefab(handle.Result, PrefabId, CollectionId);
                            else
                                Debug.LogError("Fail Getting NetworkObject from addressable");
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return Task.CompletedTask;
                }
            }

            public override string ToString()
            {
                return $"PrefabId={PrefabId}\tIsAddressable={IsAddressable}\tStatus={status}\tHandleValid={handle.IsValid()}\tAssetPath={AssetPath}";
            }

            private void EditorValidate()
            {
#if UNITY_EDITOR
                AssetPath = AssetDatabase.GetAssetPath(NonAddressableNob);
                string guid = AssetDatabase.AssetPathToGUID(AssetPath, AssetPathToGUIDOptions.OnlyExistingAssets);
                PrefabId = AssetPath.GetStableHashU16();
                AddressableAssetEntry addressableAssetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid, true);
                IsAddressable = addressableAssetEntry != null;

                Guid = guid;
                if (IsAddressable)
                    AssetReference = new AssetReferenceNetworkObject(guid);
                //Debug.Log($"{PrefabId} {AssetPath}");
                EditorUtility.SetDirty(NonAddressableNob);
                ManagedObjects.InitializePrefab(NonAddressableNob, PrefabId, CollectionId);
                //Debug.Log($"Result {NonAddressableNob.PrefabId} {AssetPath}");
#endif
            }

            public AsyncOperationHandle<NetworkObject> GetAsyncOperationHandle() => IsAddressable ? handle : throw new InvalidOperationException("This Entry is not addressable or loading is not begin");
        }
    }

    [Serializable]
    public class AssetReferenceNetworkObject : ComponentReference<NetworkObject>
    {
        public AssetReferenceNetworkObject(string guid) : base(guid)
        {
        }
    }
}
