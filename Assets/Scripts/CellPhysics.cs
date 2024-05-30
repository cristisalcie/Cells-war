using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CellPhysics : NetworkBehaviour
{
    [HideInInspector]
    public CellPhysics parentCellPhysics;
    public bool stopUserInputMovement;
    public bool canGetDividedFromInputPerspective;

    private const float moveSpeedMultiplier = 25.0f;
    private const float divisionForceMultiplier = 2000.0f;
    private const int mergeBackTimerExpireInSeconds = 8 + 30;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider2D;

    private Rigidbody2D rb;
    private int childCellsNumber; // Needed to know whether this cell can be merged back into the main cell (become inactive)
    public CellsPhysicsManager cellsPhysicsManager = null;
    private bool mergeBackTimerExpired;

    private TextMeshProUGUI playerNameTextMeshPro;

    private void Awake()
    {
        canGetDividedFromInputPerspective = true;
        // Only the first component found will be returned
        playerNameTextMeshPro = GetComponentInChildren<TextMeshProUGUI>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider2D = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        parentCellPhysics = null;
        mergeBackTimerExpired = false;
        childCellsNumber = 0;
        stopUserInputMovement = false;
        if (cellsPhysicsManager == null)
        {
            Debug.Log("Error: cellsPhysicsManager not set!");
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (cellsPhysicsManager == null)
        {
            Debug.Log("Error! Please add cellsPhysicsManager component!");
        }
    }



    #region Functions called only on CLIENT instance

    [Client]
    public void Move(Vector2 _toWorldPosition)
    {
        Vector2 _playerToWorldPosition = _toWorldPosition - rb.position;
        Vector2 _playerDirection = _playerToWorldPosition.normalized;

        float _speedFactorDistanceBased = Mathf.Min(_playerToWorldPosition.magnitude, 1);

        Debug.DrawRay(rb.position, _playerToWorldPosition, Color.red, 1); // Debug ray direction

        rb.AddForce(_playerDirection * moveSpeedMultiplier * _speedFactorDistanceBased);
    }

    [Client]
    public void ApplyDivisionForce(Vector2 _toWorldPosition)
    {
        Vector2 _playerToWorldPosition = _toWorldPosition - rb.position;
        Vector2 _playerDirection = _playerToWorldPosition.normalized;

        rb.AddForce(_playerDirection * divisionForceMultiplier);
    }

    [Client]
    public void StartMergeBackTimer()
    {
        if (parentCellPhysics == null)
        {
            Debug.Log("Error: no cell to merge back into");
            return;
        }

        StartCoroutine(WaitToMergeBack());
    }

    [Client]
    private IEnumerator WaitToMergeBack()
    {
        yield return new WaitForSeconds(mergeBackTimerExpireInSeconds);
        mergeBackTimerExpired = true;
    }

    [Client]
    public void IncrementChildCellsNumber()
    {
        ++childCellsNumber;
    }

    [Client]
    public void DecrementChildCellsNumber()
    {
        if (childCellsNumber == 0)
        {
            Debug.Log("Error: Trying to decrement child cells number that is already 0");
            return;
        }
        --childCellsNumber;
    }

    [Client]
    private IEnumerator WaitToRespawnFoodCell(FoodCellBehaviour _foodCellBehaviour)
    {
        yield return new WaitForSeconds(FoodCellBehaviour.respawnInSeconds);
        CmdSpawnFoodCell(_foodCellBehaviour);
        _foodCellBehaviour.isRespawning = false;
    }

    #endregion



    #region Commands

    [Command]
    public void CmdKillFoodCell(FoodCellBehaviour _foodCellBehaviour)
    {
        _foodCellBehaviour.KillFoodCell();
    }

    [Command]
    public void CmdSpawnFoodCell(FoodCellBehaviour _foodCellBehaviour)
    {
        // Set random position on map
        _foodCellBehaviour.CalculatePosition();

        if (!(isClient && isServer)) // Is not host
        {
            // Necessary to avoid calling SetCellVisibleAndActive function twice for the host (Here and in Rpc call).
            _foodCellBehaviour.SetCellVisibleAndActive(true);
        }
        _foodCellBehaviour.RpcSetCellVisibleAndActive(true);
    }

    #endregion



    #region Common functions

    private bool DoTransformsOverlay(Transform _firstTransform, Transform _otherTransform)
    {
        // Check whether cells overlay enough to have one assimilate the other
        return Vector3.Distance(_firstTransform.position, _otherTransform.transform.position)
            < Mathf.Max(_firstTransform.localScale.x / 2, _otherTransform.transform.localScale.x / 2);
    }

    public bool CanMergeBack()
    {
        return mergeBackTimerExpired && childCellsNumber == 0;
    }

    public void SetCellName(string _cellName)
    {
        playerNameTextMeshPro.text = _cellName;
    }

    private bool HasParentCellInCollision(Collider2D _collision)
    {
        return parentCellPhysics != null;
    }

    private bool IsCellInCollision(Collider2D _collision)
    {
        return _collision.CompareTag("Cell");
    }

    private bool IsFoodCellInCollision(Collider2D _collision)
    {
        return _collision.CompareTag("FoodCell");
    }

    private bool IsThisChildParentInCollision(Collider2D _collision)
    {
        return _collision.gameObject == parentCellPhysics.gameObject;
    }

    public bool IsSpriteEnabled()
    {
        return spriteRenderer.enabled;
    }

    public void SetEnableSprite(bool _isEnabled)
    {
        spriteRenderer.enabled = _isEnabled;
    }

    public void SetEnableCollider(bool _isEnabled)
    {
        circleCollider2D.enabled = _isEnabled;
    }

    public void SetEnableColliderTrigger(bool _isEnabled)
    {
        circleCollider2D.isTrigger = _isEnabled;
    }

    public void SetEnableNameRender(bool _isEnabled)
    {
        playerNameTextMeshPro.enabled = _isEnabled;
    }

    private void OnCollisionEnter2D(Collision2D _collision)
    {
        //Debug.Log(_collision.collider.ToString());

    }

    private void OnCollisionStay2D(Collision2D _collision)
    {
        // Solves the case in which the cells are next to each other already and OnCollisionEnter2D() is not triggering
        if (isLocalPlayer
            && IsCellInCollision(_collision.collider)
            && HasParentCellInCollision(_collision.collider)
            && IsThisChildParentInCollision(_collision.collider)
            && CanMergeBack())
        {
            // Stop user input for both child and parent
            stopUserInputMovement = true;
            parentCellPhysics.stopUserInputMovement = true;

            // Disable rigidbody automatic mass in order to maintain the cell mass while collider is trigger.
            rb.useAutoMass = false;

            // Set trigger for child in order to allow overlap
            SetEnableColliderTrigger(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        //Debug.Log(gameObject.name + " ontriggerEnter2d" + _collision.GetComponent<Collider2D>().ToString());
    }

    private void OnTriggerStay2D(Collider2D _collision)
    {
        if (isLocalPlayer
            && IsCellInCollision(_collision)
            && HasParentCellInCollision(_collision)
            && IsThisChildParentInCollision(_collision)
            && CanMergeBack())
        {
            // Drag cells towards each other
            Move(parentCellPhysics.rb.transform.position);
            parentCellPhysics.Move(rb.transform.position);

            if (DoTransformsOverlay(transform, parentCellPhysics.transform))
            {
                // This cell is asimilated back by the cell it divided from.
                cellsPhysicsManager.MergeBackCell(this);
                mergeBackTimerExpired = false;
            }
        }

        if (isLocalPlayer
            && IsFoodCellInCollision(_collision)
            && DoTransformsOverlay(transform, _collision.transform))
        {
            FoodCellBehaviour _foodCellBehaviour = _collision.GetComponent<FoodCellBehaviour>();

            if (_foodCellBehaviour.isRespawning) return;
            _foodCellBehaviour.isRespawning = true;

            transform.localScale += _foodCellBehaviour.GetLocalScaleWithMultiplier();

            CmdKillFoodCell(_foodCellBehaviour);
            _ = StartCoroutine(WaitToRespawnFoodCell(_foodCellBehaviour));
        }
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        // Launched new divided cell and once it exited its parent cell set collider
        if (isLocalPlayer
            && IsCellInCollision(_collision)
            && HasParentCellInCollision(_collision)
            && IsThisChildParentInCollision(_collision))
        {
            if (CanMergeBack())
            {
                // Because of external forces caused by merging of other 2 cells, this function is called before merging was completed

                // Resume user input for both child and parent
                stopUserInputMovement = false;
                parentCellPhysics.stopUserInputMovement = false;

                // Enable back rigidbody automatic mass.
                rb.useAutoMass = true;

                // Disable trigger for child in order to allow collisions
                SetEnableColliderTrigger(false);
            }
            else /* Is enough to be detected only at the start of division */
            {
                // Note: Rigidbody automatic mass will calculate mass to 1 while trigger is true
                SetEnableColliderTrigger(false);
            }
        }
    }

    #endregion
}
