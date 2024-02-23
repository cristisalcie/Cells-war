using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCellPhysics : MonoBehaviour
{
    public float speedFactorSizeBased = 50.0f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    public void Move(Vector2 _mouseWorldPosition)
    {
        Vector2 _playerToMouse = _mouseWorldPosition - rb.position;
        Vector2 _playerDirection = _playerToMouse.normalized;

        float _speedFactorMouseBased = Mathf.Min(_playerToMouse.magnitude, 1);

        Debug.DrawRay(rb.position, _playerToMouse, Color.red, 1); // Debug ray direction

        rb.AddForce(_playerDirection * speedFactorSizeBased * _speedFactorMouseBased);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log(collision.collider.ToString());
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log(gameObject.name + " ontriggerEnter2d" + collision.GetComponent<Collider2D>().ToString());
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        //Debug.Log(gameObject.name + "ontriggerStay2d" + collision.GetComponent<Collider2D>().ToString());
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        //Debug.Log(gameObject.name + "ontriggerExit2d" + collision.GetComponent<Collider2D>().ToString());
    }

}
