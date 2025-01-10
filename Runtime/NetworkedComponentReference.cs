using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FishNet.Addressable.Runtime
{
    [Serializable]
    public class NetworkedComponentReference<TComponent> : ComponentReference<TComponent> where TComponent : NetworkBehaviour
    {
        public NetworkedComponentReference(string guid) : base(guid)
        {
        }
    }
}