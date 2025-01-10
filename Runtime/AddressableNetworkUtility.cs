using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

namespace FishNet.Addressable.Runtime
{
    public static class AddressableNetworkUtility
    {
        /// <summary>
        /// Instantiate a <see cref="NetworkObject"/> and spawn it on <see cref="ServerManager"/>
        /// If <paramref name="serverManager"/> is not specified a default <see cref="ServerManager"/> from <see cref="InstanceFinder.ServerManager"/> will be used
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentReference"></param>
        /// <param name="ownerConnection"></param>
        /// <param name="scene"></param>
        /// <param name="serverManager"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static AsyncOperationHandle<T> InstantiateAsync<T>(this NetworkedComponentReference<T> componentReference, NetworkConnection ownerConnection =null, Scene scene = default, ServerManager serverManager = null) where T : NetworkBehaviour
        {
            try
            {
                if (componentReference is null)
                    throw new ArgumentNullException(nameof(componentReference));

                serverManager = serverManager == null ? InstanceFinder.ServerManager : serverManager;

                AsyncOperationHandle<T> asyncOperationHandle = componentReference.InstantiateAsync();
                asyncOperationHandle.Completed += AsyncOperationHandle_Completed;


                return asyncOperationHandle;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to spawn network object for {typeof(T)} key={componentReference.RuntimeKey}", e);
            }

            void AsyncOperationHandle_Completed(AsyncOperationHandle<T> handle)
            {
                AsyncOperationStatus status = handle.Status;
                switch (status)
                {
                    case AsyncOperationStatus.None:
                        throw new Exception($"AssetOperationHandle Status is not completed Status={status}");
                    case AsyncOperationStatus.Succeeded:
                        if (serverManager == null)
                            throw new NullReferenceException($"ServerManager is null when {componentReference} is loaded");
                        OnNetworkBehaviourLoaded(ownerConnection, scene, serverManager, handle.Result);
                        break;
                    case AsyncOperationStatus.Failed:
                        break;
                    default:
                        throw new NotImplementedException(status.ToString());
                }
            }
        }

        private static void OnNetworkBehaviourLoaded<T>(NetworkConnection ownerConnection, Scene scene, ServerManager serverManager, T handleResult) where T : NetworkBehaviour
        {
            if (handleResult.gameObject == null)
                throw new NullReferenceException($"AssetOperation not contain gameObject");

            if (handleResult.NetworkObject == null)
                throw new NullReferenceException($"NetworkObject is null when asset is loaded, Check your prefab");

            serverManager.Spawn(nob: handleResult.NetworkObject, ownerConnection: ownerConnection, scene: scene);
        }

        private static void OnComponentLoaded<T>(NetworkConnection ownerConnection, Scene scene, ServerManager serverManager, T handleResult) where T : UnityEngine.Component
        {
            if (handleResult.gameObject == null)
                throw new NullReferenceException($"AssetOperation not contain gameObject");

            if (!handleResult.TryGetComponent(out NetworkObject networkObject))
                throw new NullReferenceException($"NetworkObject is null when asset is loaded, Check your prefab");

            serverManager.Spawn(nob: networkObject, ownerConnection: ownerConnection, scene: scene);
        }

        /// <summary>
        /// Instantiate a <see cref="NetworkObject"/> and spawn it on <see cref="ServerManager"/>
        /// If <paramref name="serverManager"/> is not specified a default <see cref="ServerManager"/> from <see cref="InstanceFinder.ServerManager"/> will be used
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serverManager"></param>
        /// <param name="componentReference"></param>
        /// <param name="ownerConnection"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<T> SpawnAddressableAsync<T>(this ServerManager serverManager, NetworkedComponentReference<T> componentReference, NetworkConnection ownerConnection = null, Scene scene = default) where T : NetworkBehaviour
        {
            return InstantiateAsync(componentReference, ownerConnection, scene, serverManager);
        }

