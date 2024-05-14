using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : NetworkBehaviour
{
    private CellsPhysicsManager cellsPhysicsManager;
    private Vector2 mouseWorldPosition;

    private void Awake()
    {
        cellsPhysicsManager = null;
    }

    void Start()
    {
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
}
