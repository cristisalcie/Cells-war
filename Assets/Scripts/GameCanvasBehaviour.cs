using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameCanvasBehaviour : NetworkBehaviour
{

    [Header("Panels")]
    public GameObject optionsPanel;

    [Header("Buttons")]
    public Button buttonExit;

    // Start is called before the first frame update
    void Start()
    {
        buttonExit.onClick.AddListener(ButtonExit);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            optionsPanel.SetActive(!optionsPanel.activeSelf);
        }
    }

    public void ButtonExit()
    {
        if (isClient && isServer) // Is host
        {
            GameNetworkManager.singleton.StopHost();
        }
        if (isServer)
        {
            if (isClient)
            {
                GameNetworkManager.singleton.StopHost();
                return;
            }
            GameNetworkManager.singleton.StopServer();
        }
        else
        {
            GameNetworkManager.singleton.StopClient();
        }
    }
}
