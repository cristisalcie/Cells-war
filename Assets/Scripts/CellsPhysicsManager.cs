using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellsPhysicsManager : NetworkBehaviour
{
    // This is max number of divided cells including the starting cell
    public const int maxActiveCellsNumber = 10;

    public readonly CellPhysics[] cellsPhysics = new CellPhysics[maxActiveCellsNumber]; 
    private const float cameraZoomMultiplier = 0.65f;
    private const float minSizeMultiplier = 0.5f;
    private const float stepSizeMultiplier = 10; // how quick can we reach the minSizeMultiplier
    private readonly Vector2 minScale = Vector2.one;

    private void Awake()
    {
        if (gameObject.transform.childCount != maxActiveCellsNumber)
        {
            Debug.Log("Error: There are only " + gameObject.transform.childCount + " child game object cells and there should be " + maxActiveCellsNumber);
        }

        for (int i = 0; i < maxActiveCellsNumber; i++)
        {
            GameObject _child = gameObject.transform.GetChild(i).gameObject;

            cellsPhysics[i] = _child.GetComponent<CellPhysics>();
        }
    }

    public override void OnStartLocalPlayer()
    {
        /* collider.isTrigger is always going to be true for remote players connected to allow calling of OnTrigger functions set
         * hence activate it for the local player. (First cell is always index 0 in array)
         */
        cellsPhysics[0].SetEnableColliderTrigger(false);
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (gameObject.transform.childCount != maxActiveCellsNumber)
        {
            Debug.Log("Error: There are only " + gameObject.transform.childCount + " child game object cells and there should be " + maxActiveCellsNumber);
        }
    }



    #region Functions called only on CLIENT instance

    [Client]
    public void Move(Vector2 _toWorldPosition)
    {
        for (int i = 0; i < maxActiveCellsNumber; i++)
        {
            if (!cellsPhysics[i].IsSpriteEnabled()) continue;
            if (cellsPhysics[i].stopUserInputMovement) continue;
            cellsPhysics[i].Move(_toWorldPosition);
        }
    }

    [Client]
    public void CameraFollow()
    {
        // Camera follow is set to the center of all active cells
        Vector3 targetPosition = new Vector3(0, 0, 0);
        int _activeCells = 0;

        for (int i = 0; i < maxActiveCellsNumber; i++)
        {
            if (!cellsPhysics[i].IsSpriteEnabled()) continue;

            targetPosition.x += cellsPhysics[i].transform.position.x;
            targetPosition.y += cellsPhysics[i].transform.position.y;

            ++_activeCells;
        }
        targetPosition /= _activeCells;

        // oZ needs to be negative in order to be properly oriented towards the map
        targetPosition.z = -1;

        Camera.main.transform.position = targetPosition;
    }

    [Client]
    public void CameraZoom()
    {
        float _newOrthographicSize = 0;
        int _numCellsActive = 0;

        for (int i = 0; i < maxActiveCellsNumber;  ++i)
        {
            if (!cellsPhysics[i].IsSpriteEnabled()) continue;
            {
                ++_numCellsActive;
                if (_numCellsActive >= 2) break;
            }
        }

        if (_numCellsActive < 2)
        {
            float _sizeMultiplier = Mathf.Max(minSizeMultiplier, minScale.x - (cellsPhysics[0].transform.localScale.x - minScale.x) / stepSizeMultiplier);
            float _cellPerimeter = (float)(2 * Math.PI * cellsPhysics[0].transform.localScale.x / 2.0f);
            _newOrthographicSize = _cellPerimeter * cameraZoomMultiplier * _sizeMultiplier;
        }
        else
        {
            for (int i = 0; i < maxActiveCellsNumber; ++i)
            {
                if (!cellsPhysics[i].IsSpriteEnabled()) continue;

                float _cellRadius = cellsPhysics[i].transform.localScale.x / 2.0f;
                float _cellPerimeter = (float)(2.0 * Math.PI * _cellRadius);
                float _cellDistanceToCamera = Vector2.Distance(Camera.main.transform.position, cellsPhysics[i].transform.position);
                float _borderOfCellDistanceToCamera = Mathf.Max(0, _cellDistanceToCamera - _cellRadius);

                // Choose the maximum zoom based on radius of cell plus distance from border of cell to camera
                _newOrthographicSize = Mathf.Max(_newOrthographicSize, _borderOfCellDistanceToCamera + _cellPerimeter * cameraZoomMultiplier);
            }
        }

        Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, _newOrthographicSize, Time.deltaTime);
    }

    [Client]
    private bool CanDivideCellToScale(Vector3 _newScale)
    {
        return _newScale.x >= minScale.x && _newScale.y >= minScale.y;
    }

    [Client]
    public void DivideCells(Vector2 _toWorldPosition)
    {
        for (int _parentCellIdx = 0; _parentCellIdx < maxActiveCellsNumber; _parentCellIdx++)
        {
            if (!cellsPhysics[_parentCellIdx].IsSpriteEnabled()) continue;
            if (!cellsPhysics[_parentCellIdx].canGetDividedFromInputPerspective) continue;

            Vector3 _newScale = cellsPhysics[_parentCellIdx].transform.localScale / 2;

            if (!CanDivideCellToScale(_newScale)) continue;

            // Note: This could be optimized in the future
            for (int _childCellIdx = 0; _childCellIdx < maxActiveCellsNumber; _childCellIdx++)
            {   // Find an inactive cell
                if (cellsPhysics[_childCellIdx].IsSpriteEnabled()) continue;
                {
                    cellsPhysics[_parentCellIdx].canGetDividedFromInputPerspective = false;
                    cellsPhysics[_parentCellIdx].IncrementChildCellsNumber();
                    cellsPhysics[_parentCellIdx].transform.localScale = _newScale;

                    cellsPhysics[_childCellIdx].canGetDividedFromInputPerspective = false;
                    cellsPhysics[_childCellIdx].transform.localScale = _newScale;
                    cellsPhysics[_childCellIdx].transform.position = cellsPhysics[_parentCellIdx].transform.position;
                    cellsPhysics[_childCellIdx].parentCellPhysics = cellsPhysics[_parentCellIdx];
                    cellsPhysics[_childCellIdx].isCellBeingEaten = false;
                    cellsPhysics[_childCellIdx].mergeBackTimerExpired = false;
                    cellsPhysics[_childCellIdx].ResetChildCellsNumber();
                    cellsPhysics[_childCellIdx].SetEnableCollider(true);
                    cellsPhysics[_childCellIdx].SetEnableSprite(true);
                    cellsPhysics[_childCellIdx].SetEnableNameRender(true);
                    CmdSetActiveCell(cellsPhysics[_childCellIdx], true);
                    cellsPhysics[_childCellIdx].SetEnableColliderTrigger(true);
                    cellsPhysics[_childCellIdx].ApplyDivisionForce(_toWorldPosition);
                    cellsPhysics[_childCellIdx].StartMergeBackTimer();
                    break;
                }
            }
        }
        for (int _cellIdx = 0; _cellIdx < maxActiveCellsNumber; ++_cellIdx)
        {
            cellsPhysics[_cellIdx].canGetDividedFromInputPerspective = true;
        }
    }

    [Client]
    public void MergeBackCell(CellPhysics _cellPhysics)
    {
        CellPhysics _parentCellPhysics = _cellPhysics.parentCellPhysics;

        // Calculate new scale for parent cell
        Vector3 _newLocalScale = new Vector3(_cellPhysics.transform.localScale.x, _cellPhysics.transform.localScale.y, _cellPhysics.transform.localScale.z);

        _newLocalScale += _parentCellPhysics.transform.localScale;

        _parentCellPhysics.transform.localScale = _newLocalScale;

        // Mark child cell as inactive
        _cellPhysics.SetEnableCollider(false);
        _cellPhysics.SetEnableSprite(false);
        _cellPhysics.SetEnableNameRender(false);
        CmdSetActiveCell(_cellPhysics, false);

        // Child deactivated, decrement parent number of children
        _parentCellPhysics.DecrementChildCellsNumber();

        // Resume user input for both parent and child
        _parentCellPhysics.stopUserInputMovement = false;
        _cellPhysics.stopUserInputMovement = false;

        // Disable trigger for child (is default state for inactive cell, will allow rigidbody automatic mass work)
        _cellPhysics.SetEnableColliderTrigger(false);
    }

    [Client]
    private void DoTryOrfanChildCellsOfCell(CellPhysics _cellphysics)
    {
        CellPhysics _chooseNewParentCell = null;
        for (int _childIdx = 0; _childIdx < maxActiveCellsNumber; _childIdx++)
        {
            if (_cellphysics == cellsPhysics[_childIdx].parentCellPhysics)
            {
                // Try to orfan the child. However, if the parent cell had a parent, he will not be an orfan.
                cellsPhysics[_childIdx].parentCellPhysics = _cellphysics.parentCellPhysics;

                if (cellsPhysics[_childIdx].parentCellPhysics == null)
                {
                    if (_chooseNewParentCell == null)
                    {
                        // This child now has no parent, hence his mergeBackTimerExpired must be false
                        cellsPhysics[_childIdx].mergeBackTimerExpired = false;
                        _chooseNewParentCell = cellsPhysics[_childIdx];
                    }
                    else
                    {
                        // Another cell that remained without parent, can't have this situation so set new unrelated parent
                        cellsPhysics[_childIdx].parentCellPhysics = _chooseNewParentCell;
                        _chooseNewParentCell.IncrementChildCellsNumber();
                        Debug.Log("Caught the bug!");
                    }
                }
                else
                {
                    // Announce grandparent that he has a new child.
                    cellsPhysics[_childIdx].parentCellPhysics.IncrementChildCellsNumber();

                    // Delete the found child from parent since he left it (at the end he should have 0)
                    _cellphysics.DecrementChildCellsNumber();
                }
            }
        }
    }

    [Client]
    public IEnumerator HaveCellEaten(CellPhysics _cellPhysics, CellPhysics _eaterCellPhysics)
    {
        CmdSetCellLocalScale(_cellPhysics.transform.localScale * CellPhysics.localScaleGotEatenMultiplier, _eaterCellPhysics);
        yield return null; // Wait a frame

        CellPhysics _parentCellPhysics = _cellPhysics.parentCellPhysics;

        int _activeCells = 0;

        for (int i = 0; i < maxActiveCellsNumber; ++i)
        {
            if (cellsPhysics[i].IsSpriteEnabled())
            {
                ++_activeCells;
            }
            if (_activeCells > 1)
            {
                // There are at least two cells active, player can still play
                break;
            }
        }

        if (_activeCells < 2)
        {
            yield return new WaitForSeconds(0.25f); // Should be enough to make sure CmdSetCellLocalScale() ended it's execution
            // Player should return to lobby, he has no cells left.
            if (isClient && isServer) // Is host
            {
                GameNetworkManager.singleton.StopHost();
            }
            if (isServer)
            {
                if (isClient)
                {
                    GameNetworkManager.singleton.StopHost();
                }
                GameNetworkManager.singleton.StopServer();
            }
            else
            {
                GameNetworkManager.singleton.StopClient();
            }
        }
        else
        {
            // Mark child cell as inactive
            _cellPhysics.SetEnableCollider(false);
            _cellPhysics.SetEnableSprite(false);
            _cellPhysics.SetEnableNameRender(false);
            CmdSetActiveCell(_cellPhysics, false);

            DoTryOrfanChildCellsOfCell(_cellPhysics);
            if (_parentCellPhysics != null)
            {
                // Let parent know his child is gone (already knows he has grandchildren as chilldren from DoTryOrfanChildCellsOfCell function call
                _parentCellPhysics.DecrementChildCellsNumber();
                _cellPhysics.parentCellPhysics = null;
            }

            // Disable isTrigger for child (is default state for inactive cell, will allow rigidbody automatic mass work)
            _cellPhysics.SetEnableColliderTrigger(false);
        }
    }

    #endregion



    #region Commands

    [Command]
    private void CmdSetCellLocalScale(Vector3 _eatenLocalScale, CellPhysics _eaterCellPhysics)
    {
        // Make target rpc run directly on the eater cell client where he has authority to change his localScale.
        TargetSetCellLocalScale(_eaterCellPhysics.netIdentity.connectionToClient, _eatenLocalScale, _eaterCellPhysics);
    }

    [Command]
    private void CmdSetActiveCell(CellPhysics _cellPhysics, bool _isActive)
    {
        if (!(isServer && isClient)) // Is NOT host
        {
            // Necessary to avoid calling this code twice for the host. (Here and in Rpc call)
            _cellPhysics.SetEnableCollider(_isActive);
            _cellPhysics.SetEnableSprite(_isActive);
            _cellPhysics.SetEnableNameRender(_isActive);
        }
        RpcSetActiveCell(_cellPhysics, _isActive);
    }

    #endregion



    #region TargetRpcs

    [TargetRpc]
    private void TargetSetCellLocalScale(NetworkConnectionToClient _target, Vector3 _eatenLocalScale, CellPhysics _eaterCellPhysics)
    {
        _eaterCellPhysics.transform.localScale += (_eatenLocalScale * CellPhysics.minSizeDifferenceToGetEaten);
    }

    #endregion



    #region ClientRpcs

    [ClientRpc(includeOwner = false)] // Avoid delay by calling body on owner, before calling from owner the command to send RPC and eventually call body
    private void RpcSetActiveCell(CellPhysics _cellPhysics, bool _isActive)
    {
        _cellPhysics.SetEnableCollider(_isActive);
        _cellPhysics.SetEnableSprite(_isActive);
        _cellPhysics.SetEnableNameRender(_isActive);
    }

    #endregion



    #region Common functions

    public void SetCellsName(string name)
    {
        for (int i = 0; i < maxActiveCellsNumber; i++)
        {
            // Set cell name regardless of whether cell is active or not
            cellsPhysics[i].SetCellName(name);
        }
    }

    #endregion
}
