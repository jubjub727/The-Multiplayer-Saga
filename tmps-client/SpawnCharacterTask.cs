using Networking;
using OMP.LSWTSS.CApi1;

namespace tmpsclient;

public enum CharacterSpawnState
{
    ReadyToStart,
    LoadingResource,
    ResourceLoaded,
    CharacterSpawned
}

public class SpawnCharacterTask
{
    public nttSceneGraphResourceHandle.Handle CharacterPrefabResourceHandle;
    public NetworkedPlayer Player;
    public CharacterSpawnState State;
    public int Identifier;

    public SpawnCharacterTask(NetworkedPlayer player)
    {
        Player = player;
        State = CharacterSpawnState.ReadyToStart;
        Identifier = CharacterSpawnManager.IdentifierIndex;
        CharacterSpawnManager.IdentifierIndex++;
    }
}
