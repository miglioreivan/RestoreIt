using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction restoreAction;
    
    [Header("Impostazioni Movimento")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 10.0f;
    [SerializeField] private float pushForce = 2.0f;
    [SerializeField] private float slideSpeed = 8.0f;

    [Header("Impostazioni Fisica")]
    [SerializeField] private float gravity = -9.81f;
    private float verticalVelocity = 0f;

    [Header("Impostazioni Telecamera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownRange = 80f;
    [SerializeField] private GameObject infoCanvas;

    [Header("Impostazioni Interazione")]
    [SerializeField] private TMPro.TextMeshProUGUI interactionText;
    [SerializeField] private float interactionRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Inventario")]
    [SerializeField] private InventarioManoSO inventario;
    [SerializeField] private Transform handTransform;

    public InventarioManoSO Inventario => inventario;

    private CharacterController characterController;
    private float verticalRotation = 0f;
    private Vector3 hitNormal;
    private bool isSliding;
    private GameObject currentInteractable;
    private IInteractable currentInteractableScript;

    private void Awake()
    {
        if (handTransform == null)
            Debug.LogError("Posizione della mano non trovata.");
        inventario.puntoMano = handTransform;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        characterController.enableOverlapRecovery = true;

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        interactAction = InputSystem.actions.FindAction("Interact");
        restoreAction = InputSystem.actions.FindAction("Restore");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        CheckForInteractable();

        if (currentInteractable && interactAction.WasPressedThisFrame())
        {
            if (currentInteractableScript != null && currentInteractableScript.canInteract())
            {
                currentInteractableScript.StartInteraction();
                interactionText.gameObject.SetActive(false);
            }
        }

        if (restoreAction.WasPressedThisFrame())
        {
            if (infoCanvas != null)
                infoCanvas.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            // Disabilita solo questo script, non il GameObject
            this.enabled = false;
            Debug.Log("InfoCanvas aperto, FirstPersonController disabilitato.");
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            currentInteractable = hit.collider.gameObject;

            if (currentInteractable.TryGetComponent(out currentInteractableScript))
            {
                interactionText.SetText(currentInteractableScript.GetInteractionText());
                interactionText.gameObject.SetActive(true);
            }
            else
            {
                currentInteractableScript = null;
                interactionText.gameObject.SetActive(false);
            }
        }
        else
        {
            currentInteractable = null;
            interactionText.gameObject.SetActive(false);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;

        Vector3 movement = (transform.forward * moveValue.y) + (transform.right * moveValue.x);
        movement *= currentSpeed;

        if (characterController.isGrounded)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hitNormal);
            isSliding = slopeAngle > characterController.slopeLimit;

            if (isSliding)
            {
                Vector3 slopeHorizontalAxis = Vector3.Cross(Vector3.up, hitNormal);
                Vector3 slideDirection = Vector3.Cross(slopeHorizontalAxis, hitNormal).normalized;

                movement.x = slideDirection.x * slideSpeed;
                movement.z = slideDirection.z * slideSpeed;
            }
            else if (verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        characterController.Move(movement * Time.deltaTime);
    }

    private void HandleLook()
    {
        Vector2 lookValue = lookAction.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * (lookValue.x * mouseSensitivity));

        verticalRotation -= lookValue.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hitNormal.y > 0.1f)
            hitNormal = hit.normal;

        Rigidbody body = hit.collider.attachedRigidbody;
        if (body != null && !body.isKinematic)
        {
            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            body.linearVelocity = pushDirection * pushForce;
        }
    }

    public void SetFocus()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}