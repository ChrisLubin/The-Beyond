using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VehicleSeatController : NetworkBehaviourWithLogger<VehicleSeatController>
{
    private VehicleNetworkController _networkController;
    private VehicleInteractionController _interactionController;

    private const ulong _EMPTY_SEAT_CLIENT_ID = ulong.MaxValue;
    private const float _MAX_SEAT_DESYNC_THRESHOLD = 0.1f;

    [SerializeField] private List<Transform> _seatTransforms;
    public bool IsLocalPlayerInVehicle { get; private set; } = false;

    public bool HasAvailableSeat
    {
        get
        {
            foreach (SeatData seat in this._networkController.Seats)
            {
                if (seat.PlayerId == _EMPTY_SEAT_CLIENT_ID) { return true; }
            }

            return false;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        this._networkController = GetComponent<VehicleNetworkController>();
        this._interactionController = GetComponent<VehicleInteractionController>();
        this._interactionController.OnDidInteraction += this.OnDidInteraction;
        MultiplayerSystem.OnPlayerDisconnect += this.OnPlayerDisconnect;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!this.IsHost) { return; }

        for (int i = 0; i < this._seatTransforms.Count; i++)
            this._networkController.Seats.Add(new(_EMPTY_SEAT_CLIENT_ID, i));
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._interactionController.OnDidInteraction -= this.OnDidInteraction;
        MultiplayerSystem.OnPlayerDisconnect -= this.OnPlayerDisconnect;

        foreach (Transform seat in this._seatTransforms)
            Destroy(seat.gameObject);
    }

    private void Update()
    {
        if (!this.IsLocalPlayerInVehicle) { return; }

        // Necessary to sync player position when they enter moving car or immediately move after entering car
        foreach (SeatData seat in this._networkController.Seats)
        {
            if (seat.PlayerId != MultiplayerSystem.LocalClientId) { continue; }
            if (Vector3.Distance(PlayerManager.LocalPlayer.transform.localPosition, Vector3.zero) > _MAX_SEAT_DESYNC_THRESHOLD)
            {
                PlayerManager.LocalPlayer.transform.localPosition = Vector3.zero;
                PlayerManager.LocalPlayer.transform.forward = this._seatTransforms[seat.SeatIndex].forward;
            }
        }

    }

    public bool CanAddPlayer(ulong clientId)
    {
        if (!this.IsHost) { return false; }
        int availableSeats = 0;

        foreach (SeatData seat in this._networkController.Seats)
        {
            if (seat.PlayerId == clientId) { return false; }
            if (seat.PlayerId == _EMPTY_SEAT_CLIENT_ID)
                availableSeats++;
        }

        return availableSeats > 0;
    }

    public void AddPlayer(ulong clientId)
    {
        if (!this.CanAddPlayer(clientId)) { return; }

        if (!PlayerManager.Instance.TryGetPlayer(clientId, out PlayerController player))
        {
            this._logger.Log($"Could not find player with the client ID {clientId}", Logger.LogLevel.Error);
            return;
        }

        for (int i = 0; i < this._networkController.Seats.Count; i++)
        {
            SeatData seat = this._networkController.Seats[i];
            if (seat.PlayerId != _EMPTY_SEAT_CLIENT_ID) { continue; }

            seat.PlayerId = clientId;
            this._networkController.Seats[i] = seat;
            player.NetworkObject.TrySetParent(this._seatTransforms[seat.SeatIndex]);
            this._logger.Log($"Parented player {clientId} to seat index {seat.SeatIndex}");
            break;
        }
    }

    public bool CanRemovePlayer(ulong clientId)
    {
        if (!this.IsHost) { return false; }

        foreach (SeatData seat in this._networkController.Seats)
            if (seat.PlayerId == clientId) { return true; }

        return false;
    }

    public void RemovePlayer(ulong clientId)
    {
        if (!this.CanRemovePlayer(clientId)) { return; }

        if (!PlayerManager.Instance.TryGetPlayer(clientId, out PlayerController player))
        {
            this._logger.Log($"Could not find player with the client ID {clientId}", Logger.LogLevel.Error);
            return;
        }

        for (int i = 0; i < this._networkController.Seats.Count; i++)
        {
            SeatData seat = this._networkController.Seats[i];
            if (seat.PlayerId != clientId) { continue; }

            seat.PlayerId = _EMPTY_SEAT_CLIENT_ID;
            this._networkController.Seats[i] = seat;
            player.NetworkObject.TryRemoveParent();
            this._logger.Log($"Unparented player {clientId} from seat index {seat.SeatIndex}");
            break;
        }
    }

    private void OnDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this.IsLocalPlayerInVehicle = true;
                break;
            case InteractionType.ExitVehicle:
                this.IsLocalPlayerInVehicle = false;
                break;
            default:
                break;
        }
    }

    private void OnPlayerDisconnect(PlayerData player)
    {
        if (!this.IsOwner) { return; }

        for (int i = 0; i < this._networkController.Seats.Count; i++)
        {
            SeatData seat = this._networkController.Seats[i];
            if (seat.PlayerId != player.ClientId) { continue; }

            seat.PlayerId = _EMPTY_SEAT_CLIENT_ID;
            this._networkController.Seats[i] = seat;
            this._logger.Log($"Unparented player {player.ClientId} from seat index {seat.SeatIndex}");
            break;
        }
    }

    [Serializable]
    public struct SeatData : INetworkSerializable, System.IEquatable<SeatData>
    {
        public ulong PlayerId;
        public int SeatIndex;

        public SeatData(ulong playerId, int seatIndex)
        {
            this.PlayerId = playerId;
            this.SeatIndex = seatIndex;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out PlayerId);
                reader.ReadValueSafe(out SeatIndex);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(PlayerId);
                writer.WriteValueSafe(SeatIndex);
            }
        }

        public readonly bool Equals(SeatData other) => PlayerId == other.PlayerId && SeatIndex == other.SeatIndex;
    }
}
