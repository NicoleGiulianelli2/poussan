using UnityEngine;
using UnityEngine.InputSystem;
 
// Va sul Pulcino. Lo fa sedere (o sdraiare) su un mobile che ha lo script Seduta.
// Si attiva cliccando il mobile (ci pensa Interazione). Per rialzarsi: WASD o Spazio.
public class Sedersi : MonoBehaviour
{
    public float distanzaPiediCentro = 0.5f;  // dal centro del pulcino ai suoi piedi
    public float affonda = 0.1f;              // quanto "affonda" nel cuscino quando è seduto
    public float altezzaSdraiato = 0.3f;      // quanto sta sopra il materasso da sdraiato
 
    public bool Seduto { get; private set; }
 
    CharacterController controller;
    MovimentoPulcino movimento;
    Vector3 posizionePrima;
    Quaternion rotazionePrima;
 
    void Start()
    {
        controller = GetComponent<CharacterController>();
        movimento = GetComponent<MovimentoPulcino>();
    }
 
    public void Siediti(Seduta s)
    {
        if (Seduto) return;
 
        // Mi ricordo dov'ero, per tornarci quando mi alzo
        posizionePrima = transform.position;
        rotazionePrima = transform.rotation;
 
        // Spengo movimento e collisioni: altrimenti il mobile "spingerebbe fuori" il pulcino
        if (movimento != null) movimento.enabled = false;
        if (controller != null) controller.enabled = false;
 
        Vector3 p = s.Punto();
        if (s.sdraiato)
        {
            // Pancia all'insù, testa verso la testiera (dalla parte opposta al "davanti" del letto)
            transform.position = p + Vector3.up * altezzaSdraiato;
            transform.rotation = Quaternion.LookRotation(Vector3.up, -s.Direzione());
        }
        else
        {
            transform.position = p + Vector3.up * (distanzaPiediCentro - affonda);
            transform.rotation = Quaternion.LookRotation(s.Direzione());
        }
        Seduto = true;
    }
 
    void Update()
    {
        if (!Seduto) return;
        var t = Keyboard.current;
        if (t == null) return;
 
        if (t.wKey.wasPressedThisFrame || t.aKey.wasPressedThisFrame ||
            t.sKey.wasPressedThisFrame || t.dKey.wasPressedThisFrame ||
            t.spaceKey.wasPressedThisFrame)
        {
            Alzati();
        }
    }
 
    public void Alzati()
    {
        transform.position = posizionePrima;
        transform.rotation = rotazionePrima;
        if (controller != null) controller.enabled = true;
        if (movimento != null) movimento.enabled = true;
        Seduto = false;
    }
}
 