        /// <summary>
        /// Instantiate a <see cref="NetworkObject"/> and spawn it on <see cref="ServerManager"/>
        /// If <paramref name="serverManager"/> is not specified a default <see cref="ServerManager"/> from <see cref="InstanceFinder.ServerManager"/> will be used
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentReference"></param>
        /// <param name="ownerConnection"></param>
        /// <param name="scene"></param>
        /// <param name="serverManager"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<T> SpawnAddressableAsync<T>(this NetworkedComponentReference<T> componentReference, NetworkConnection ownerConnection = null, Scene scene = default, ServerManager serverManager = null) where T : NetworkBehaviour
        {
            return InstantiateAsync(componentReference, ownerConnection, scene, serverManager);
        }

        /// <summary>
        /// Instantiate a <see cref="NetworkObject"/> and spawn it on <see cref="ServerManager"/>
        /// If <paramref name="serverManager"/> is not specified a default <see cref="ServerManager"/> from <see cref="InstanceFinder.ServerManager"/> will be used
        /// This is Not recommended since it can be a prone to error from mismatch type and etc
        /// Try using <see cref="AddressableNetworkUtility.InstantiateAsync{T}(NetworkedComponentReference{T}, NetworkConnection, Scene, ServerManager)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentReference"></param>
        /// <param name="ownerConnection"></param>
        /// <param name="scene"></param>
        /// <param name="serverManager"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static AsyncOperationHandle<T> InstantiateAsync<T>(this ComponentReference<T> componentReference, NetworkConnection ownerConnection = null, Scene scene = default, ServerManager serverManager = null) where T : UnityEngine.Component
        {
            AsyncOperationHandle<T> asyncOperationHandle = componentReference.InstantiateAsync();
            asyncOperationHandle.Completed += AsyncOperationHandle_Completed;


            return asyncOperationHandle;

            void AsyncOperationHandle_Completed(AsyncOperationHandle<T> handle)
            {
                AsyncOperationStatus status = handle.Status;
                switch (status)
                {
                    case AsyncOperationStatus.None:
                        throw new Exception($"AssetOperationHandle Status is not completed Status={status}");
                    case AsyncOperationStatus.Succeeded:
                        if (serverManager == null)
                            throw new NullReferenceException($"ServerManager is null when {componentReference} is loaded");
                        OnComponentLoaded(ownerConnection, scene, serverManager, handle.Result);
                        break;
                    case AsyncOperationStatus.Failed:
                        break;
                    default:
                        throw new NotImplementedException(status.ToString());
                }
            }
        }

        /// <summary>
        /// Instantiate a <see cref="NetworkObject"/> and spawn it on <see cref="ServerManager"/>
        /// If <paramref name="serverManager"/> is not specified a default <see cref="ServerManager"/> from <see cref="InstanceFinder.ServerManager"/> will be used
        /// This is Not recommended since it can be a prone to error from mismatch type and etc
        /// Try using <see cref="AddressableNetworkUtility.SpawnAddressableAsync{T}(NetworkedComponentReference{T}, NetworkConnection, Scene, ServerManager)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serverManager"></param>
        /// <param name="componentReference"></param>
        /// <param name="ownerConnection"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<T> SpawnAddressableAsync<T>(this ComponentReference<T> componentReference, NetworkConnection ownerConnection = null, Scene scene = default, ServerManager serverManager = null) where T : UnityEngine.Component
        {
            return InstantiateAsync(componentReference, ownerConnection, scene, serverManager);
        }

        /// <summary>
        /// Instantiate a <see cref="NetworkObject"/> and spawn it on <see cref="ServerManager"/>
        /// If <paramref name="serverManager"/> is not specified a default <see cref="ServerManager"/> from <see cref="InstanceFinder.ServerManager"/> will be used
        /// This is Not recommended since it can be a prone to error from mismatch type and etc
        /// Try using <see cref="AddressableNetworkUtility.SpawnAddressableAsync{T}(ServerManager, NetworkedComponentReference{T}, NetworkConnection, Scene)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentReference"></param>
        /// <param name="serverManager"></param>
        /// <param name="ownerConnection"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<T> SpawnAddressableAsync<T>(this ServerManager serverManager, ComponentReference<T> componentReference, NetworkConnection ownerConnection = null, Scene scene = default) where T : NetworkBehaviour
        {
            return InstantiateAsync(componentReference, ownerConnection, scene, serverManager);
        }
    }
}