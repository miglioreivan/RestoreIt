using UnityEngine;

public static class RestorationUtils
{
    /// <summary>
    /// Cambia il parent del transform target azzerando posizione e rotazione locale
    /// ma compensando la scala locale per preservare la scala globale preesistente.
    /// </summary>
    public static void ReparentPreservingScale(Transform target, Transform parent)
    {
        if (target == null) return;

        Vector3 targetWorldScale = target.lossyScale;

        target.SetParent(parent, false);
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;

        if (parent != null)
        {
            Vector3 parentLossyScale = parent.lossyScale;
            target.localScale = new Vector3(
                parentLossyScale.x != 0 ? targetWorldScale.x / parentLossyScale.x : targetWorldScale.x,
                parentLossyScale.y != 0 ? targetWorldScale.y / parentLossyScale.y : targetWorldScale.y,
                parentLossyScale.z != 0 ? targetWorldScale.z / parentLossyScale.z : targetWorldScale.z
            );
        }
        else
        {
            target.localScale = targetWorldScale;
        }
    }

    /// <summary>
    /// Genera una copia leggibile e modificabile in formato RGBA32 a partire da una texture sorgente.
    /// </summary>
    public static Texture2D CopiaTextureInRGBA32(Texture2D sorgente)
    {
        if (sorgente == null) return null;

        RenderTexture rt = RenderTexture.GetTemporary(
            sorgente.width, 
            sorgente.height, 
            0, 
            RenderTextureFormat.Default, 
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(sorgente, rt);

        RenderTexture precedenteActive = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D nuovaTexture = new Texture2D(sorgente.width, sorgente.height, TextureFormat.RGBA32, false);
        nuovaTexture.name = sorgente.name + "_Leggibile";
        
        nuovaTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        nuovaTexture.Apply();

        RenderTexture.active = precedenteActive;
        RenderTexture.ReleaseTemporary(rt);

        return nuovaTexture;
    }

    /// <summary>
    /// Esegue il wrapping delle coordinate UV tra 0 e 1.
    /// </summary>
    public static Vector2 WrapUV(Vector2 uv)
    {
        uv.x = uv.x - Mathf.Floor(uv.x);
        uv.y = uv.y - Mathf.Floor(uv.y);
        return uv;
    }

    /// <summary>
    /// Verifica se l'oggetto o la sua gerarchia contiene il tag componente OggettoRestaurato.
    /// </summary>
    public static bool IsOggettoRestaurato(GameObject go)
    {
        if (go == null) return false;
        return go.GetComponent<OggettoRestaurato>() != null ||
               go.GetComponentInParent<OggettoRestaurato>() != null ||
               go.GetComponentInChildren<OggettoRestaurato>() != null;
    }

    /// <summary>
    /// Coroutine che fa vibrare un GameObject per simulare consolidamento, pulizia o interazione.
    /// </summary>
    public static System.Collections.IEnumerator VibraOggetto(GameObject target, float duration = 0.6f, float magnitude = 0.012f)
    {
        if (target == null) yield break;

        AudioManager.Instance?.PlayFineFase();

        Vector3 originalPos = target.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;

            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;
            target.transform.position = originalPos + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            target.transform.position = originalPos;
        }
    }

    /// <summary>
    /// Calcola la progressione normalizzata tra 0 e 1, prevenendo divisioni per zero.
    /// </summary>
    /// <param name="dipinti">Il numero di elementi o pixel completati.</param>
    /// <param name="totale">Il numero totale di elementi o pixel richiesti.</param>
    /// <returns>Percentuale normalizzata come float tra 0.0f e 1.0f.</returns>
    public static float CalcolaProgressione(int dipinti, int totale)
    {
        if (totale <= 0) return 0f;
        return Mathf.Clamp01((float)dipinti / totale);
    }
}
