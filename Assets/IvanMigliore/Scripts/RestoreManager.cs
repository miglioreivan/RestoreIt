using UnityEngine;

public class RestoreManager : MonoBehaviour
{
    [Header("Close-Up 3D")]
    [SerializeField] private FirstPersonController player;
    [SerializeField] private Transform target;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private GameObject canvas;

    private Camera playerCamera;
    private Transform startCameraTransform;
    private bool interaction = false;
    
    void StartInteraction()
    {
        playerCamera = Camera.main;
        canvas.SetActive(true);
    }
    
}
