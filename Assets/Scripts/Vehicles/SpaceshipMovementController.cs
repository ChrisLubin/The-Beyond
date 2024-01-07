using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpaceshipMovementController : NetworkBehaviourWithLogger<SpaceshipMovementController>, IGravityWellObject
{
    private Rigidbody _rigidBody;
    private VehicleSeatController _seatController;
    private VehicleInteractionController _interactionController;
    private VehicleNetworkController _networkController;
    private NetworkTransform _networkTransform;

    [Header("Movement Attributes")]
    [SerializeField] private float _yawTorque = 100f;
    [SerializeField] private float _pitchTorque = 100f;
    [SerializeField] private float _rollTorque = 100f;
    [SerializeField] private float _thrust = 800f;
    [SerializeField] private float _upThrust = 300f;
    [SerializeField] private float _strafeThrust = 300f;
    [SerializeField, Range(0.001f, 0.999f)] private float _thrustGlideReduction = 0.5f;
    [SerializeField, Range(0.001f, 0.999f)] private float _upDownGlideReduction = 0.111f;
    [SerializeField, Range(0.001f, 0.999f)] private float _leftRightGlideReduction = 0.111f;

    [Header("Boost Attributes")]
    [SerializeField] private float _maxBoostAmount = 100;
    [SerializeField] private float _boostDeprecationRate = 10f;
    [SerializeField] private float _boostRechargeRate = 5f;
    [SerializeField] private float _boostMultiplier = 2;

    [Header("Gravity Well Attributes")]
    [SerializeField, Range(0.001f, 0.999f)] private float _enterGravityWellVelocityMultiplier = 0.1f;
    [SerializeField] private float _enterGravityWellVelocityThreshold = 6f;

    [Header("Current Flight Stats")]
    [SerializeField] private NetworkVariable<float> _glide = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private float _horizontalGlide = 0f;
    [SerializeField] private float _verticalGlide = 0f;
    [SerializeField] private float _currentBoostAmount = 0f;
    [SerializeField] private bool _isBoosting = false;
    [SerializeField] private float _thrustScale = 0.5f;

    private int _thrust1d;
    private int _upDown1d;
    private int _strafe1d;
    private int _roll1d;
    private Vector2 _pitchYaw;

    public float ForwardThrust { get => this._glide.Value; }
    public float MaxForwardThrust { get => this._thrust; }

    public static float CurrentThrottle { get; protected set; } = 0.5f;
    public static float CurrentBoost { get; protected set; } = 1f;
    public static float CurrentVelocityMph { get; protected set; } = 0f;
    public static bool IsLocalPlayerInVehicle { get; protected set; } = false;
    public static bool IsLocalPlayerDriver { get; protected set; } = false;
    private const float _METERS_PER_SECOND_TO_MILES_PER_HOUR = 2.23694f;

    protected override void Awake()
    {
        base.Awake();
        this._rigidBody = GetComponent<Rigidbody>();
        this._seatController = GetComponent<VehicleSeatController>();
        this._interactionController = GetComponent<VehicleInteractionController>();
        this._interactionController.OnDidInteraction += this.OnDidInteraction;
        this._networkController = GetComponent<VehicleNetworkController>();
        this._networkTransform = GetComponent<NetworkTransform>();
    }

    private void Start() => this._currentBoostAmount = this._maxBoostAmount;

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._interactionController.OnDidInteraction -= this.OnDidInteraction;
    }

    private void FixedUpdate()
    {
        if (this._seatController.IsLocalPlayerInVehicle)
            this.UpdateStaticValues();

        if (!this.IsOwner) { return; }
        HandleInputs();
        HandleGliding();
        this._networkController.Velocity.Value = this._rigidBody.velocity;

        if (!this._seatController.IsDriver(MultiplayerSystem.LocalClientId)) { return; }
        HandleBoosting();
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Roll
        this._rigidBody.AddRelativeTorque(Vector3.back * this._roll1d * this._rollTorque * this._thrustScale * Time.fixedDeltaTime);

        // Pitch
        this._rigidBody.AddRelativeTorque(Vector3.right * Mathf.Clamp(this._pitchYaw.y, -1f, 1f) * this._pitchTorque * Time.fixedDeltaTime);

        // Yaw
        this._rigidBody.AddRelativeTorque(Vector3.up * Mathf.Clamp(this._pitchYaw.x, -1f, 1f) * this._yawTorque * Time.fixedDeltaTime);

        // Thrust
        if (this._thrust1d == 1 || this._thrust1d == -1)
        {
            float currentThrust = this._thrust * this._thrustScale;

            if (this._isBoosting)
                currentThrust *= this._boostMultiplier;

            this._rigidBody.AddRelativeForce(Vector3.forward * this._thrust1d * currentThrust * Time.fixedDeltaTime);
            this._glide.Value = this._thrust1d * this._thrust * this._thrustScale;
        }

        // Up/Down
        if (this._upDown1d == 1 || this._upDown1d == -1)
        {
            this._rigidBody.AddRelativeForce(Vector3.up * this._upDown1d * this._upThrust * this._thrustScale * Time.fixedDeltaTime);
            this._verticalGlide = this._upDown1d * this._upThrust * this._thrustScale;
        }

        // Strafing
        if (this._strafe1d == 1 || this._strafe1d == -1)
        {
            this._rigidBody.AddRelativeForce(Vector3.right * this._strafe1d * this._strafeThrust * this._thrustScale * Time.fixedDeltaTime);
            this._horizontalGlide = this._strafe1d * this._strafeThrust * this._thrustScale;
        }
    }

    private void HandleBoosting()
    {
        if (this._isBoosting && this._currentBoostAmount > 0f)
        {
            this._currentBoostAmount = Mathf.Clamp(this._currentBoostAmount - this._boostDeprecationRate, 0f, this._maxBoostAmount);

            if (this._currentBoostAmount <= 0f)
                this._isBoosting = false;
        }
        else if (this._currentBoostAmount < this._maxBoostAmount)
            this._currentBoostAmount = Mathf.Clamp(this._currentBoostAmount + this._boostRechargeRate, 0f, this._maxBoostAmount);
    }

    private void HandleInputs()
    {
        this._pitchYaw = InputSystem.look;

        this._thrustScale = Mathf.Clamp(this._thrustScale + (Input.mouseScrollDelta.y * 0.02f), 0.1f, 1f);

        // Thrust
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S))
            this._thrust1d = 0;
        else if (Input.GetKey(KeyCode.W))
            this._thrust1d = 1;
        else if (Input.GetKey(KeyCode.S))
            this._thrust1d = -1;
        else
            this._thrust1d = 0;

        // Roll
        if (Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.Q))
            this._roll1d = 0;
        else if (Input.GetKey(KeyCode.E))
            this._roll1d = 1;
        else if (Input.GetKey(KeyCode.Q))
            this._roll1d = -1;
        else
            this._roll1d = 0;

        // Up/Down
        if (Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.LeftControl))
            this._upDown1d = 0;
        else if (Input.GetKey(KeyCode.Space))
            this._upDown1d = 1;
        else if (Input.GetKey(KeyCode.LeftControl))
            this._upDown1d = -1;
        else
            this._upDown1d = 0;

        // Strafe
        if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.A))
            this._strafe1d = 0;
        else if (Input.GetKey(KeyCode.D))
            this._strafe1d = 1;
        else if (Input.GetKey(KeyCode.A))
            this._strafe1d = -1;
        else
            this._strafe1d = 0;

        // Boost
        this._isBoosting = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleGliding()
    {
        if (this._thrust1d == 0 || !this._seatController.IsDriver(MultiplayerSystem.LocalClientId))
        {
            this._rigidBody.AddRelativeForce(Vector3.forward * this._glide.Value * Time.fixedDeltaTime);
            this._glide.Value *= this._thrustGlideReduction;
        }
        if (this._upDown1d == 0 || !this._seatController.IsDriver(MultiplayerSystem.LocalClientId))
        {
            this._rigidBody.AddRelativeForce(Vector3.up * this._verticalGlide * Time.fixedDeltaTime);
            this._verticalGlide *= this._upDownGlideReduction;
        }
        if (this._strafe1d == 0 || !this._seatController.IsDriver(MultiplayerSystem.LocalClientId))
        {
            this._rigidBody.AddRelativeForce(Vector3.right * this._horizontalGlide * Time.fixedDeltaTime);
            this._horizontalGlide *= this._leftRightGlideReduction;
        }
    }

    private void UpdateStaticValues()
    {
        SpaceshipMovementController.CurrentThrottle = this._thrustScale;
        SpaceshipMovementController.CurrentBoost = this._currentBoostAmount / this._maxBoostAmount;
        SpaceshipMovementController.CurrentVelocityMph = this._networkController.Velocity.Value.magnitude * _METERS_PER_SECOND_TO_MILES_PER_HOUR;
        SpaceshipMovementController.IsLocalPlayerInVehicle = this._seatController.IsLocalPlayerInVehicle;
        SpaceshipMovementController.IsLocalPlayerDriver = this._seatController.IsLocalPlayerDriver;
    }

    private void OnTransformParentChanged()
    {
        if (!this.IsOwner) { return; }
        bool isParented = transform.parent != null;

        if (isParented)
            this._rigidBody.velocity *= this._enterGravityWellVelocityMultiplier;

        this._networkTransform.InLocalSpace = isParented;

        if (transform.parent == null || transform.parent.CompareTag(Constants.TagNames.GravityWellContainer))
            this._logger.Log($"{(isParented ? "Entered" : "Exited")} gravity well");
    }

    private void OnDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.ExitVehicle:
                SpaceshipMovementController.IsLocalPlayerInVehicle = false;
                SpaceshipMovementController.IsLocalPlayerDriver = false;
                break;
            default:
                break;
        }
    }

    public void SetRigidBodyVelocity() => this._rigidBody.velocity = this._networkController.Velocity.Value; // Used so momentum is carried between owner changes
    public bool CanBeReParented() => this._networkController.Velocity.Value.magnitude < this._enterGravityWellVelocityThreshold;
}
