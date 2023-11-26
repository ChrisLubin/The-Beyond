using UnityEngine;

public class InfoCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject _showControlsObject;
    [SerializeField] private GameObject _controlsObject;

    private void Update()
    {
        if (MultiplayerSystem.State != MultiplayerState.CreatedLobby && MultiplayerSystem.State != MultiplayerState.JoinedLobby) { return; }

        this._showControlsObject.SetActive(!Input.GetKey(KeyCode.Tab));
        this._controlsObject.SetActive(Input.GetKey(KeyCode.Tab));
    }
}
