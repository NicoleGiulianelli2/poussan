using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NascondiMuri : MonoBehaviour
{
    public Transform bersaglio;   // il Pulcino
    public Transform muri;        // il contenitore "Muri" dentro "Casa"

    // I muri che ho nascosto nell'ultimo frame
    readonly List<Renderer> nascosti = new List<Renderer>();

    void LateUpdate()
    {
        if (bersaglio == null || muri == null) return;

        // 1. Rimetto visibili i muri nascosti nel frame precedente
        foreach (var r in nascosti)
            r.shadowCastingMode = ShadowCastingMode.On;
        nascosti.Clear();

        // 2. Il "laser": dalla telecamera al pulcino
        Vector3 origine = transform.position;
        Vector3 verso = bersaglio.position - origine;
        RaycastHit[] colpiti = Physics.RaycastAll(origine, verso.normalized, verso.magnitude);

        // 3. Ogni muro colpito diventa invisibile (ma continua a fare ombra)
        foreach (var hit in colpiti)
        {
            if (!hit.transform.IsChildOf(muri)) continue;   // ignoro tutto ciò che non è un muro

            var r = hit.collider.GetComponent<Renderer>();
            if (r == null) continue;

            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            nascosti.Add(r);
        }
    }
}