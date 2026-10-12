using UnityEngine;
 
// Va sui mobili che, quando li tocchi, aprono un "primo piano" (frigo, fornello, scrivania...).
// Non serve attaccarlo a mano: lo aggiunge CostruisciPrimiPiani.
public class PrimoPiano : MonoBehaviour
{
    public Transform posto;          // dove si mette il pulcino (guarda verso il mobile)
    public Transform guarda;         // il punto che la telecamera inquadra
    public GameObject contenuto;     // cose che compaiono solo nel primo piano (cibo, pentola, libri...)
    public GameObject modello;       // il modello 3D del mobile
    public bool nascondiModello;     // true = durante il primo piano si vede il "dentro" al posto del modello
 
    public float dietro = 0.9f;      // quanto sta la telecamera dietro al pulcino (m)
    public float altezza = 1.4f;     // a che altezza da terra sta la telecamera (m)
 
    public void Mostra(bool si)
    {
        if (contenuto != null) contenuto.SetActive(si);
        if (nascondiModello && modello != null)
            foreach (var r in modello.GetComponentsInChildren<Renderer>()) r.enabled = !si;
 
        // Avvisa gli script dentro il mobile (es. il cassetto della friggitrice si apre/chiude)
        BroadcastMessage("SuPrimoPiano", si, SendMessageOptions.DontRequireReceiver);
    }
 
    // Telecamera "sopra la spalla": dietro al pulcino e più in alto
    public Vector3 PosizioneTelecamera()
    {
        Vector3 p = posto.position - posto.forward * dietro;
        p.y = altezza;
        return p;
    }
 
    // Nella vista Scene: pallino azzurro = posto del pulcino, linea = dove guarda la telecamera
    void OnDrawGizmosSelected()
    {
        if (posto == null || guarda == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(posto.position, 0.1f);
        Gizmos.DrawLine(PosizioneTelecamera(), guarda.position);
    }
}
 