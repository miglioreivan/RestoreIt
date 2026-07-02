using UnityEngine;
using UnityEngine.Events;

public class SpawnRestauro : MonoBehaviour
{
    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO faseCheTriggeraSpawn;

    [Header("Spawn")]
    [SerializeField] private Transform puntoSpawn;

    [Header("Eventi")]
    public UnityEvent<GameObject> onAnforaSpawnata;

    private GameObject anforaSpawnata;

    private void OnEnable()
    {
        tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
    }

    private void OnDisable()
    {
        tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        if (fase != faseCheTriggeraSpawn) return;
        if (tavoloCorrente.vaschettaCorrente?.prefabAnfora == null)
        {
            Debug.LogWarning($"[SpawnRestauro] '{gameObject.name}': nessun prefabAnfora assegnato nella VaschettaSO corrente.");
            return;
        }

        if (anforaSpawnata != null)
            Destroy(anforaSpawnata);

        GameObject prefab = tavoloCorrente.vaschettaCorrente.prefabAnfora;
        anforaSpawnata = Instantiate(prefab, puntoSpawn);
        anforaSpawnata.transform.localPosition = Vector3.zero;
        anforaSpawnata.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = puntoSpawn.lossyScale;
        Vector3 targetScale = prefab.transform.localScale;
        anforaSpawnata.transform.localScale = new Vector3(
            parentScale.x != 0 ? targetScale.x / parentScale.x : targetScale.x,
            parentScale.y != 0 ? targetScale.y / parentScale.y : targetScale.y,
            parentScale.z != 0 ? targetScale.z / parentScale.z : targetScale.z
        );

        onAnforaSpawnata?.Invoke(anforaSpawnata);
    }
}
