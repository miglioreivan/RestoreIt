using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestisce l'effetto visivo (billboard orientato al giocatore ed oscillazione sinusoidale)
/// per le frecce 3D di indicazione sopra le postazioni di restauro attive.
/// </summary>
public class GestoreFrecceRestauro : MonoBehaviour
{
    [System.Serializable]
    public struct IndicatoreTavolo
    {
        public DropZone_Interaction dropZone;
        public List<GameObject> arrowVisuals;
        [HideInInspector] public List<Vector3> startLocalPositions;
        [HideInInspector] public List<Quaternion> startRotations;
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
            ind.startLocalPositions = new List<Vector3>();
            ind.startRotations = new List<Quaternion>();
            if (ind.arrowVisuals != null)
            {
                for (int j = 0; j < ind.arrowVisuals.Count; j++)
                {
                    var arrow = ind.arrowVisuals[j];
                    if (arrow != null)
                    {
                        ind.startLocalPositions.Add(arrow.transform.localPosition);
                        ind.startRotations.Add(arrow.transform.rotation);
                    }
                    else
                    {
                        ind.startLocalPositions.Add(Vector3.zero);
                        ind.startRotations.Add(Quaternion.identity);
                    }
                }
            }
            indicatori[i] = ind;
        }

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (manoGiocatore == null)
        {
            Debug.LogError($"Componente manoGiocatore non assegnato su {gameObject.name}.");
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
            if (ind.dropZone == null || ind.arrowVisuals == null) continue;

            bool shouldShow = ind.dropZone.canInteract();

            for (int j = 0; j < ind.arrowVisuals.Count; j++)
            {
                var arrow = ind.arrowVisuals[j];
                if (arrow == null) continue;

                if (arrow.activeSelf != shouldShow)
                {
                    arrow.SetActive(shouldShow);
                }

                if (shouldShow && j < ind.startLocalPositions.Count && j < ind.startRotations.Count)
                {
                    // Animazione di oscillazione verticale tramite funzione sinusoidale
                    Vector3 startPos = ind.startLocalPositions[j];
                    float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                    arrow.transform.localPosition = new Vector3(startPos.x, newY, startPos.z);

                    // Rotazione dell'indicatore verso la telecamera (billboard)
                    if (lookAtPlayer && mainCameraTransform != null)
                    {
                        Vector3 targetDir = mainCameraTransform.position - arrow.transform.position;

                        if (lookAtPlayerYOnly)
                        {
                            targetDir.y = 0;
                        }

                        if (targetDir != Vector3.zero)
                        {
                            Quaternion lookRot = Quaternion.LookRotation(targetDir.normalized, Vector3.up);
                            arrow.transform.rotation = lookRot * ind.startRotations[j];
                        }
                    }
                }
            }
        }
    }
}
