using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LobbyCanvasBehaviour : MonoBehaviour
{
    [Header("Buttons")]
    public Button buttonHost;
    public Button buttonServer;
    public Button buttonClient;
    public Button buttonExit;

    [Header("Panels")]
    public GameObject PanelStart;

    [Header("InputFields")]
    public InputField inputFieldAddress;
    public InputField inputFieldPlayerName;

    public TextMeshProUGUI clientText;

    private const int connectionTimeoutSeconds = 5;

    private void Start()
    {
        // Update the canvas text if you have manually changed network manager's address from the game object before starting the game scene
        if (GameNetworkManager.singleton.networkAddress != "localhost") { inputFieldAddress.text = GameNetworkManager.singleton.networkAddress; }

        // Adds a listener to the main input field and invokes a method when the value changes.
        inputFieldAddress.onValueChanged.AddListener(delegate { AddressChangeCheck(); });
        inputFieldPlayerName.onValueChanged.AddListener(delegate { PlayerNameChangeCheck(); });

        // Make sure to attach these Buttons in the Inspector
        buttonHost.onClick.AddListener(ButtonHost);
        buttonServer.onClick.AddListener(ButtonServer);
        buttonClient.onClick.AddListener(ButtonClient);
        buttonExit.onClick.AddListener(ButtonExit);

        // This updates the Unity canvas, we have to manually call it every change, unlike legacy OnGUI.
        SetupCanvas();
    }

    // Invoked when the value of the text field changes.
    public void AddressChangeCheck()
    {
        GameNetworkManager.singleton.networkAddress = inputFieldAddress.text;
    }

    // Invoked when the value of the text field changes.
    public void PlayerNameChangeCheck()
    {
        ((GameNetworkManager)GameNetworkManager.singleton).localPlayerName = inputFieldPlayerName.text;
    }

    public void ButtonHost()
    {
        GameNetworkManager.singleton.StartHost();
        SetupCanvas();
    }

    public void ButtonServer()
    {
        GameNetworkManager.singleton.StartServer();
        SetupCanvas();
    }

    public void ButtonClient()
    {
        GameNetworkManager.singleton.StartClient();
        SetupCanvas();
    }
    public void ButtonExit()
    {
        Application.Quit();
    }

    private IEnumerator CheckConnectionState()
    {
        // Since this coroutine is attached to CanvasLobbyHUD, when scene changes this script's game object
        // will be destroyed, hence this coroutine will be destroyed
        yield return new WaitForSeconds(connectionTimeoutSeconds);
        // Call SetupCanvas function that will check connection state
        SetupCanvas();
    }

    public void SetupCanvas()
    {
        // Unlock cursor and set it to be visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (NetworkClient.active)
            {
                PanelStart.SetActive(false);
                clientText.text = "Connecting to " + GameNetworkManager.singleton.networkAddress + "..";

                // Set cursor to be invisible (Future feature)
                //Cursor.visible = false;

                // Start timeout timer
                StartCoroutine(CheckConnectionState());
            }
            else
            {
                clientText.text = null;
                PanelStart.SetActive(true);
            }
        }
    }
}