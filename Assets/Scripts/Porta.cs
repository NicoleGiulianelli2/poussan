using UnityEngine;
 
// Va sul "perno" (il cardine) di ogni anta. Non serve attaccarlo a mano: lo mette CostruisciCasa.
// L'anta si apre da sola quando il pulcino si avvicina, dalla parte opposta a lui,
// e si richiude quando si allontana.
public class Porta : MonoBehaviour
{
    public float larghezza = 1f;        // larghezza dell'anta (m)
    public float distanza = 1.6f;       // a che distanza dal pulcino si apre
    public float angoloAperto = 95f;    // quanto si apre (gradi)
    public float velocita = 300f;       // gradi al secondo
    public bool bloccata = false;       // true = non si apre mai
    public Transform pulcino;           // se vuoto, lo cerca da solo per nome ("Pulcino")
 
    Quaternion chiusa;   // rotazione da chiusa
    Vector3 centro;      // centro dell'anta da chiusa
    float angolo;        // angolo attuale: 0 = chiusa
    float verso = 1f;    // +1 o -1: da che parte si apre
 
    void Start()
    {
        chiusa = transform.localRotation;
        centro = transform.position + transform.forward * (larghezza / 2f);
        if (pulcino == null)
        {
            var p = GameObject.Find("Pulcino");
            if (p != null) pulcino = p.transform;
        }
    }
 
    void Update()
    {
        if (bloccata || pulcino == null) return;
 
        // 1. Il pulcino è vicino? (misuro solo in orizzontale)
        Vector3 d = pulcino.position - centro;
        d.y = 0f;
        bool vicino = d.magnitude < distanza;
 
        // 2. Se la porta è chiusa e il pulcino arriva, decido il verso:
        //    ruotando di +angolo l'anta va verso la "destra" del perno,
        //    quindi se il pulcino sta a destra la apro verso sinistra (e viceversa).
        if (vicino && Mathf.Abs(angolo) < 0.01f)
            verso = Vector3.Dot(d, transform.right) > 0f ? -1f : 1f;
 
        // 3. Ruoto piano piano verso l'angolo che voglio
        float obiettivo = vicino ? angoloAperto * verso : 0f;
        angolo = Mathf.MoveTowards(angolo, obiettivo, velocita * Time.deltaTime);
        transform.localRotation = chiusa * Quaternion.Euler(0f, angolo, 0f);
    }
}
 