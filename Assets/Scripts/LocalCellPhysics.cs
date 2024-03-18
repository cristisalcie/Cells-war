using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LocalCellPhysics : MonoBehaviour
{
    [HideInInspector]
    public LocalCellPhysics parentCellPhysics;
    public bool stopUserInputMovement;

    private const float moveSpeedMultiplier = 25.0f;
    private const float divisionForceMultiplier = 2000.0f;
    private const int mergeBackTimerExpireInSeconds = 8;
    private readonly Vector2 minScale = Vector2.one;

    private Rigidbody2D rb;
    private int childCellsNumber;
    private LocalCellsPhysicsManager localCellsPhysicsManager;
    private bool mergeBackTimerExpired;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        parentCellPhysics = null;
        localCellsPhysicsManager = null;
        mergeBackTimerExpired = false;
        childCellsNumber = 0;
        stopUserInputMovement = false;
    }

    public void Move(Vector2 _toWorldPosition)
    {
        Vector2 _playerToWorldPosition = _toWorldPosition - rb.position;
        Vector2 _playerDirection = _playerToWorldPosition.normalized;

        float _speedFactorDistanceBased = Mathf.Min(_playerToWorldPosition.magnitude, 1);

        Debug.DrawRay(rb.position, _playerToWorldPosition, Color.red, 1); // Debug ray direction

        rb.AddForce(_playerDirection * moveSpeedMultiplier * _speedFactorDistanceBased);
    }

    private bool IsCurrentCellAbsorbedByCellTransform(Transform _otherCell)
    {
        // Check whether cells overlay enough to have one assimilate the other
        return Vector3.Distance(transform.position, _otherCell.transform.position)
            < Mathf.Max(transform.localScale.x / 2, _otherCell.transform.localScale.x / 2);
    }

    public bool CanMergeBack()
    {
        return mergeBackTimerExpired && childCellsNumber == 0;
    }

    public void ApplyDivisionForce(Vector2 _toWorldPosition)
    {
        Vector2 _playerToWorldPosition = _toWorldPosition - rb.position;
        Vector2 _playerDirection = _playerToWorldPosition.normalized;

        rb.AddForce(_playerDirection * divisionForceMultiplier);
    }

    public void StartMergeBackTimer()
    {
        if (parentCellPhysics == null)
        {
            Debug.Log("Error: no cell to merge back into");
            return;
        }

        StartCoroutine(WaitToMergeBack());
    }

    private IEnumerator WaitToMergeBack()
    {
        yield return new WaitForSeconds(mergeBackTimerExpireInSeconds);
        mergeBackTimerExpired = true;
    }

    public void SetLocalPlayerPhysics(LocalCellsPhysicsManager _localCellsPhysicsManager)
    {
        localCellsPhysicsManager = _localCellsPhysicsManager;
    }

    public void IncrementChildCellsNumber()
    {
        ++childCellsNumber;
    }
    
    public void DecrementChildCellsNumber()
    {
        if (childCellsNumber == 0)
        {
            Debug.Log("Error: Trying to decrement child cells number that is already 0");
            return;
        }
        --childCellsNumber;
    }
    
    public bool CanDivide(Vector3 _newScale)
    {
        return _newScale.x >= minScale.x && _newScale.y >= minScale.y;
    }

    private bool HasParentCellInCollision(Collider2D _collision)
    {
        return parentCellPhysics != null;
    }

    private bool IsLocalCellInCollision(Collider2D _collision)
    {
        return _collision.CompareTag("LocalCell");
    }

    private bool IsThisChildParentInCollision(Collider2D _collision)
    {
        return _collision.gameObject == parentCellPhysics.gameObject;
    }

    private void OnCollisionEnter2D(Collision2D _collision)
    {
        //Debug.Log(_collision.collider.ToString());

    }

    private void OnCollisionStay2D(Collision2D _collision)
    {
        // Solves the case in which the cells are next to each other already and OnCollisionEnter2D() is not triggering
        if (IsLocalCellInCollision(_collision.collider)
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
            GetComponent<CircleCollider2D>().isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        //Debug.Log(gameObject.name + " ontriggerEnter2d" + _collision.GetComponent<Collider2D>().ToString());
    }

    private void OnTriggerStay2D(Collider2D _collision)
    {
        if (IsLocalCellInCollision(_collision)
            && HasParentCellInCollision(_collision)
            && IsThisChildParentInCollision(_collision)
            && CanMergeBack())
        {
            // Drag cells towards each other
            Move(parentCellPhysics.rb.transform.position);
            parentCellPhysics.Move(rb.transform.position);

            if (IsCurrentCellAbsorbedByCellTransform(parentCellPhysics.transform))
            {
                // This cell is asimilated back by the cell it divided from.
                localCellsPhysicsManager.MergeBackCell(this);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D _collision)
    {
        // Launched new divided cell and once it exited its parent cell set collider
        if (IsLocalCellInCollision(_collision)
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
                GetComponent<CircleCollider2D>().isTrigger = false;
            }
            else /* Is enough to be detected only at the start of division */
            {
                // Note: Rigidbody automatic mass will calculate mass to 1 while trigger is true
                GetComponent<CircleCollider2D>().isTrigger = false;
            }
        }
    }

}
