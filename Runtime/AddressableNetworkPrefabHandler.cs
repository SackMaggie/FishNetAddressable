using FishNet.Managing.Object;
using FishNet.Object;
using GameKit.Dependencies.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace FishNet.Addressable.Runtime
{
    [CreateAssetMenu(fileName = "AddressableNetworkPrefabHandler", menuName = "FishNet/Spawnable Prefabs/AddressableNetworkPrefabHandler")]
    public class AddressableNetworkPrefabHandler : SinglePrefabObjects
    {
        private const string DefaultPrefabHandlerAssetPath = "Assets/AddressableNetworkPrefabHandler.asset";
        public static AddressableNetworkPrefabHandler Instance => GetOrCreateAddressableNetworkPrefabHandler();
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
            try
            {
                Debug.LogWarning($"InitializePrefabRange {startIndex}");
                base.InitializePrefabRange(startIndex);
                if (autoPreloadPrefab)
                    await PreloadAsset();
            }
            catch (Exception e)
            {
                throw new Exception($"AddressableNetworkPrefabHandler.InitializePrefabRange Error, Object may not be loaded", e);
            }
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

        private static AddressableNetworkPrefabHandler GetOrCreateAddressableNetworkPrefabHandler()
        {
            AddressableNetworkPrefabHandler addressableNetworkPrefabHandler;
            if (Application.isEditor && !Application.isPlaying)
            {
#if UNITY_EDITOR
                if (!IsValidEditorState())
                    return null;
                addressableNetworkPrefabHandler = AssetDatabase.LoadAssetAtPath<AddressableNetworkPrefabHandler>(DefaultPrefabHandlerAssetPath);
                if (addressableNetworkPrefabHandler == null)
                {
                    addressableNetworkPrefabHandler = ScriptableObject.CreateInstance<AddressableNetworkPrefabHandler>();
                    AssetDatabase.CreateAsset(addressableNetworkPrefabHandler, DefaultPrefabHandlerAssetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log("AddressableNetworkPrefabHandler created");
                }

                if (addressableNetworkPrefabHandler.defaultPrefabObjects == null)
                {
                    ///Path for DefaultPrefabObjects.asset which stored in <see cref="FishNet.Configuring.PrefabGeneratorConfigurations"/>
                    ///<see cref="FishNet.Configuring.PrefabGeneratorConfigurations.DefaultPrefabObjectsPath_Platform"/>
                    ///Which is an internal access, so we need to manually set it
                    string assetPath = Path.Combine("Assets", "DefaultPrefabObjects.asset");
                    addressableNetworkPrefabHandler.defaultPrefabObjects = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(assetPath);
                    UnityEditor.EditorUtility.SetDirty(addressableNetworkPrefabHandler);
                }
#else
                addressableNetworkPrefabHandler = null;
#endif
            }
            else
            {
                addressableNetworkPrefabHandler = Resources.Load<AddressableNetworkPrefabHandler>(DefaultPrefabHandlerAssetPath);
            }

            return addressableNetworkPrefabHandler;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Fish-Networking/Addressable/Gennerate", validate = false)]
        public static void Gennerate()
        {
            try
            {
                if (!IsValidEditorState())
                    return;

                AddressableNetworkPrefabHandler prefabHandlerInstance = Instance;
                if (prefabHandlerInstance == null)
                    return;

                DefaultPrefabObjects defaultPrefabObjects = prefabHandlerInstance.defaultPrefabObjects;
                List<EntryWrapper> assetReferences = prefabHandlerInstance.assetReferences;
                ushort CollectionId = prefabHandlerInstance.CollectionId;

                if (defaultPrefabObjects == null)
                    throw new NullReferenceException("defaultPrefabObjects not exist");

                for (int i = 0; i < assetReferences.Count; i++)
                {
                    if (assetReferences[i] == null || assetReferences[i].NetworkObjectPrefab == null)
                    {
                        assetReferences.RemoveAt(i);
                        i--;
                    }
                }
                foreach (NetworkObject nob in defaultPrefabObjects.Prefabs)
                {
                    if (nob == null)
                        continue;
                    EntryWrapper item = assetReferences.Find(x => x.NetworkObjectPrefab == nob);
                    if (item == null)
                    {
                        item = new EntryWrapper(nob, CollectionId);
                        assetReferences.Add(item);
                        UnityEditor.EditorUtility.SetDirty(prefabHandlerInstance);
                    }
                    item.Validate();
                }
                foreach (EntryWrapper item in assetReferences)
                {
                    AssetDatabase.SaveAssetIfDirty(item.NetworkObjectPrefab);
                }
                Debug.Log($"{nameof(AddressableNetworkPrefabHandler)} found {assetReferences.Count} prefabs");
            }
            catch
            {
                throw;
            }
        }
#endif

#if UNITY_EDITOR
        [MenuItem("Tools/Fish-Networking/Addressable/ForceSetPrefabId", validate = false)]
        public static void ForceSetPrefabId()
        {
            if (!IsValidEditorState())
                return;

            foreach (EntryWrapper item in Instance.assetReferences)
            {
                NetworkObject networkObjectPrefab = item.NetworkObjectPrefab;
                ushort prefabId = networkObjectPrefab.PrefabId;
                ManagedObjects.InitializePrefab(networkObjectPrefab, prefabId, Instance.CollectionId);
            }
        }
#endif

#if UNITY_EDITOR
        public static bool IsValidEditorState()
        {
#if PARRELSYNC
            if (ParrelSync.ClonesManager.IsClone())
                return false;
#endif
            if (Application.isPlaying)
                return false;

            if (!Application.isEditor)
                return false;
#if UNITY_EDITOR
            if (EditorApplication.isCompiling)
                return false;

            if (EditorApplication.isUpdating)
                return false;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            return true;
#else
            return false;
#endif
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
        [FormerlySerializedAs("nonAddressableNob")][SerializeField] private NetworkObject networkObjectPrefab;

        public EntryWrapper(NetworkObject networkObjectPrefab, ushort collectionId)
        {
            CollectionId = collectionId;
            NetworkObjectPrefab = networkObjectPrefab;
        }

        public bool IsAddressable { get => isAddressable; private set => isAddressable = value; }
        public ushort CollectionId { get; private set; }
        public ushort PrefabId { get => prefabId; private set => prefabId = value; }
        public string Guid { get; private set; }
        [Obsolete("Renamed to NetworkObjectPrefab", true)]
        public NetworkObject NonAddressableNob => NetworkObjectPrefab;
        public NetworkObject NetworkObjectPrefab { get => networkObjectPrefab; private set => networkObjectPrefab = value; }
        public AssetReferenceNetworkObject AssetReference { get => assetReference; private set => assetReference = value; }
        public string AssetPath { get => assetPath; private set => assetPath = value; }

        public NetworkObject GetNetworkObject()
        {
            return IsAddressable ? handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null : NetworkObjectPrefab;
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

        public void Validate()
        {
            EditorValidate();
        }

        private void EditorValidate()
        {
#if UNITY_EDITOR
            if (NetworkObjectPrefab == null)
                throw new NullReferenceException("NetworkObjectPrefab is not exist");
            AssetPath = AssetDatabase.GetAssetPath(NetworkObjectPrefab);
            string guid = AssetDatabase.AssetPathToGUID(AssetPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            PrefabId = AssetPath.GetStableHashU16();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new NullReferenceException($"Attempting to access default Addressables Settings, but no settings file exists.  Open 'Window/Asset Management/Addressables/Groups' for more info.");
            AddressableAssetEntry addressableAssetEntry = settings.FindAssetEntry(guid, true);
            IsAddressable = addressableAssetEntry != null;

            Guid = guid;
            if (IsAddressable)
                AssetReference = new AssetReferenceNetworkObject(guid);
            //Debug.Log($"{PrefabId} {AssetPath}");
            EditorUtility.SetDirty(NetworkObjectPrefab);


            //Debug.Log($"Result {NonAddressableNob.PrefabId} {AssetPath}");
#endif
        }

        public AsyncOperationHandle<NetworkObject> GetAsyncOperationHandle() => IsAddressable ? handle : throw new InvalidOperationException("This Entry is not addressable or loading is not begin");
    }

    [Serializable]
    public class AssetReferenceNetworkObject : ComponentReference<NetworkObject>
    {
        public AssetReferenceNetworkObject(string guid) : base(guid)
        {
        }
    }
}
