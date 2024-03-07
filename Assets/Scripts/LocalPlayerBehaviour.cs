using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerBehaviour : MonoBehaviour
{
    private LocalCellsPhysicsManager localCellsPhysicsManager;
    private Vector2 mouseWorldPosition;

    private void Awake()
    {
        localCellsPhysicsManager = null;
    }

    void Start()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.name == "Local Cells")
            {
                localCellsPhysicsManager = child.GetComponent<LocalCellsPhysicsManager>();
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
            localCellsPhysicsManager.DivideCells(mouseWorldPosition);
        }
    }

    private void FixedUpdate()
    {
        localCellsPhysicsManager.Move(mouseWorldPosition);
    }
}
