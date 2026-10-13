using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sul Fornello (lo mette CostruisciPrimiPiani). Il forno è la parte sotto ai fuochi.
// Davanti al forno premi B: si accende la luce del forno e parte la cottura delle lasagne.
// Quando sono pronte, la luce si spegne e le lasagne compaiono fumanti sul tavolo della cucina.
public class Forno : MonoBehaviour
{
    public Key tasto = Key.B;
    public float distanza = 1.8f;        // quanto deve essere vicino il pulcino (m, in orizzontale)
    public float durata = 20f;           // secondi di cottura
    public GameObject lucina;            // vetro arancione + luce: acceso durante la cottura
    public GameObject lasagne;           // compaiono sul tavolo quando sono pronte
    public Transform pulcino;            // se vuoto lo cerca per nome
    public bool mostraScritte = true;
 
    enum Stato { Spento, InCottura, Pronto }
    Stato stato = Stato.Spento;
    float restante;
    float scrittaPronto;
    Light luce;
 
    void Start()
    {
        if (pulcino == null)
        {
            var p = GameObject.Find("Pulcino");
            if (p != null) pulcino = p.transform;
        }
        if (lucina != null)
        {
            luce = lucina.GetComponentInChildren<Light>();
            lucina.SetActive(false);
        }
        if (lasagne != null) lasagne.SetActive(false);
    }
 
    bool Vicino()
    {
        if (pulcino == null) return false;
        Vector3 a = pulcino.position, b = transform.position;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) <= distanza;
    }
 
    void Update()
    {
        var k = Keyboard.current;
        bool premuto = k != null && k[tasto].wasPressedThisFrame && Vicino();
 
        switch (stato)
        {
            case Stato.Spento:
                if (premuto) Accendi();
                break;
 
            case Stato.InCottura:
                restante -= Time.deltaTime;
                // la luce "respira" un pochino, come un forno vero
                if (luce != null) luce.intensity = 1.4f + Mathf.Sin(Time.time * 3f) * 0.2f;
                if (restante <= 0f) Sforna();
                break;
 
            case Stato.Pronto:
                scrittaPronto -= Time.deltaTime;
                if (premuto) Accendi();   // altre lasagne!
                break;
        }
    }
 
    void Accendi()
    {
        stato = Stato.InCottura;
        restante = durata;
        if (lucina != null) lucina.SetActive(true);
        if (lasagne != null) lasagne.SetActive(false);
    }
 
    void Sforna()
    {
        stato = Stato.Pronto;
        scrittaPronto = 4f;
        if (lucina != null) lucina.SetActive(false);
        if (lasagne != null) lasagne.SetActive(true);
    }
 
    void OnGUI()
    {
        if (!mostraScritte) return;
        string msg = null;
        if (stato == Stato.InCottura)               msg = $"Lasagne in forno... {Mathf.CeilToInt(restante)} s";
        else if (stato == Stato.Pronto && scrittaPronto > 0f) msg = "Lasagne pronte! Sono sul tavolo";
        else if (Vicino())                          msg = $"Premi {tasto} per cuocere le lasagne";
        Avviso.Disegna(msg);
    }
}
 
