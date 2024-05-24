using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellsPhysicsManager : MonoBehaviour
{
    private List<CellPhysics> cellsPhysics;
    public GameObject cellPrefab;
    private const float cameraZoomMultiplier = 0.65f;
    private const float minSizeMultiplier = 0.5f;
    private const float stepSizeMultiplier = 10; // how quick can we reach the minSizeMultiplier
    private readonly Vector2 minScale = Vector2.one;

    private void Awake()
    {
        cellsPhysics = new List<CellPhysics>();

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject _child = gameObject.transform.GetChild(i).gameObject;
            cellsPhysics.Add(_child.GetComponent<CellPhysics>());
        }
    }

    public void Move(Vector2 _toWorldPosition)
    {
        foreach (CellPhysics _cellPhysics in cellsPhysics)
        {
            if (_cellPhysics.stopUserInputMovement) continue;
            _cellPhysics.Move(_toWorldPosition);
        }
    }

    public void CameraFollow()
    {
        if (cellsPhysics.Count <= 0) return;

        // Camera follow = set to the center of all cells
        Vector3 targetPosition = new Vector3(0, 0, 0);

        foreach (CellPhysics _cellPhysics in cellsPhysics)
        {
            targetPosition.x += _cellPhysics.transform.position.x;
            targetPosition.y += _cellPhysics.transform.position.y;
        }
        targetPosition /= cellsPhysics.Count;

        // oZ needs to be negative in order to be properly oriented towards the map
        targetPosition.z = -1;

        Camera.main.transform.position = targetPosition;
    }

    public void CameraZoom()
    {
        if (cellsPhysics.Count <= 0) return;

        float _newOrthographicSize = 0;

        if (cellsPhysics.Count == 1)
        {
            float _sizeMultiplier = Mathf.Max(minSizeMultiplier, minScale.x - (cellsPhysics[0].transform.localScale.x - minScale.x) / stepSizeMultiplier);
            float _cellPerimeter = (float)(2 * Math.PI * cellsPhysics[0].transform.localScale.x / 2.0f);
            _newOrthographicSize = _cellPerimeter * cameraZoomMultiplier * _sizeMultiplier;
        }
        else
        {
            foreach (CellPhysics _cellPhysics in cellsPhysics)
            {
                float _cellRadius = _cellPhysics.transform.localScale.x / 2.0f;
                float _cellPerimeter = (float)(2.0 * Math.PI * _cellRadius);
                float _cellDistanceToCamera = Vector2.Distance(Camera.main.transform.position, _cellPhysics.transform.position);
                float _borderOfCellDistanceToCamera = Mathf.Max(0, _cellDistanceToCamera - _cellRadius);

                // Choose the maximum zoom based on radius of cell plus distance from border of cell to camera
                _newOrthographicSize = Mathf.Max(_newOrthographicSize, _borderOfCellDistanceToCamera + _cellPerimeter * cameraZoomMultiplier);
            }
        }

        Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, _newOrthographicSize, Time.deltaTime);
    }

    private bool CanDivideCell(Vector3 _newScale)
    {
        return _newScale.x >= minScale.x && _newScale.y >= minScale.y;
    }

    public void DivideCells(Vector2 _toWorldPosition)
    {
        GameObject[] _childrenCells = new GameObject[cellsPhysics.Count];
        int i = 0;

        foreach (CellPhysics _cellPhysics in cellsPhysics)
        {
            Vector3 _newScale = _cellPhysics.transform.localScale / 2;

            if (!CanDivideCell(_newScale))
            {
                continue;
            }

            // Delay addition of new cell into main cell list through a local list.
            _childrenCells[i] = Instantiate(cellPrefab, _cellPhysics.transform.position, Quaternion.identity, transform);

            CellPhysics _childCellPhysics = _childrenCells[i].GetComponent<CellPhysics>();

            _cellPhysics.IncrementChildCellsNumber();

            _cellPhysics.transform.localScale = _newScale;
            _childCellPhysics.transform.localScale = _newScale;

            _childCellPhysics.parentCellPhysics = _cellPhysics;
            _childCellPhysics.SetPlayerPhysics(this);
            _childCellPhysics.GetComponent<CircleCollider2D>().isTrigger = true;
            _childCellPhysics.SetCellName(_cellPhysics.GetCellName());
            ++i;
        }

        for (i = 0; i < _childrenCells.Length; ++i)
        {
            if (_childrenCells[i] == null)
            {
                break;
            }

            CellPhysics _childCellPhysics = _childrenCells[i].GetComponent<CellPhysics>();

            // Add to cellsPhysics list in order to achieve normal cell behaviour.
            cellsPhysics.Add(_childCellPhysics);

            _childCellPhysics.ApplyDivisionForce(_toWorldPosition);

            _childCellPhysics.StartMergeBackTimer();
        }
    }

    public void MergeBackCell(CellPhysics _cellPhysics)
    {
        CellPhysics _parentCell = _cellPhysics.parentCellPhysics;

        // Calculate new scale for parent cell
        Vector3 _newLocalScale = new Vector3(_cellPhysics.transform.localScale.x, _cellPhysics.transform.localScale.y, _cellPhysics.transform.localScale.z);

        _newLocalScale += _parentCell.transform.localScale;

        _parentCell.transform.localScale = _newLocalScale;

        // Remove from list of cells
        cellsPhysics.Remove(_cellPhysics);

        // Destroy child cell
        Destroy(_cellPhysics.gameObject);

        // Child destroyed, decrement parent number of childs
        _parentCell.DecrementChildCellsNumber();

        // Allow movement for parent again
        _parentCell.stopUserInputMovement = false;
    }

    public void SetCellsName(string name)
    {
        foreach (CellPhysics _cellPhysics in cellsPhysics)
        {
            _cellPhysics.SetCellName(name);
        }
    }
}
