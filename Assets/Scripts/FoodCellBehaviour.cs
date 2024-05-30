using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class FoodCellBehaviour : NetworkBehaviour
{
    public const int respawnInSeconds = 5;
    private const float scaleIncreaseMultiplier = 0.02f;
    private Transform mapBordersTransform;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;

    // Used to make sure code inside CellPhysics.cs, function OnTriggerStay2D for the client
    // with authority over that object is not ran twice
    public bool isRespawning;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        isRespawning = false;
    }



    #region Functions that run only on SERVER instance

    [Server]
    public void FindMapBordersTransform()
    {
        mapBordersTransform = GameObject.Find("Map Borders").transform;
    }

    [Server]
    public void CalculatePosition()
    {
        float _newPositionX = Random.Range
            (
                -mapBordersTransform.localScale.x / 2 + transform.localScale.x / 2,
                mapBordersTransform.localScale.x / 2 - transform.localScale.x / 2
            );
        float _newPositionY = Random.Range
            (
                -mapBordersTransform.localScale.y / 2 + transform.localScale.y / 2,
                mapBordersTransform.localScale.y / 2 - transform.localScale.y / 2
            );

        transform.position = new Vector3(_newPositionX, _newPositionY, 0);
    }

    [Server]
    public void KillFoodCell()
    {
        if (!(isServer & isClient)) // Is not host
        {
            // Necessary to avoid calling SetCellVisibleAndActive function twice for the host (Here and in Rpc call).
            SetCellVisibleAndActive(false);
        }
        RpcSetCellVisibleAndActive(false);
    }


    #endregion



    #region ClientRpcs

    [ClientRpc]
    public void RpcSetCellVisibleAndActive(bool _isVisible)
    {
        SetCellVisibleAndActive(_isVisible);
    }

    #endregion



    #region Common functions

    public void SetCellVisibleAndActive(bool _isVisible)
    {
        Debug.Log("SetCellVisibleAndActive called with value " + _isVisible);
        spriteRenderer.enabled = _isVisible;
        circleCollider.enabled = _isVisible;
    }


    public Vector3 GetLocalScaleWithMultiplier()
    {
        return transform.localScale * scaleIncreaseMultiplier;
    }

    #endregion
}
