using UnityEngine;
using System.Collections;

public class RestoreManager : MonoBehaviour, IInteractable
{
    [Header("Close-Up 3D")]
    [SerializeField] private MonoBehaviour player;
    [SerializeField] private Transform target;
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private GameObject canvas;

    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private Transform targetAssemblaggio;
    [SerializeField] private FaseRestauroSO faseAssemblaggio;

    private Camera playerCamera;
    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;
    private Transform startCameraParent;
    private bool isRestoring = false;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    private void Start()
    {
        if (TryGetComponent<Collider>(out var col))
            col.enabled = (tavoloCorrente != null && tavoloCorrente.vaschettaCorrente != null);

        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnVaschettaPosata += OnVaschettaPosata;
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
        }
    }

    private void OnDestroy()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnVaschettaPosata -= OnVaschettaPosata;
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
        }
    }

    private void OnVaschettaPosata(VaschettaSO vaschetta)
    {
        if (TryGetComponent<Collider>(out var col))
            col.enabled = (vaschetta != null);
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        if (fase == faseAssemblaggio && targetAssemblaggio != null)
        {
            StartCoroutine(MoveCamera(targetAssemblaggio.position, targetAssemblaggio.rotation));
        }
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
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

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

        if (this.gameObject.TryGetComponent<StrumentoPulizia>(out var sp))
        {
            sp.CountVisiblePixel();
            sp.SetMouseCursor();
            sp.IniziaMinigame();
        }
    }

    public void StopInteraction()
    {
        canvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (this.gameObject.TryGetComponent<StrumentoPulizia>(out var sp))
        {
            sp.TerminaMinigame();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

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
        if (TryGetComponent<Collider>(out var col)) col.enabled = true;

        isRestoring = false;
    }
}