using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class VehicleSeatController : NetworkBehaviourWithLogger<VehicleSeatController>
{
    private VehicleNetworkController _networkController;
    private VehicleInteractionController _interactionController;

    public const ulong EMPTY_SEAT_PLAYER_ID = 5000;
    private const float _MAX_SEAT_DESYNC_THRESHOLD = 0.1f;

    [SerializeField] private List<Transform> _seatTransforms;
    public bool IsLocalPlayerInVehicle { get; private set; } = false;
    public bool IsLocalPlayerDriver => this._networkController.DriverClientId.Value == MultiplayerSystem.LocalClientId;

    public bool HasAvailableSeat { get => this._networkController.Seats.ToArray().Any(seat => seat.PlayerId == EMPTY_SEAT_PLAYER_ID); }
    public bool HasDriver { get => this._networkController.DriverClientId.Value != EMPTY_SEAT_PLAYER_ID; }

    [SerializeField] private float _exitSeatDistance = 1f;

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

        this._networkController.DriverClientId.Value = EMPTY_SEAT_PLAYER_ID;
        for (int i = 0; i < this._seatTransforms.Count; i++)
            this._networkController.Seats.Add(new(EMPTY_SEAT_PLAYER_ID, i));
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

    public bool CanAddPlayer(ulong playerId)
    {
        SeatData[] seats = this._networkController.Seats.ToArray();

        if (!this.IsHost) { return false; }
        if (seats.Any(seat => seat.PlayerId == playerId)) { return false; }

        return seats.Where(seat => seat.PlayerId == EMPTY_SEAT_PLAYER_ID).Count() > 0;
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
            if (seat.PlayerId != EMPTY_SEAT_PLAYER_ID) { continue; }
            if (seat.SeatIndex == 0)
            {
                this._networkController.DriverClientId.Value = clientId;
                this.NetworkObject.ChangeOwnership(clientId);
            }

            seat.PlayerId = clientId;
            this._networkController.Seats[i] = seat;
            player.NetworkObject.TrySetParent(this._seatTransforms[seat.SeatIndex]);
            this._logger.Log($"Parented player {clientId} to seat index {seat.SeatIndex}");
            break;
        }
    }

    public bool CanRemovePlayer(ulong playerId) => this.IsHost && this._networkController.Seats.ToArray().Any(seat => seat.PlayerId == playerId);

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
            if (seat.SeatIndex == 0)
            {
                this._networkController.DriverClientId.Value = EMPTY_SEAT_PLAYER_ID;
                this.NetworkObject.RemoveOwnership();
            }

            seat.PlayerId = EMPTY_SEAT_PLAYER_ID;
            this._networkController.Seats[i] = seat;
            player.NetworkObject.TryRemoveParent();
            this._logger.Log($"Unparented player {clientId} from seat index {seat.SeatIndex}");
            break;
        }
    }

    public bool CanChangeSeat(ulong playerId) => this.IsHost && this.isInVehicle(playerId) || this.HasAvailableSeat;

    public void ChangeSeat(ulong playerId)
    {
        if (!this.CanChangeSeat(playerId)) { return; }
        if (!PlayerManager.Instance.TryGetPlayer(playerId, out PlayerController player))
        {
            this._logger.Log($"Could not find player with the client ID {playerId}", Logger.LogLevel.Error);
            return;
        }

        SeatData[] seats = this._networkController.Seats.ToArray();
        int playerCurrentSeatIndex = seats.FirstOrDefault(seat => seat.PlayerId == playerId).SeatIndex;

        for (int i = playerCurrentSeatIndex; i < seats.Count() + playerCurrentSeatIndex; i++)
        {
            int seatIndex = i % seats.Count();
            SeatData seat = this._networkController.Seats[seatIndex];

            // Don't use i anymore, use seatIndex
            if (seatIndex == playerCurrentSeatIndex)
            {
                seat.PlayerId = EMPTY_SEAT_PLAYER_ID;
                this._networkController.Seats[seatIndex] = seat;

                if (seatIndex == 0)
                {
                    this._networkController.DriverClientId.Value = EMPTY_SEAT_PLAYER_ID;
                    this.NetworkObject.RemoveOwnership();
                }
                continue;
            }

            if (seat.PlayerId == EMPTY_SEAT_PLAYER_ID)
            {
                seat.PlayerId = playerId;
                this._networkController.Seats[seatIndex] = seat;
                player.NetworkObject.TrySetParent(this._seatTransforms[seatIndex]);

                if (seatIndex == 0)
                {
                    this._networkController.DriverClientId.Value = playerId;
                    this.NetworkObject.ChangeOwnership(playerId);
                }
                this._logger.Log($"Moved player {playerId} to seat index {seatIndex}");
                break;
            }
        }
    }

    public bool IsDriver(ulong clientId) => this._networkController.DriverClientId.Value == clientId;
    public bool isInVehicle(ulong clientId) => this._networkController.Seats.ToArray().Any(seat => seat.PlayerId == clientId);

    private void OnDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this.IsLocalPlayerInVehicle = true;
                break;
            case InteractionType.ExitVehicle:
                this.IsLocalPlayerInVehicle = false;
                PlayerManager.LocalPlayer.transform.position += transform.up * this._exitSeatDistance;
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

            seat.PlayerId = EMPTY_SEAT_PLAYER_ID;
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
