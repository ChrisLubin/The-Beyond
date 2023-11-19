using UnityEngine;

public class DoorAnimatorController : MonoBehaviour
{
    private Animator _animator;
    private DoorPlayerCountController _playerCountController;
    [SerializeField] private int _doorTypeId;

    private const string _PLAYERS_IN_DOORWAY_PARAMETER = "PlayersInDoorway";
    private const string _DOOR_TYPE_ID_PARAMETER = "DoorTypeId";
    private int _playersInDoorwayHash;
    private int _doorTypeIdHash;

    private void Awake()
    {
        this._animator = GetComponent<Animator>();
        this._playersInDoorwayHash = Animator.StringToHash(_PLAYERS_IN_DOORWAY_PARAMETER);
        this._doorTypeIdHash = Animator.StringToHash(_DOOR_TYPE_ID_PARAMETER);
        this._playerCountController = GetComponent<DoorPlayerCountController>();
        this._playerCountController.OnPlayersInDoorwayChange += OnPlayersInDoorwayChange;
    }

    private void Start() => this._animator.SetInteger(this._doorTypeIdHash, this._doorTypeId);
    private void OnDestroy() => this._playerCountController.OnPlayersInDoorwayChange -= OnPlayersInDoorwayChange;

    private void OnPlayersInDoorwayChange(int count) => this._animator.SetInteger(this._playersInDoorwayHash, count);
}
