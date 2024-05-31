using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class PlayerBehaviour : NetworkBehaviour
{
    private CellsPhysicsManager cellsPhysicsManager;
    private Vector2 mouseWorldPosition;

    private string playerNameString;
    private GameCanvasBehaviour gameCanvasBehaviour;

    private void Awake()
    {
        // Only the first component found will be returned
        cellsPhysicsManager = GetComponentInChildren<CellsPhysicsManager>();

        // Since I am sure there is always going to be only one GameCanvasBehaviour in the scene.
        gameCanvasBehaviour = FindObjectOfType<GameCanvasBehaviour>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (gameCanvasBehaviour.isInputPaused) return;

        // Handle input
        mouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            cellsPhysicsManager.DivideCells(mouseWorldPosition);
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        if (gameCanvasBehaviour.isInputPaused) return;

        cellsPhysicsManager.Move(mouseWorldPosition);
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer) return;

        cellsPhysicsManager.CameraFollow();
        cellsPhysicsManager.CameraZoom();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (string.IsNullOrEmpty(GameNetworkManager.singleton.localPlayerName))  // Name was not set by client in Lobby scene
        {
            GameNetworkManager.singleton.localPlayerName = "Player" + netIdentity.netId;
        }

        CmdSetupPlayer(GameNetworkManager.singleton.localPlayerName);
    }

    // Commands execute on server side only
    #region Commands

    [Command]
    private void CmdSetupPlayer(string _name)
    {
        // Server will also have the name set in set scene before sending it to all clients including the sender
        if (!(isServer && isClient)) // Is not host
        {
            // Necessary to avoid calling this code twice for the host. (Here and in Rpc call)
            playerNameString = _name;
            cellsPhysicsManager.SetCellsName(_name);
        }
        RpcSetPlayerName(_name); // Update all clients with the new joined player name

        // Request player data to be sent to the player that just joined
        foreach (KeyValuePair<int, NetworkConnectionToClient> connection in NetworkServer.connections)
        {
            if (netIdentity.connectionToClient == connection.Value)  // Skip current script's attached GameObject
            {
                continue;
            }
            PlayerBehaviour _playerBehaviour = connection.Value.identity.gameObject.GetComponent<PlayerBehaviour>();

            // Goes back to owner of playerBehaviour script (the player who just joined) and sets name of _playerBehaviour (existing players).
            // First parameter is sent to know which playerBehaviour to modify and second parameter is the value that needs to be set of the above
            // playerBehaviour script because the player who just joined doesn't know it but the server (where this Command is running), knows it.
            TargetSetPlayerName(_playerBehaviour, _playerBehaviour.playerNameString);
        }
    }

    #endregion

    // ClientRpcs can be executed from server side only (e.g. inside Command function)
    // Will call function on every client with parameter received from server
    // If we should exclude the owner add tag [ClientRpc(includeOwner = false)] instead
    #region ClientRpcs

    [ClientRpc]
    private void RpcSetPlayerName(string _name)
    {
        playerNameString = _name;
        cellsPhysicsManager.SetCellsName(_name);
    }

    #endregion

    // Context matters
    // If the first parameter of your TargetRpc method is a NetworkConnection then that's the connection that will receive the message regardless of context.
    // If the first parameter is any other type, then the owner client of the object with the script containing your TargetRpc will receive the message.
    #region TargetRpcs

    /// <summary> Used on CmdSetupPlayer() in PlayerScript to gather existing modified data of all joined players </summary>
    /// <param name="_target"> The GameObject identity to be modified (will have the values of the local scene, not the ones sent from server) </param>
    /// <param name="_playerNameString"> The string of playerNameString that needs to be changed to in _target gameobject </param>
    [TargetRpc]
    private void TargetSetPlayerName(PlayerBehaviour _target, string _playerNameString)
    {
        _target.playerNameString = _playerNameString;
        _target.cellsPhysicsManager.SetCellsName(_playerNameString);
    }

    #endregion

}
