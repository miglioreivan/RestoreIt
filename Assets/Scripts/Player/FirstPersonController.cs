using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestisce il movimento del giocatore in prima persona, la rotazione della telecamera,
/// la gestione del salto, della gravità e dello scivolamento sulle superfici troppo inclinate.
/// </summary>
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
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float upDownRange = 80f;
    [SerializeField] private GameObject infoCanvas;

    [Header("Impostazioni Interazione")]
    [SerializeField] private TMPro.TextMeshProUGUI interactionText;
    [SerializeField] private float interactionRange = 3.0f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Inventario")]
    [SerializeField] private InventarioManoSO inventario;
    [SerializeField] private Transform handTransform;

    [Header("Impostazioni Audio")]
    [SerializeField] private SoundEffect footstepSound;
    [SerializeField] private float footstepWalkInterval = 0.6f;
    [SerializeField] private float footstepSprintInterval = 0.38f;
    private float footstepTimer = 0f;
    private float _debugMovementLogTimer = 0f; // Throttle per il log di diagnostica movimento

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
        // Inizializza il timer in modo che il primo passo avvenga rapidamente
        footstepTimer = footstepWalkInterval;

        // Debug configurazione audio passi
        if (footstepSound.clip == null)
            Debug.LogWarning("[FirstPersonController] footstepSound.clip non è assegnato nell'Inspector! I passi non saranno udibili.");
        else
            RestoreLogger.Log($"[FirstPersonController] footstepSound configurato: clip='{footstepSound.clip.name}', volume={footstepSound.volume}. Walk interval={footstepWalkInterval}s, Sprint interval={footstepSprintInterval}s.");

        if (footstepSound.volume <= 0f)
            Debug.LogWarning($"[FirstPersonController] footstepSound.volume={footstepSound.volume}! Il volume del SoundEffect nell'Inspector è 0 — i passi non saranno udibili anche se il clip è assegnato.");

        // Gestione automatica dello stato attivo/disattivo in base alla scena
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Museo")
        {
            this.enabled = true;
            RestoreLogger.Log("[FirstPersonController] Scena Museo rilevata: FirstPersonController abilitato all'avvio.");
        }
        else if (sceneName == "Game")
        {
            this.enabled = false;
            RestoreLogger.Log("[FirstPersonController] Scena Game (Restauro) rilevata: FirstPersonController disabilitato all'avvio.");
        }
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
            if (infoCanvas == null)
            {
                RestoreLogger.LogWarning("[FirstPersonController] restoreAction premuto, ma 'infoCanvas' non è assegnato nell'Inspector! Interazione annullata per prevenire il blocco del gioco.");
                return;
            }

            infoCanvas.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            NascondiTestoInterazione();

            // Disabilita solo questo script, non il GameObject
            this.enabled = false;
            RestoreLogger.Log("InfoCanvas aperto, FirstPersonController disabilitato.");
        }
    }

    private void OnEnable()
    {
        // Quando lo script viene abilitato/riabilitato, riaccende l'HUD e i suggerimenti
        MostraTestoInterazione();
    }

    public void NascondiTestoInterazione()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    public void MostraTestoInterazione()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(true);
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

        // Gestione audio dei passi
        bool isMoving = characterController.isGrounded && !isSliding && moveValue.sqrMagnitude > 0.01f;

        // Log diagnostica movimento (throttled a 1 volta al secondo) — rimuovere dopo il debug
        if (moveValue.sqrMagnitude > 0.01f)
        {
            _debugMovementLogTimer += Time.deltaTime;
            if (_debugMovementLogTimer >= 1f)
            {
                _debugMovementLogTimer = 0f;
                RestoreLogger.Log($"[FirstPersonController] Diagnostica passi: isGrounded={characterController.isGrounded}, isSliding={isSliding}, sqrMagnitude={moveValue.sqrMagnitude:F3}, isMoving={isMoving}, footstepTimer={footstepTimer:F3}, interval={(sprintAction.IsPressed() ? footstepSprintInterval : footstepWalkInterval):F3}.");
            }
        }
        else
        {
            _debugMovementLogTimer = 0f;
        }

        if (isMoving)
        {
            float interval = sprintAction.IsPressed() ? footstepSprintInterval : footstepWalkInterval;
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;

                // --- DEBUG PASSI ---
                if (AudioManager.Instance == null)
                {
                    Debug.LogWarning("[FirstPersonController] Passo: AudioManager.Instance è null.");
                }
                else if (footstepSound.clip == null)
                {
                    Debug.LogWarning("[FirstPersonController] Passo: footstepSound.clip è null.");
                }
                else if (footstepSound.volume <= 0f)
                {
                    Debug.LogWarning($"[FirstPersonController] Passo: footstepSound.volume={footstepSound.volume}. Il volume del SoundEffect è 0! Imposta un valore > 0 nell'Inspector.");
                }
                else
                {
                    RestoreLogger.Log($"[FirstPersonController] Passo: riproduzione '{footstepSound.clip.name}', vol={footstepSound.volume}, sfxMult={AudioManager.Instance.SFXVolumeMultiplier}, isSprinting={sprintAction.IsPressed()}.");
                    AudioManager.Instance.Play2D(footstepSound, 0.9f, 1.1f);
                }
                // --- FINE DEBUG ---
            }
        }
        else
        {
            // Mantiene il timer vicino alla soglia per fare in modo che il primo passo parta subito al movimento
            float interval = sprintAction.IsPressed() ? footstepSprintInterval : footstepWalkInterval;
            footstepTimer = interval - 0.05f;
        }
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

    public void RemoveFocus()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}
