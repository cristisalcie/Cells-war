using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PlayerBehaviour : NetworkBehaviour
{
    private CellsPhysicsManager cellsPhysicsManager;
    private Vector2 mouseWorldPosition;

    [SyncVar(hook = nameof(OnNameChanged))]
    private string playerNameString;

    private void Awake()
    {
        cellsPhysicsManager = null;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.name == "Cells")
            {
                cellsPhysicsManager = child.GetComponent<CellsPhysicsManager>();
                break;
            }
        }
    }

    void Update()
    {
        // Handle input
        mouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            cellsPhysicsManager.DivideCells(mouseWorldPosition);
        }
    }

    private void FixedUpdate()
    {
        cellsPhysicsManager.Move(mouseWorldPosition);
    }





    #region Hook functions

    private void OnNameChanged(string _oldName, string _newName)
    {
        cellsPhysicsManager.SetCellsName(_newName);
    }

    #endregion

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Lock cursor on window blocked in the center.
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        string _name = "Player" + netIdentity.netId;
        if (string.IsNullOrEmpty(GameNetworkManager.singleton.localPlayerName))  // Name was not set by client in Lobby scene
        {
            GameNetworkManager.singleton.localPlayerName = _name;
        }
        else  // Name was set by client in Lobby scene
        {
            _name = GameNetworkManager.singleton.localPlayerName;
        }

        // Setting playerNameString will call OnNameChanged hook
        playerNameString = _name;
    }
}
