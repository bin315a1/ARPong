using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class TempNetManager : NetworkManager
{
    // This class inherits NetworkManager and overrides a few functions
    // so that we can have 2 player prefabs.

    // configuration
    [SerializeField] GameObject m_PlayerPrefab1;
    [SerializeField] GameObject m_PlayerPrefab2;
    [SerializeField] int numConnected = 0;

    // properties
    public GameObject PlayerPrefab1 { get { return m_PlayerPrefab1; } set { m_PlayerPrefab1 = value; } }
    public GameObject PlayerPrefab2 { get { return m_PlayerPrefab2; } set { m_PlayerPrefab2 = value; } }
    public GameObject P1Location;
    public GameObject P2Location;

    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);

        // always remember to register prefabs before spawning them.
        ClientScene.RegisterPrefab(m_PlayerPrefab1);
        ClientScene.RegisterPrefab(m_PlayerPrefab2);

        Debug.Log("Registered player prefab #" + numConnected);
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        if (m_PlayerPrefab1 == null)
        {
            if (LogFilter.logError) { Debug.LogError("The PlayerPrefab1 is empty on the NetworkManager. Please setup a PlayerPrefab object."); }
            return;
        }

        if (m_PlayerPrefab2 == null)
        {
            if (LogFilter.logError) { Debug.LogError("The PlayerPrefab2 is empty on the NetworkManager. Please setup a PlayerPrefab object."); }
            return;
        }

        if (m_PlayerPrefab1.GetComponent<NetworkIdentity>() == null)
        {
            if (LogFilter.logError) { Debug.LogError("The PlayerPrefab1 does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab."); }
            return;
        }

        if (m_PlayerPrefab2.GetComponent<NetworkIdentity>() == null)
        {
            if (LogFilter.logError) { Debug.LogError("The PlayerPrefab2 does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab."); }
            return;
        }

        if (playerControllerId < conn.playerControllers.Count && conn.playerControllers[playerControllerId].IsValid && conn.playerControllers[playerControllerId].gameObject != null)
        {
            if (LogFilter.logError) { Debug.LogError("There is already a player at that playerControllerId for this connections."); }
            return;
        }

        GameObject player;

        if (numConnected % 2 == 0)
        {
            player = (GameObject)Instantiate(m_PlayerPrefab1, P1Location.transform.position, Quaternion.identity);
            numConnected++;
        }
        else if (numConnected % 2 == 1)
        {
            player = (GameObject)Instantiate(m_PlayerPrefab2, P2Location.transform.position, Quaternion.identity);
            numConnected++;
        }
        else
        {
            if (LogFilter.logError) { Debug.LogError("The max allowed connections is 2!"); }
            return;
        }

        NetworkServer.Spawn(player);
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }


    //public override void OnStartClient(NetworkClient client)
    //{
    //    base.OnStartClient(client);

    //    // always remember to register prefabs before spawning them.
    //    foreach (GameObject go in playerCharacterPrefabs)
    //        ClientScene.RegisterPrefab(go);

    //    Debug.Log("Connect to a new game");

    //}

    //public override void OnClientConnect(NetworkConnection conn)
    //{
    //    base.OnClientConnect(conn);
    //}

    //public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    //{
    //    GameObject newPlayer = GameObject.Instantiate(playerCharacterPrefabs[playerControllerId % 2]);
    //    newPlayer.transform.position = Vector3.zero + Vector3.right * playerControllerId;
    //    NetworkServer.Spawn(newPlayer);

    //    // object spawned via this function will be a local player
    //    // which belongs to the client connection who called the ClientScene.AddPlayer
    //    NetworkServer.AddPlayerForConnection(conn, newPlayer, playerControllerId);
    //}

}