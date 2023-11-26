using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpaceshipMovementController : NetworkBehaviour
{
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

    [Header("Current Flight Stats")]
    [SerializeField] private float _glide = 0f;
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

    private Rigidbody _rigidBody;
    private VehicleSeatController _seatController;
    private VehicleNetworkController _networkController;

    public Vector3 Velocity { get => this._rigidBody.velocity; set => this._rigidBody.velocity = value; }

    private void Awake()
    {
        this._rigidBody = GetComponent<Rigidbody>();
        this._seatController = GetComponent<VehicleSeatController>();
        this._networkController = GetComponent<VehicleNetworkController>();
    }

    private void Start() => this._currentBoostAmount = this._maxBoostAmount;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this._networkController.DriverClientId.OnValueChanged += this.OnDriverChange;

        if (this.IsHost)
            this.enabled = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._networkController.DriverClientId.OnValueChanged -= this.OnDriverChange;
    }

    private void FixedUpdate()
    {
        if (!this.IsOwner) { return; }
        HandleInputs();
        HandleGliding();

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
            this._glide = this._thrust1d * this._thrust * this._thrustScale;
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
            this._currentBoostAmount -= this._boostDeprecationRate;

            if (this._currentBoostAmount <= 0f)
                this._isBoosting = false;
        }
        else if (this._currentBoostAmount < this._maxBoostAmount)
            this._currentBoostAmount += this._boostRechargeRate;
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
            this._rigidBody.AddRelativeForce(Vector3.forward * this._glide * Time.fixedDeltaTime);
            this._glide *= this._thrustGlideReduction;
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

    public override void OnGainedOwnership() => this.enabled = true;
    public override void OnLostOwnership() => this.enabled = false;
    private void OnDriverChange(ulong _, ulong newDriverId) => this.enabled = newDriverId == MultiplayerSystem.LocalClientId;
}
