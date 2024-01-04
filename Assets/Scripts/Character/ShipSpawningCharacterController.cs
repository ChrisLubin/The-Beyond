using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShipSpawningCharacterController : NetworkBehaviour, IInteractable
{
    private ShipSpawningCharacterNetworkController _networkController;

    [SerializeField] private TextMeshPro _interactText;
    [SerializeField] private Transform _spawnArea;
    [SerializeField] private float _distanceCheckLength = 15f;

    public Vector3 SpawnPoint => this._spawnArea.position;
    public Vector3 SpawnPointForward => this._spawnArea.forward;
    public Vector3 SpawnPointLocalPosition => this._spawnArea.localPosition;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Gizmos.DrawCube(this._spawnArea.position, new Vector3(this._distanceCheckLength, this._distanceCheckLength, this._distanceCheckLength));
    }

    private void Awake()
    {
        this._networkController = GetComponent<ShipSpawningCharacterNetworkController>();
    }

    private void Update()
    {
        if (this._networkController.SpawnableSpaceshipCount.Value == 0)
            this._interactText.text = "I'm all out of ships!";
        else if (this.CanSpawn())
            this._interactText.text = "Interact with me to spawn another spaceship!";
        else
            this._interactText.text = "Come back to me when a spaceship isn't docked here!";
    }

    public bool CanSpawn() => Physics.BoxCastAll(this._spawnArea.position, new(this._distanceCheckLength / 2f, this._distanceCheckLength / 2f, this._distanceCheckLength / 2f), Vector3.up, Quaternion.identity, 0.001f).Where(hit => hit.collider.CompareTag(Constants.TagNames.Vehicle)).Count() == 0 && this._networkController.SpawnableSpaceshipCount.Value != 0;

    public void DoInteract()
    {
        if (!this.CanSpawn()) { return; }

        this._networkController.SpawnSpaceshipServerRpc();
    }
}
