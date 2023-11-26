using UnityEngine;
using System;

public class PlayerInteractorController : NetworkBehaviorAutoDisable<PlayerInteractorController>
{
    public event Action<InteractionType> OnDidInteraction;

    [SerializeField] private float _maxInteractDistance = 2f;

    private void Update()
    {
        if (!this.IsOwner) { return; }

        if (Input.GetKeyDown(KeyCode.T))
        {
            this.TryDoInteraction();
        }
    }

    protected void TryDoInteraction()
    {
        if (!this.IsOwner) { return; }

        for (int i = 0; i < 3; i++)
        {
            Vector3 startingPoint = this.transform.position + this.transform.up * (float)i;
            Vector3 endPoint = startingPoint + this.transform.forward * this._maxInteractDistance;

            Debug.DrawLine(startingPoint, endPoint, Color.black, 10f);
            bool didFindInteractable = Physics.Linecast(startingPoint, endPoint, out RaycastHit hit, LayerMask.GetMask(Constants.LayerNames.Interactable));
            if (!didFindInteractable) { continue; }

            bool didFindComponent = hit.collider.TryGetComponent(out IInteractable interactable);
            if (!didFindComponent) { continue; }

            interactable.DoInteract();
            break;
        }
    }

    // Called by the object(s) we interacted with
    public void DidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this.enabled = false;
                break;
            case InteractionType.ExitVehicle:
                this.enabled = true;
                break;
            default:
                break;
        }

        this.OnDidInteraction?.Invoke(interaction);
    }
}

public enum InteractionType
{
    EnterVehicle,
    ExitVehicle,
}
