## FishNetAddressable
The package itself is mainly for providing FishNet Networking a prefab collection that could be spawn on the network.
> Very easy to setup and remove your headache from having to figure out ``Missing prefab id XXXX`` error

> Type safe and return correct component you want which eliminate the need of ``GetComponent``

> Zero use of ambiguous and error prone of using string as a key ``Use GUID directly from Unity`` instead

# Installing
The package can be easily installed via Unity package manager
```
https://github.com/SackMaggie/FishNetAddressable.git
```
1. Open Unity Package Manager ``Window -> Package Manager``
2. Add new package using plus icon on the top left
3. Select ``Add package from Git URL...``
4. Enter the git path above
# Sample
You could clone the sample project on this repo
> https://github.com/SackMaggie/FishNetAddressableSample

# Usecase
So you want to instantiate a networked adressable prefab object with a simple class in it ?
```cs
//Your typical NetworkBehaviour class
public class AmazingCube : NetworkBehaviour
{
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Debug.Log($"I'm on networked IsClient={IsClientStarted} IsServer={IsServerStarted}");
    }
}
```
Now your got your class and make a prefab ready to use on the network
```cs
// Simple but error prone use-case of addressable
AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.InstantiateAsync("Prefabs/AmazingCube.prefab"); // what if path/key/name get changed? -> ERROR
GameObject yourGameObject = await asyncOperationHandle.Task;`
ServerManager.Spawn(yourGameObject);
AmazingCube amazingCube = yourGameObject.GetComponent<AmazingCube>(); // Have to use GetComponent and that's a prone to error since there's no promise that object will contain your AmazingCube class
// Do stuff
```
My prefered way
```cs
public AmazingCubeAssetRef amazingCubeAssetRef; // declared somewhere in your project just like a reference

// A more safer loader
AsyncOperationHandle<AmazingCube> asyncOperationHandle = amazingCubeAssetRef.SpawnAddressableAsync(); // path/key/name change doesn't affect this at all
AmazingCube amazingCube = asyncOperationHandle.Task; // You get your class right away ready to use and have editor validation for correct type internally
// Do stuff
```
Notice ``AmazingCubeAssetRef`` class?? that is another class that will have to be declared somewhere, Normally I'd just put it in the same file.
This class will provide us with a GUID based reference just like what you did when reference other stuff through out the project
```
[Serializable]
public class AmazingCubeAssetRef : NetworkedComponentReference<AmazingCube>
{
    public AmazingCubeAssetRef(string guid) : base(guid)
    {
    }
}
```
