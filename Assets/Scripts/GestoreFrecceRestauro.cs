using UnityEngine;
using System.Collections.Generic;

public class GestoreFrecceRestauro : MonoBehaviour
{
    [System.Serializable]
    public struct IndicatoreTavolo
    {
        public DropZone_Interaction dropZone;
        public GameObject arrowVisual;
        [HideInInspector] public Vector3 startLocalPosition;
    }

    [Header("Riferimenti")]
    [SerializeField] private InventarioManoSO manoGiocatore;
    [SerializeField] private List<IndicatoreTavolo> indicatori = new List<IndicatoreTavolo>();

    [Header("Movimento Galleggiamento")]
    [SerializeField] private float floatSpeed = 3.0f;
    [SerializeField] private float floatAmplitude = 0.15f;

    [Header("Rotazione (Billboard)")]
    [SerializeField] private bool lookAtPlayer = true;
    [SerializeField] private bool lookAtPlayerYOnly = true;

    private Transform mainCameraTransform;

    private void Start()
    {
        for (int i = 0; i < indicatori.Count; i++)
        {
            var ind = indicatori[i];
            if (ind.arrowVisual != null)
            {
                ind.startLocalPosition = ind.arrowVisual.transform.localPosition;
            }
            indicatori[i] = ind;
        }

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (manoGiocatore == null)
        {
            Debug.LogError($"[GestoreFrecceRestauro] '{gameObject.name}': manoGiocatore non assegnato.");
        }
    }

    private void Update()
    {
        if (Camera.main != null && mainCameraTransform == null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        for (int i = 0; i < indicatori.Count; i++)
        {
            var ind = indicatori[i];
            if (ind.arrowVisual == null || ind.dropZone == null) continue;

            bool shouldShow = ind.dropZone.canInteract();

            if (ind.arrowVisual.activeSelf != shouldShow)
            {
                ind.arrowVisual.SetActive(shouldShow);
            }

            if (shouldShow)
            {
                // Galleggiamento
                float newY = ind.startLocalPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                ind.arrowVisual.transform.localPosition = new Vector3(ind.startLocalPosition.x, newY, ind.startLocalPosition.z);

                // Billboard
                if (lookAtPlayer && mainCameraTransform != null)
                {
                    Vector3 targetDir = mainCameraTransform.position - ind.arrowVisual.transform.position;

                    if (lookAtPlayerYOnly)
                    {
                        targetDir.y = 0;
                    }

                    if (targetDir != Vector3.zero)
                    {
                        ind.arrowVisual.transform.rotation = Quaternion.LookRotation(targetDir.normalized);
                    }
                }
            }
        }
    }
}
