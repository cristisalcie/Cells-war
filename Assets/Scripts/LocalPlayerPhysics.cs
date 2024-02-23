using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// TODO: Consider merging this script file with LocalPlayerBehaviour script

public class LocalPlayerPhysics : MonoBehaviour
{
    private List<LocalCellPhysics> cellsPhysics;
    public GameObject cellPrefab;

    private void Awake()
    {
        cellsPhysics = new List<LocalCellPhysics>();
    }

    private void Start()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            cellsPhysics.Add(child.GetComponent<LocalCellPhysics>());
        }
    }

    private void LateUpdate()
    {
        CameraFollow();
    }

    public void Move(Vector2 _mouseWorldPosition)
    {
        foreach (LocalCellPhysics cellPhysics in cellsPhysics)
        {
            cellPhysics.Move(_mouseWorldPosition);
        }
    }

    private void CameraFollow()
    {
        // Camera follow set to the center of all cells
        if (cellsPhysics.Count > 0)
        {
            Vector3 targetPosition = new Vector3(0, 0, 0);

            foreach (LocalCellPhysics cellPhysics in cellsPhysics)
            {
                targetPosition.x += cellPhysics.transform.position.x;
                targetPosition.y += cellPhysics.transform.position.y;
            }
            targetPosition /= cellsPhysics.Count;

            // oZ needs to be negative in order to be properly oriented towards the map
            targetPosition.z = -1;

            Camera.main.transform.position = targetPosition;
        }
    }

    public void DivideCells()
    {
        GameObject[] childrenCells = new GameObject[cellsPhysics.Count];
        int i = 0;

        foreach (LocalCellPhysics cellPhysics in cellsPhysics)
        {
            // cellPhysics.transform.localScale
            Debug.Log(cellPhysics.transform.localScale);

            // Instantiate at position (0, 0, 0) and zero rotation with this attached script as parent.
            childrenCells[i] = Instantiate(cellPrefab, new Vector3(0, 0, 0), Quaternion.identity, transform);
            ++i;
        }

        for (i = 0; i < childrenCells.Length; ++i)
        {
            if (childrenCells[i] == null)
            {
                break;
            }
            cellsPhysics.Add(childrenCells[i].GetComponent<LocalCellPhysics>());
        }
    }
}
