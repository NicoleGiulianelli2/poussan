using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sulla Main Camera. Telecamera dall'alto, stile Animal Crossing:
// segue il pulcino da lontano, sempre con la stessa inclinazione, e non si "attacca" ai muri
// (i muri che coprono il pulcino li fa sparire NascondiMuri).
//   Q / E        → gira la visuale di 90° (scatto morbido), per vedere i mobili dall'altro lato
//   rotellina    → avvicina o allontana
public class TelecameraSegue : MonoBehaviour
{
    public Transform bersaglio;               // il Pulcino (se vuoto lo cerca da solo)
 
    [Header("Posizione")]
    public float distanza = 9f;               // quanto sta lontana dal pulcino (m)
    public float pendenza = 55f;              // quanto guarda dall'alto (gradi): 90 = dritto dall'alto
    public float angolo = 0f;                 // da che lato guarda: 0 = da sud verso nord
    public float altezzaSguardo = 0.5f;       // il punto guardato, rispetto al centro del pulcino
 
    [Header("Comandi")]
    public bool ruotaConQE = true;            // false = visuale sempre fissa
    public float passoRotazione = 90f;        // di quanti gradi gira con Q / E
    public float velocitaRotazione = 6f;      // quanto è rapida la girata
    public float distanzaMin = 5f;
    public float distanzaMax = 14f;
 
    [Header("Comportamento")]
    public float morbidezza = 0.15f;          // più alto = più "elastica" nel seguire
 
    float angoloAttuale;
    Vector3 velocitaCorrente;
 
    void Start()
    {
        if (bersaglio == null)
        {
            var p = GameObject.Find("Pulcino");
            if (p != null) bersaglio = p.transform;
        }
        angoloAttuale = angolo;
        if (bersaglio == null) return;
        Calcola(out Vector3 pos, out Quaternion rot);
        transform.SetPositionAndRotation(pos, rot);   // all'avvio salto subito al posto giusto
    }
 
    void LateUpdate()
    {
        if (bersaglio == null) return;
        LeggiComandi();
 
        // L'angolo arriva piano piano a quello voluto (girata morbida)
        angoloAttuale = Mathf.LerpAngle(angoloAttuale, angolo, 1f - Mathf.Exp(-velocitaRotazione * Time.deltaTime));
 
        Calcola(out Vector3 pos, out Quaternion rot);
        transform.position = Vector3.SmoothDamp(transform.position, pos, ref velocitaCorrente, morbidezza);
        transform.rotation = rot;
    }
 
    // Dove deve stare la telecamera (la usa anche GestorePrimoPiano per tornare indietro)
    public void Calcola(out Vector3 posizione, out Quaternion rotazione)
    {
        rotazione = Quaternion.Euler(pendenza, angoloAttuale, 0f);
        Vector3 punto = bersaglio.position + Vector3.up * altezzaSguardo;
        posizione = punto - rotazione * Vector3.forward * distanza;
    }
 
    void LeggiComandi()
    {
        var k = Keyboard.current;
        var m = Mouse.current;
 
        if (ruotaConQE && k != null)
        {
            if (k.qKey.wasPressedThisFrame) angolo -= passoRotazione;
            if (k.eKey.wasPressedThisFrame) angolo += passoRotazione;
        }
        if (m != null)
        {
            float rotella = m.scroll.ReadValue().y;
            if (Mathf.Abs(rotella) > 0.01f)
                distanza = Mathf.Clamp(distanza - Mathf.Clamp(rotella, -1f, 1f) * 0.5f, distanzaMin, distanzaMax);
        }
    }
}
 