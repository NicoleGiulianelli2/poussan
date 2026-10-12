using UnityEngine;
using UnityEngine.InputSystem;
 
// Va attaccato al Pulcino.
// Quando clicchi, lancia un raggio dal mouse dentro la scena e cerca un oggetto Interagibile.
// Se il pulcino è abbastanza vicino, lo "tocca".
public class Interazione : MonoBehaviour
{
    public float distanzaMassima = 1.5f;   // metri
 
    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
 
        // Durante un primo piano, o da seduto, i clic non toccano altro
        var gestore = GestorePrimoPiano.Istanza;
        if (gestore != null && gestore.Attivo) return;
        var sedersi = GetComponent<Sedersi>();
        if (sedersi != null && sedersi.Seduto) return;
 
        // 1. Raggio dalla telecamera, passando per il punto cliccato
        Ray raggio = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
 
        // 2. Tra tutto ciò che attraversa, cerco l'interagibile più vicino alla telecamera
        Interagibile scelto = null;
        Collider colliderScelto = null;
        float migliore = float.MaxValue;
 
        foreach (var hit in Physics.RaycastAll(raggio, 100f))
        {
            var i = hit.collider.GetComponentInParent<Interagibile>();
            if (i != null && hit.distance < migliore)
            {
                migliore = hit.distance;
                scelto = i;
                colliderScelto = hit.collider;
            }
        }
        if (scelto == null) return;
 
        // 3. Il pulcino è abbastanza vicino?
        Vector3 puntoPiuVicino = colliderScelto.ClosestPoint(transform.position);
        if (Vector3.Distance(transform.position, puntoPiuVicino) > distanzaMassima)
        {
            Debug.Log(scelto.nome + " è troppo lontano!");
            return;
        }
 
        // 4. Se è un posto dove sedersi (sedia, divano, letto), mi siedo
        var seduta = scelto.GetComponent<Seduta>();
        if (seduta != null && sedersi != null)
        {
            sedersi.Siediti(seduta);
            return;
        }
 
        // 5. Se ha un primo piano (frigo, fornello, scrivania...), lo apro
        var primoPiano = scelto.GetComponent<PrimoPiano>();
        if (primoPiano != null && gestore != null)
        {
            gestore.Apri(primoPiano);
            return;
        }
 
        // 6. Altrimenti, interazione normale
        scelto.Interagisci();
    }
}
 
