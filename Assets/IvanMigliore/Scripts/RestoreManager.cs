using UnityEngine;
using System.Collections;

public class RestoreManager : MonoBehaviour, IInteractable
{
    [Header("Close-Up 3D")]
    [SerializeField] private MonoBehaviour player; 
    [SerializeField] private Transform target;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private GameObject canvas;

    private Camera playerCamera;
    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;
    private Transform startCameraParent;
    private bool isRestoring = false;

    private void Awake()
    {
        playerCamera = Camera.main; 
    }

    public bool canInteract()
    {
        return !isRestoring; 
    }

    public string GetInteractionText()
    {
        return "[E] Usa postazione di restauro";
    }

    public void StartInteraction()
    {
        if (!canInteract()) return;

        isRestoring = true;
        
        if (player != null) player.enabled = false;
        
        TryGetComponent<Collider>(out var col);
        col.enabled = false;
        
        

        startCameraParent = playerCamera.transform.parent;
        startCameraPosition = playerCamera.transform.position;
        startCameraRotation = playerCamera.transform.rotation;

        playerCamera.transform.SetParent(null, true);

        canvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(MoveCamera(target.position, target.rotation));
    }

    private IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot)
    {
        float elapsedTime = 0f;
        Vector3 startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;

        while (elapsedTime < transitionDuration)
        {
            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / transitionDuration);
            playerCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, elapsedTime / transitionDuration);
            
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        playerCamera.transform.position = targetPos;
        playerCamera.transform.rotation = targetRot;
    }

    public void StopInteraction()
    {
        canvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        StartCoroutine(MoveCameraBack());
    }

    private IEnumerator MoveCameraBack()
    {
        float elapsedTime = 0f;
        Vector3 currentPos = playerCamera.transform.position;
        Quaternion currentRot = playerCamera.transform.rotation;

        while (elapsedTime < transitionDuration)
        {
            playerCamera.transform.position = Vector3.Lerp(currentPos, startCameraPosition, elapsedTime / transitionDuration);
            playerCamera.transform.rotation = Quaternion.Lerp(currentRot, startCameraRotation, elapsedTime / transitionDuration);
            
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        playerCamera.transform.SetParent(startCameraParent, true);
        
        playerCamera.transform.position = startCameraPosition;
        playerCamera.transform.rotation = startCameraRotation;

        if (player != null) player.enabled = true;
        isRestoring = false;
    }
}