using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sul Lavandino della cucina (lo mette CostruisciPrimiPiani).
// Vicino al lavandino premi H: l'acqua scorre, c'è la schiuma e i piatti sporchi
// (sul mobile accanto) vengono lavati uno alla volta e finiscono nello scolapiatti.
public class Lavandino : MonoBehaviour
{
    public Key tasto = Key.H;
    public float distanza = 1.8f;           // quanto deve essere vicino il pulcino (m, in orizzontale)
    public float durata = 12f;              // secondi per lavare tutti i piatti
 
    public GameObject acqua;                // getto + acqua nel lavello + schiuma: accesi durante il lavaggio
    public Transform schiuma;               // le bolle "respirano"
    public Transform piattoInLavaggio;      // il piatto che gira nel lavello con la spugna
    public GameObject[] piattiSporchi;      // dal basso in alto: spariscono uno alla volta
    public GameObject[] piattiPuliti;       // compaiono uno alla volta nello scolapiatti
    public Transform pulcino;               // se vuoto lo cerca per nome
 
    enum Stato { Fermo, Lavaggio, Finito }
    Stato stato = Stato.Fermo;
    float restante;
    float scrittaFinito;
    Vector3[] scalaBolle;
    Quaternion rotPiatto;
 
    void Start()
    {
        if (pulcino == null)
        {
            var p = GameObject.Find("Pulcino");
            if (p != null) pulcino = p.transform;
        }
        if (schiuma != null)
        {
            scalaBolle = new Vector3[schiuma.childCount];
            for (int i = 0; i < scalaBolle.Length; i++) scalaBolle[i] = schiuma.GetChild(i).localScale;
        }
        if (piattoInLavaggio != null) rotPiatto = piattoInLavaggio.rotation;
        Mostra(false);
        Pila(piattiSporchi, piattiSporchi != null ? piattiSporchi.Length : 0);
        Pila(piattiPuliti, 0);
    }
 
    bool Vicino()
    {
        if (pulcino == null) return false;
        Vector3 a = pulcino.position, b = transform.position;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) <= distanza;
    }
 
    // Chiamato da PrimoPiano: aprendo il primo piano del lavandino si comincia a lavare
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
            case Stato.Finito:
                if (stato == Stato.Finito) scrittaFinito -= Time.deltaTime;
                if (premuto) Avvia();
                break;
 
            case Stato.Lavaggio:
                restante -= Time.deltaTime;
                Anima();
                if (restante <= 0f) Fine();
                break;
        }
    }
 
    void Avvia()
    {
        stato = Stato.Lavaggio;
        restante = durata;
        Mostra(true);
        Pila(piattiSporchi, piattiSporchi != null ? piattiSporchi.Length : 0);
        Pila(piattiPuliti, 0);
    }
 
    void Fine()
    {
        stato = Stato.Finito;
        scrittaFinito = 4f;
        Mostra(false);
        Pila(piattiSporchi, 0);
        Pila(piattiPuliti, piattiPuliti != null ? piattiPuliti.Length : 0);
    }
 
    void Anima()
    {
        // Quanti piatti sono già stati lavati
        int n = piattiSporchi != null ? piattiSporchi.Length : 0;
        float fatto = 1f - restante / durata;
        int lavati = Mathf.Min(n, Mathf.FloorToInt(fatto * n));
        Pila(piattiSporchi, n - lavati);
        Pila(piattiPuliti, lavati);
 
        // Il piatto nel lavello gira e dondola, come se lo stessimo strofinando
        if (piattoInLavaggio != null)
            piattoInLavaggio.rotation = Quaternion.AngleAxis(Time.time * 120f, Vector3.up)
                                        * Quaternion.AngleAxis(Mathf.Sin(Time.time * 6f) * 12f, Vector3.right) * rotPiatto;
 
        // Bolle che si gonfiano e sgonfiano
        if (schiuma != null && scalaBolle != null)
            for (int i = 0; i < scalaBolle.Length; i++)
                schiuma.GetChild(i).localScale = scalaBolle[i] * (0.8f + 0.25f * Mathf.Sin(Time.time * 4f + i * 1.7f));
    }
 
    void Mostra(bool si)
    {
        if (acqua != null) acqua.SetActive(si);
        if (piattoInLavaggio != null) piattoInLavaggio.gameObject.SetActive(si);
    }
 
    // Attiva i primi "quanti" oggetti della lista, spegne gli altri
    static void Pila(GameObject[] lista, int quanti)
    {
        if (lista == null) return;
        for (int i = 0; i < lista.Length; i++)
            if (lista[i] != null) lista[i].SetActive(i < quanti);
    }
 
    void OnGUI()
    {
        string msg = null;
        if (stato == Stato.Lavaggio)
        {
            int n = piattiSporchi != null ? piattiSporchi.Length : 0;
            int rimasti = n - Mathf.Min(n, Mathf.FloorToInt((1f - restante / durata) * n));
            msg = $"Sto lavando i piatti... ne mancano {rimasti}";
        }
        else if (stato == Stato.Finito && scrittaFinito > 0f) msg = "Piatti puliti! Sono nello scolapiatti";
        else if (Vicino()) msg = $"Premi {tasto} per lavare i piatti";
        Avviso.Disegna(msg);
    }
}
 