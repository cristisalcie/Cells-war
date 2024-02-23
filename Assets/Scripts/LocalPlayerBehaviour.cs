using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerBehaviour : MonoBehaviour
{
    private LocalPlayerPhysics localPlayerPhysics;
    private Vector3 mouseWorldPosition;

    private void Awake()
    {
        localPlayerPhysics = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.name == "Cells")
            {
                localPlayerPhysics = child.GetComponent<LocalPlayerPhysics>();
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Handle input
        mouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // TODO: Divide Cells
            Debug.Log("Divide cells");
            localPlayerPhysics.DivideCells();
        }
    }

    private void FixedUpdate()
    {
        localPlayerPhysics.Move(mouseWorldPosition);
    }
}
