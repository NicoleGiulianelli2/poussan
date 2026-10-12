using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sulla Main Camera, insieme a TelecameraSegue.
// Quando tocchi un mobile con PrimoPiano: il pulcino si mette davanti al mobile,
// la telecamera vola "sopra la sua spalla" e compare il contenuto (cibo, pentola...).
// Per uscire: Esc, Spazio, WASD o clic destro.
public class GestorePrimoPiano : MonoBehaviour
{
    public static GestorePrimoPiano Istanza { get; private set; }
 
    public float durata = 0.7f;        // secondi del "volo" della telecamera
    public float altezzaPiedi = 0.5f;  // dal centro del pulcino ai piedi
    public Transform pulcino;          // se vuoto lo cerca per nome
 
    public bool Attivo => attivo != null;
 
    PrimoPiano attivo;
    bool inChiusura;
    TelecameraSegue segue;
    CharacterController controller;
    MovimentoPulcino movimento;
 
    Vector3 posDa, posA;
    Quaternion rotDa, rotA;
    float t = 1f;
 
    void Awake()
    {
        Istanza = this;
        segue = GetComponent<TelecameraSegue>();
    }
 
    void Start()
    {
        if (pulcino == null)
        {
            var p = GameObject.Find("Pulcino");
            if (p != null) pulcino = p.transform;
        }
        if (pulcino != null)
        {
            controller = pulcino.GetComponent<CharacterController>();
            movimento = pulcino.GetComponent<MovimentoPulcino>();
        }
    }
 
    public void Apri(PrimoPiano p)
    {
        if (attivo != null || p == null || p.posto == null) return;
        attivo = p;
        inChiusura = false;
 
        // Fermo telecamera che segue e movimento del pulcino
        if (segue != null) segue.enabled = false;
        if (movimento != null) movimento.enabled = false;
        if (controller != null) controller.enabled = false;
 
        // Pulcino davanti al mobile, girato verso di lui
        if (pulcino != null)
        {
            pulcino.position = p.posto.position + Vector3.up * altezzaPiedi;
            pulcino.rotation = Quaternion.LookRotation(p.posto.forward);
        }
 
        p.Mostra(true);
        Vector3 bersaglio = p.guarda != null ? p.guarda.position : p.transform.position;
        Vola(p.PosizioneTelecamera(), bersaglio - p.PosizioneTelecamera());
    }
 
    void Update()
    {
        if (attivo == null) return;
 
        // 1. Se la telecamera sta ancora volando, la muovo
        if (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / durata);
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(posDa, posA, k);
            transform.rotation = Quaternion.Slerp(rotDa, rotA, k);
            if (t >= 1f && inChiusura) Fine();
            return;
        }
 
        // 2. Altrimenti aspetto che il giocatore voglia uscire
        if (!inChiusura && VuoleUscire()) Chiudi();
    }
 
    void Vola(Vector3 posizione, Vector3 direzione)
    {
        posDa = transform.position;
        rotDa = transform.rotation;
        posA = posizione;
        rotA = Quaternion.LookRotation(direzione);
        t = 0f;
    }
 
    bool VuoleUscire()
    {
        var k = Keyboard.current;
        var m = Mouse.current;
        bool tasto = k != null && (k.escapeKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame ||
                                   k.wKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame ||
                                   k.sKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame);
        bool destro = m != null && m.rightButton.wasPressedThisFrame;
        return tasto || destro;
    }
 
    // Torno alla posizione "normale" della telecamera che segue il pulcino
    void Chiudi()
    {
        inChiusura = true;
        if (segue != null && segue.bersaglio != null)
        {
            segue.Calcola(out Vector3 pos, out Quaternion rot);
            Vola(pos, rot * Vector3.forward);
        }
        else
        {
            Vola(transform.position, transform.forward);
        }
    }
 
    void Fine()
    {
        attivo.Mostra(false);
        if (controller != null) controller.enabled = true;
        if (movimento != null) movimento.enabled = true;
        if (segue != null) segue.enabled = true;
        attivo = null;
        inChiusura = false;
    }
}