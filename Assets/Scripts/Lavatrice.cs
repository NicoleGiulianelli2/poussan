using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sulla Lavatrice (lo mette CostruisciPrimiPiani).
// Vicino alla lavatrice premi T: nell'oblò girano acqua e panni colorati per "durata" secondi
// (negli ultimi secondi centrifuga, e la lavatrice trema). Alla fine compare per terra
// un cesto pieno di panni puliti. Premi T di nuovo per un altro bucato.
public class Lavatrice : MonoBehaviour
{
    public Key tasto = Key.T;
    public float distanza = 1.8f;          // quanto deve essere vicino il pulcino (m, in orizzontale)
    public float durata = 14f;             // secondi di lavaggio
    public float centrifuga = 4f;          // gli ultimi secondi girano più veloce
    public float giriAlSecondo = 0.6f;
 
    public GameObject oblo;                // acqua + panni nell'oblò: visibile durante il lavaggio
    public Transform tamburo;              // gira durante il lavaggio
    public Vector3 asse = Vector3.forward; // asse di rotazione del tamburo (verso dove guarda l'oblò)
    public GameObject cesto;               // compare per terra a fine lavaggio
    public Transform modello;              // trema durante la centrifuga
    public Transform pulcino;              // se vuoto lo cerca per nome
 
    enum Stato { Fermo, Lavaggio, Pronto }
    Stato stato = Stato.Fermo;
    float restante;
    float scrittaPronto;
    Vector3 posModello;
 
    void Start()
    {
        if (pulcino == null)
        {
            var p = GameObject.Find("Pulcino");
            if (p != null) pulcino = p.transform;
        }
        if (modello != null) posModello = modello.localPosition;
        if (oblo != null) oblo.SetActive(false);
        if (cesto != null) cesto.SetActive(false);
    }
 
    bool Vicino()
    {
        if (pulcino == null) return false;
        Vector3 a = pulcino.position, b = transform.position;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) <= distanza;
    }
 
    // Chiamato da PrimoPiano: aprendo il primo piano della lavatrice parte il lavaggio
    public void SuPrimoPiano(bool aperto)
    {
        if (aperto && stato != Stato.Lavaggio) Avvia();
    }
 
    void Update()
    {
        var k = Keyboard.current;
        bool premuto = k != null && k[tasto].wasPressedThisFrame && Vicino();
 
        switch (stato)
        {
            case Stato.Fermo:
                if (premuto) Avvia();
                break;
 
            case Stato.Lavaggio:
                restante -= Time.deltaTime;
                bool veloce = restante < centrifuga;
                if (tamburo != null)
                    tamburo.Rotate(asse, 360f * giriAlSecondo * (veloce ? 4f : 1f) * Time.deltaTime, Space.World);
                if (modello != null)
                    modello.localPosition = posModello + (veloce ? Random.insideUnitSphere * 0.004f : Vector3.zero);
                if (restante <= 0f) Fine();
                break;
 
            case Stato.Pronto:
                scrittaPronto -= Time.deltaTime;
                if (premuto) Avvia();
                break;
        }
    }
 
    void Avvia()
    {
        stato = Stato.Lavaggio;
        restante = durata;
        if (oblo != null) oblo.SetActive(true);
        if (cesto != null) cesto.SetActive(false);
    }
 
    void Fine()
    {
        stato = Stato.Pronto;
        scrittaPronto = 4f;
        if (oblo != null) oblo.SetActive(false);
        if (cesto != null) cesto.SetActive(true);
        if (modello != null) modello.localPosition = posModello;
    }
 
    void OnGUI()
    {
        string msg = null;
        if (stato == Stato.Lavaggio)                       msg = $"La lavatrice finirà in {Mathf.CeilToInt(restante)} s";
        else if (stato == Stato.Pronto && scrittaPronto > 0f) msg = "Bucato finito! Il cesto è per terra";
        else if (Vicino())                                  msg = $"Premi {tasto} per fare il bucato";
        Avviso.Disegna(msg);
    }
}
 