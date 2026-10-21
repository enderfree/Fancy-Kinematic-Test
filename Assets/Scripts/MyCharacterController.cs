using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using KinematicCharacterController;
using System;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class MyCharacterController : MonoBehaviour, ICharacterController
{
    public InputActionReference MoveAction;
    public InputActionReference JumpAction;
    public InputActionReference SprintAction;
    public CinemachineCamera PlayerCamera;

    public float MaxGroundSpeed = 10f;
    public float MaxAirSpeed = 5f;
    public float MaxRunSpeed = 15f;

    public float GroundAcceleration = 5f;
    public float AirAcceleration = 2f;
    public float RunAcceleration = 9f;

    public float JumpStrength = 5f;
    public float JumpBuffer = 0f;
    public float CoyoteTime = 0f;

    public float RotationRate = 5f;

    public Vector3 Gravity = new Vector3(0, -10, 0);

    // If the character is currently swimming or not
    public bool IsSwimming = false;

    private Vector3 _moveInput;
    private Vector3 _lookInput;
    private bool _jumpPressed;
    private bool _sprintPressed;

    private bool _hasJumpedThisFrame;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceGrounded = Mathf.Infinity;

    private KinematicCharacterMotor _kinematicMotor;

    // MonoBehavior
    private void Start()
    {
        Initialize();
    }

    // ICharacterController
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInput == Vector3.zero) return;

        Vector3 targetLookDirection = Vector3.Slerp(_kinematicMotor.CharacterForward, _lookInput, deltaTime * RotationRate);

        currentRotation = Quaternion.LookRotation(targetLookDirection, _kinematicMotor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_kinematicMotor.GroundingStatus.IsStableOnGround)
        {
            GroundMovement(ref currentVelocity, deltaTime);
        }
        else if (IsSwimming)
        {
            LiquidMovement(ref currentVelocity, deltaTime);
        }
        else
        {
            AirMovement(ref currentVelocity, deltaTime);
        }

        ApplyJumping(ref currentVelocity, deltaTime);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        UpdateInputs();
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        ProcessJumpPostUpdate(deltaTime);
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    // Kaermak
    private void Initialize()
    {
        _kinematicMotor = GetComponent<KinematicCharacterMotor>();
        _kinematicMotor.CharacterController = this;
    }
    private void UpdateInputs()
    {
        Vector2 moveActionInput = MoveAction.action.ReadValue<Vector2>();

        _moveInput = new Vector3(moveActionInput.x, 0, moveActionInput.y);
        _moveInput = Vector3.ClampMagnitude(_moveInput, 1);

        _moveInput = GetCameraPlanarRotation(PlayerCamera.State.GetFinalOrientation()) * _moveInput;

        _lookInput = _moveInput;

        _jumpPressed = (JumpAction.action.ReadValue<float>() > 0);

        _sprintPressed = (SprintAction.action.ReadValue<float>() > 0);

        if (_jumpPressed)
        {
            _timeSinceJumpRequested = 0f;
        }
    }

    private Quaternion GetCameraPlanarRotation(Quaternion cameraRotation)
    {
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, _kinematicMotor.CharacterUp).normalized;

        if (cameraPlanarDirection.sqrMagnitude > 0)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.up, _kinematicMotor.CharacterUp).normalized;
        }

        return Quaternion.LookRotation(cameraPlanarDirection, _kinematicMotor.CharacterUp);
    }

    private void GroundMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        float maxSpeed = MaxGroundSpeed;
        float acceleration = GroundAcceleration;

        if (_sprintPressed)
        {
            maxSpeed = MaxRunSpeed;
            acceleration = RunAcceleration;
        }

        Vector3 targetVelocity = _moveInput * maxSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, deltaTime * acceleration);
    }

    private void AirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_moveInput.sqrMagnitude > 0)
        {
            Vector3 targetVelocity = _moveInput * MaxAirSpeed;

            Vector3 velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, Gravity);

            currentVelocity += velocityDiff * deltaTime * AirAcceleration;
        }

        ApplyGravity(ref currentVelocity, deltaTime);
    }

    private void ApplyJumping(ref Vector3 currentVelocity, float deltaTime)
    {
        _hasJumpedThisFrame = false;

        if ((_jumpPressed &&
            _timeSinceJumpRequested <= JumpBuffer && 
            (IsGrounded() || _timeSinceGrounded <= CoyoteTime)))
        {
            _kinematicMotor.ForceUnground();

            Vector3 jumpForce = _kinematicMotor.CharacterUp * JumpStrength;

            currentVelocity += jumpForce;

            _timeSinceJumpRequested = Mathf.Infinity;
            _timeSinceGrounded = Mathf.Infinity;

            _hasJumpedThisFrame = true;
        }

        _timeSinceJumpRequested += deltaTime;
    }

    protected void ProcessJumpPostUpdate(float deltaTime)
    {
        // If we're on a ground surface, we are allowed to jump, so set timeSinceLastAbleToJump to 0
        if (IsGrounded())
        {
            // We make sure that we haven't jumped this frame though, as we might have triggered a jump but still touching ground. Reseting timeSinceLastAbleToJump in that case wouldn't be a good idea
            if (!_hasJumpedThisFrame)
            {
                _timeSinceGrounded = 0f;
            }
        }
        else
        {
            // Keep track of time since we were last able to jump (for grace period)
            _timeSinceGrounded += deltaTime;
        }
    }

    // Finally, my functions!
    /*
    I wanted to make an RV and swap my character controller to the car to control it and make cool car things, 
    but the character controller is limited to capsules and I don't seem to have enough control over it to 
    make it work seemlessly over an RV... I checked the boat exemple to see how I'm supposed to handle vehicles, 
    and while I think I can still pull it off, I doubt it would still count for the homework...
     */
    // also, I'm only commenting from here as I wont pretend I understand all that happens in ICharacterController, 
    // nor that I can comment your code better than you can. 

    /// <summary>
    /// Returns true if the char is on stable ground or swimming. 
    /// This mostly allows to jump while not touching the ground while underwater, 
    /// which is essentially a stroke. Makes a swimming mechanic similar to mario. 
    /// </summary>
    /// <returns>If the character is grounded or not</returns>
    public bool IsGrounded()
    {
        return _kinematicMotor.GroundingStatus.IsStableOnGround || IsSwimming;
    }

    /// <summary>
    /// Pushes player down when called. Usually called in a loop. 
    /// </summary>
    /// <param name="currentVelocity">current velocity of the player</param>
    /// <param name="deltaTime">real-world time that passes between two loop executions</param>
    private void ApplyGravity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity += Gravity * deltaTime;
    }

    /// <summary>
    /// Liquid movement is like ground movement, but with gravity, different stats and you're always grounded
    /// </summary>
    /// <param name="currentVelocity">current velocity of the player</param>
    /// <param name="deltaTime">real-world time that passes between two loop executions</param>
    private void LiquidMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        GroundMovement(ref currentVelocity, deltaTime);
        ApplyGravity(ref currentVelocity, deltaTime);
    }
}
