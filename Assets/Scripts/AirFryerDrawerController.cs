using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
 
/// <summary>
/// Apre e chiude il cassetto della friggitrice:
///  - da solo, quando si apre/chiude il primo piano della friggitrice (messaggio SuPrimoPiano)
///  - oppure premendo il tasto scelto (F) quando il pulcino è vicino.
/// Lo attacca CostruisciPrimiPiani al modello AirFryer, e collega da solo Drawer e Player.
/// </summary>
public class AirFryerDrawerController : MonoBehaviour
{
    [Header("Collegamenti")]
    [SerializeField] private Transform drawer;
    [SerializeField] private Transform player;
 
    [Header("Interazione")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;
    // Tasto per aprire/chiudere. Non usare Q o E: girano la telecamera.
    // Nell'Inspector puoi sceglierne un altro dal menu a tendina.
    [SerializeField] private Key tasto = Key.F;
 
    [Header("Movimento cassetto (assi locali del suo genitore)")]
    // DIREZIONE in cui esce il cassetto, negli assi del suo genitore.
    // Nel modello di Blender il "davanti" del genitore è -Y (Z è l'alto!). Conta solo la direzione.
    [SerializeField] private Vector3 openOffset = new Vector3(0f, -1f, 0f);
    // Di QUANTO esce, in metri veri (indipendente da come Unity ha scalato il modello)
    [SerializeField, Min(0.01f)] private float apertura = 0.15f;
    [SerializeField, Min(0.05f)] private float animationDuration = 0.5f;
 
    private Vector3 closedLocalPosition;
    private Vector3 offsetLocale;   // openOffset convertito in "unità del genitore"
    private bool isOpen;
    private Coroutine animationRoutine;
 
    private void Awake()
    {
        if (drawer == null)
        {
            Debug.LogError("AirFryerDrawerController: assegna AirFryer_Drawer nel campo Drawer.", this);
            enabled = false;
            return;
        }
 
        closedLocalPosition = drawer.localPosition;
 
        // Il genitore del cassetto è scalato (×100 da Blender, più l'adattamento allo spazio):
        // trasformo "apertura metri nella direzione openOffset" in unità locali del genitore.
        Transform genitore = drawer.parent;
        if (genitore != null && openOffset.sqrMagnitude > 0f)
        {
            Vector3 direzioneMondo = genitore.TransformDirection(openOffset.normalized);
            offsetLocale = genitore.InverseTransformVector(direzioneMondo * apertura);
        }
        else
        {
            offsetLocale = openOffset.normalized * apertura;
        }
    }
 
    private void Update()
    {
        if (player == null || !TastoPremuto())
            return;
 
        // Distance on the floor plane, so a fryer on a counter is still reachable.
        Vector3 playerFloor = player.position;
        Vector3 fryerFloor = transform.position;
        playerFloor.y = 0f;
        fryerFloor.y = 0f;
 
        if (Vector3.Distance(playerFloor, fryerFloor) > interactionDistance)
            return;
 
        Imposta(!isOpen);
    }
 
    // Chiamato da PrimoPiano quando il primo piano della friggitrice si apre o si chiude
    public void SuPrimoPiano(bool aperto)
    {
        if (enabled) Imposta(aperto);
    }
 
    private void Imposta(bool apri)
    {
        isOpen = apri;
        Vector3 destination = closedLocalPosition + (isOpen ? offsetLocale : Vector3.zero);
 
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
 
        animationRoutine = StartCoroutine(MoveDrawer(destination));
    }
 
    private IEnumerator MoveDrawer(Vector3 destination)
    {
        Vector3 start = drawer.localPosition;
        float elapsed = 0f;
 
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = t * t * (3f - 2f * t); // smooth start and stop
            drawer.localPosition = Vector3.LerpUnclamped(start, destination, t);
            yield return null;
        }
 
        drawer.localPosition = destination;
        animationRoutine = null;
    }
 
    private bool TastoPremuto()
    {
        return Keyboard.current != null && Keyboard.current[tasto].wasPressedThisFrame;
    }
}