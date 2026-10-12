using UnityEngine;
 
// Fa salire in loop le palline bianche che ha come figli: il vapore che esce dalla pentola.
// Lo aggiunge CostruisciPrimiPiani sul fornello.
public class Vapore : MonoBehaviour
{
    public float altezza = 0.45f;    // quanto salgono (m)
    public float durata = 2.2f;      // secondi per salire
    public float grandezza = 0.09f;  // grandezza massima di una pallina
 
    Transform[] bolle;
 
    void OnEnable()
    {
        bolle = new Transform[transform.childCount];
        for (int i = 0; i < bolle.Length; i++) bolle[i] = transform.GetChild(i);
    }
 
    void Update()
    {
        if (bolle == null) return;
        for (int i = 0; i < bolle.Length; i++)
        {
            float fase = (float)i / bolle.Length;                 // ogni pallina parte sfasata
            float t = Mathf.Repeat(Time.time / durata + fase, 1f); // 0 = in basso, 1 = in alto
            float ondeggia = Mathf.Sin((t + fase) * Mathf.PI * 4f) * 0.03f;
            bolle[i].localPosition = new Vector3(ondeggia, t * altezza, Mathf.Cos(fase * 6.28f) * 0.03f);
            bolle[i].localScale = Vector3.one * (Mathf.Sin(t * Mathf.PI) * grandezza); // cresce e poi sparisce
        }
    }
}
 
