using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerControls playerControls;
    private PlayerControls.MovementActions playerActions;

    [Header("Input Refrences")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Variables")]
    public float speed = 65;
    public float
        maxSlopeAngle = 45,
        heightOffset = 1.1f,
        offsetStrength = 200,
        offsetDamper = 10,
        dragRate = 5;

    [Header("Turning Variables")]
    public float sensitivity = 30;
    private float lookRotation;

    public Vector2 moveInput;

    private bool
        isGrounded,
        isMoving,
        isUp,
        disableMovement;

    public bool _isMoving { get { return isMoving; } }
    public bool _isGrounded { get { return isGrounded; } }
    public bool _isUp { get { return isUp; } }

    private Rigidbody rb;

    private Camera cam;

    private RaycastHit groundHit;

    public delegate void DisableControls();
    public static DisableControls disableControls;

    private void Awake()
    {
        playerControls = new PlayerControls();
        playerActions = playerControls.Movement;

        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();

        playerActions.Pause.performed += CallPause;
    }

    private void OnEnable()
    {
        playerActions.Enable();
        disableControls += DisableMovement;
    }
    private void OnDisable()
    {
        playerActions.Disable();
        disableControls -= DisableMovement;
    }
    private void Update()
    {
        //Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, 1.1f, groundLayer);

        //Movement check
        if (playerActions.Move.IsInProgress())
            isMoving = true;
        else
            isMoving = false;

        if (lookRotation > 80 || lookRotation < -80)
            isUp = true;
        else
            isUp = false;
    }
    private void LateUpdate()
    {
        if (disableMovement)
            return;

        Look();
    }
    private void FixedUpdate()
    {
        if (disableMovement)
            return;

        if (isGrounded)
            Movement();
    }
    private void Movement()
    {
        Vector3 moveForce = MoveDirection() * speed;

        float slopeAngle = Vector3.Angle(Vector3.up, groundHit.normal);

        Vector3 yOffsetForce = Vector3.zero;

        if (slopeAngle <= maxSlopeAngle)
        {
            float yOffsetError = (heightOffset - groundHit.distance);
            //?
            float yOffsetVelocity = Vector3.Dot(Vector3.up, rb.velocity);
            yOffsetForce = Vector3.up * (yOffsetError * offsetStrength - yOffsetVelocity * offsetDamper);
        }
        Vector3 combinedForces = moveForce + yOffsetForce;
        Vector3 dampingForces = rb.velocity * dragRate;

        rb.AddForce((combinedForces - dampingForces) * (100 * Time.fixedDeltaTime));
    }
    private Vector3 MoveDirection()
    {
        //Reads the x and y inputs
        moveInput = playerActions.Move?.ReadValue<Vector2>() ?? Vector2.zero;

        //Determines direction
        Vector3 moveDirection =
            (Vector3.ProjectOnPlane(transform.forward, Vector3.up) * moveInput.y +
            Vector3.ProjectOnPlane(transform.right, Vector3.up) * moveInput.x);
        moveDirection.Normalize();

        return moveDirection;
    }
    void Look()
    {
        Vector2 lookForce = playerActions.Look.ReadValue<Vector2>();
        //Turning the gameobject
        gameObject.transform.Rotate(Vector3.up * lookForce.x * sensitivity / 100);

        //Rotating the Camera

        lookRotation += (-lookForce.y * sensitivity / 100);
        lookRotation = Mathf.Clamp(lookRotation, -90, 90);
        cam.transform.eulerAngles = new Vector3(lookRotation, cam.transform.eulerAngles.y, cam.transform.eulerAngles.z);
    }
    /// <summary>
    /// Calls pause delegate from pause menu script
    /// </summary>
    /// <param name="ctx"></param>
    void CallPause(InputAction.CallbackContext ctx)
    {
        PauseMenu.pause?.Invoke();
    }
    /// <summary>
    /// Disabled movement when called, renabled when called again
    /// </summary>
    void DisableMovement() => disableMovement = !disableMovement;
    public void DisableScript() => enabled = false;
    public void EnableScript() => enabled = true;
}
