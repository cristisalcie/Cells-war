using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class FoodCellBehaviour : MonoBehaviour
{
    private const int respawnInSeconds = 5;
    private const float scaleIncreaseMultiplier = 0.02f;
    private Transform mapBordersTransform;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        FindMapBordersTransform();
        Spawn();
    }

    private void FindMapBordersTransform()
    {
        mapBordersTransform = GameObject.Find("Map Borders").transform;
    }

    private bool IsFoodCellAbsorbedByCellTransform(Transform _otherCell)
    {
        // Check whether cells overlay enough to have one assimilate the other
        return Vector3.Distance(transform.position, _otherCell.transform.position)
            < Mathf.Max(transform.localScale.x / 2, _otherCell.transform.localScale.x / 2);
    }

    private bool IsCellInCollision(Collider2D _collision)
    {
        return _collision.CompareTag("Cell");
    }

    private IEnumerator WaitToRespawn()
    {
        yield return new WaitForSeconds(respawnInSeconds);
        Spawn();
    }

    private void KillFoodCell()
    {
        spriteRenderer.enabled = false;
        circleCollider.enabled = false;
    }

    private void Spawn()
    {
        // Set random position on map
        CalculatePosition();

        // Set cell alive
        spriteRenderer.enabled = true;
        circleCollider.enabled = true;
    }

    private void CalculatePosition()
    {
        float _newPositionX = UnityEngine.Random.Range
            (
                -mapBordersTransform.localScale.x / 2 + transform.localScale.x / 2,
                mapBordersTransform.localScale.x / 2 - transform.localScale.x / 2
            );
        float _newPositionY = UnityEngine.Random.Range
            (
                -mapBordersTransform.localScale.y / 2 + transform.localScale.y / 2,
                mapBordersTransform.localScale.y / 2 - transform.localScale.y / 2
            );

        transform.position = new Vector3(_newPositionX, _newPositionY, 0);
    }

    private void OnTriggerStay2D(Collider2D _collision)
    {
        if (IsCellInCollision(_collision)
            && IsFoodCellAbsorbedByCellTransform(_collision.transform))
        {
            // Calculate new scale for the eater cell
            Vector3 _newLocalScale = new Vector3(
                _collision.transform.localScale.x,
                _collision.transform.localScale.y,
                _collision.transform.localScale.z);

            _newLocalScale += (transform.localScale * scaleIncreaseMultiplier);

            _collision.transform.localScale = _newLocalScale;

            // Kill this food cell
            KillFoodCell();

            // Start timer to respawn
            StartCoroutine(WaitToRespawn());
        }
    }
}
