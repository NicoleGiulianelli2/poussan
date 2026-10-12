using UnityEngine;
 
// Va sui mobili dove il pulcino può sedersi o sdraiarsi (sedie, divano, letto).
// Non serve attaccarlo a mano: lo aggiunge CostruisciArredi alle righe con "seduta: ...".
public class Seduta : MonoBehaviour
{
    public Vector3 puntoLocale;   // dove si siede, rispetto al centro del mobile
    public float rotazione;       // verso dove guarda da seduto: 0 nord, 90 est, 180 sud, 270 ovest
    public bool sdraiato;         // true = si sdraia (letto)
 
    public Vector3 Punto() => transform.TransformPoint(puntoLocale);
    public Vector3 Direzione() => Quaternion.Euler(0f, rotazione, 0f) * Vector3.forward;
 
    // Quando selezioni il mobile nella Hierarchy, nella vista Scene compare
    // un pallino giallo (dove si siede) e una linea (verso dove guarda).
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(Punto(), 0.08f);
        Gizmos.DrawRay(Punto(), Direzione() * 0.4f);
    }
}