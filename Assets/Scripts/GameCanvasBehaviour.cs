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

    public bool isInputPaused;

    private void Awake()
    {
        isInputPaused = false;
    }

    void Start()
    {
        buttonExit.onClick.AddListener(ButtonExit);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isInputPaused = !optionsPanel.activeSelf;
            optionsPanel.SetActive(isInputPaused);
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
