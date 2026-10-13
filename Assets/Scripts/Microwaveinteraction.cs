using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
 
/// <summary>
/// Microonde: premi M vicino (o tocca il microonde per il primo piano):
/// la porta si apre, entra una ciotola, la porta si chiude, il piatto gira con la luce accesa,
/// poi la porta si riapre e la zuppa fuma. Premi M di nuovo per togliere la ciotola.
///
/// Lo attacca CostruisciPrimiPiani al modello "Microwave".
/// Il modello viene da Blender: è ruotato (Z = alto) e scalato ×100. Per questo lo script
/// NON usa gli assi locali del modello ma quelli del mondo, e le misure in metri veri.
///
/// VETRO: nel modello il vetro blu è appoggiato su una porta piena e il corpo è un blocco pieno,
/// quindi rendere trasparente solo il vetro mostrerebbe... la porta dietro.
/// Con "Rendi Visibile Dentro" lo script sostituisce corpo e porta con versioni "cave"
/// (pareti + cornice intorno al vetro): così attraverso il vetro si vede davvero dentro.
/// </summary>
public class MicrowaveInteraction : MonoBehaviour
{
    [Header("Parti del modello (se vuote: ricerca per nome)")]
    public Transform doorHinge;
    public Transform door;
    public Transform glass;
    public Transform plate;
    public Transform body;
    public Transform player;
 
    [Header("Interazione")]
    public Key tasto = Key.M;
    [Min(0.1f)] public float interactionDistance = 1.8f;
    public bool showMessage = true;
 
    [Header("Tempi")]
    [Range(30f, 160f)] public float openAngle = 105f;
    [Min(0.05f)] public float doorSeconds = 0.65f;
    [Min(0.05f)] public float bowlInsertionSeconds = 0.85f;
    [Min(0.1f)] public float cookingSeconds = 8f;
    public float plateDegreesPerSecond = 120f;
 
    [Header("Ciotola (misure vere, in metri)")]
    [Min(0.02f)] public float bowlDiameter = 0.13f;
    [Min(0.01f)] public float bowlHeight = 0.055f;
 
    [Header("Vetro e interno")]
    public bool rendiVisibileDentro = true;
    [Range(0f, 1f)] public float glassOpacity = 0.22f;
    public Color glassTint = new Color(0.30f, 0.50f, 0.85f, 1f);
    public Color coloreInterno = new Color(0.93f, 0.93f, 0.88f, 1f);
 
    enum Fase { Fermo, Carico, Cottura, Pronto }
    Fase fase = Fase.Fermo;
    float restante;
    bool pronto;
 
    Vector3 su = Vector3.up;   // alto nel mondo
    Vector3 avanti;            // verso dove guarda lo sportello (nel mondo)
    Vector3 lato;              // orizzontale, perpendicolare ad avanti
    Quaternion pernoChiuso;
    float segno = 1f;
    Transform pernoPiatto;
    Quaternion piattoIniziale;
    GameObject ciotola;
    Light luceInterna;
 
    void Awake()
    {
        if (!doorHinge) doorHinge = Trova(transform, "DoorHinge");
        if (!door) door = Trova(transform, "Microwave_door");
        if (!glass) glass = Trova(transform, "Microwave_Glass");
        if (!plate) plate = Trova(transform, "Microwave_Plate");
        if (!body) body = Trova(transform, "Microwave_body");
        if (!player)
        {
            var p = GameObject.Find("Pulcino");
            if (p) player = p.transform;
        }
        if (!door || !doorHinge || !body)
        {
            Debug.LogError("[Microonde] Non trovo DoorHinge / Microwave_door / Microwave_body nel modello.", this);
            enabled = false;
            return;
        }
        if (!door.IsChildOf(doorHinge)) door.SetParent(doorHinge, true);
 
        // 1. Direzioni nel MONDO: "avanti" = dal centro del corpo verso la porta
        Bounds bCorpo = Ingombro(body);
        Bounds bPorta = Ingombro(door);
        Vector3 d = bPorta.center - bCorpo.center;
        d.y = 0f;
        avanti = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0f, 0f)
                                                 : new Vector3(0f, 0f, Mathf.Sign(d.z));
        lato = Vector3.Cross(su, avanti);
 
        // 2. Interno visibile e vetro trasparente
        if (rendiVisibileDentro) SvuotaCorpoEPorta(bCorpo, bPorta);
        if (glass) VetroTrasparente();
 
        // 3. Da che parte ruota la porta per aprirsi VERSO DI NOI
        pernoChiuso = doorHinge.rotation;
        Vector3 v = bPorta.center - doorHinge.position;
        Vector3 vRuotato = Quaternion.AngleAxis(openAngle, su) * v;
        segno = Vector3.Dot(vRuotato - v, avanti) >= 0f ? 1f : -1f;
 
        // 4. Perno al centro del piatto: gira intorno all'alto del MONDO
        if (plate)
        {
            Bounds bPiatto = Ingombro(plate);
            pernoPiatto = new GameObject("Microwave_PlateSpin_Runtime").transform;
            pernoPiatto.position = bPiatto.center;
            pernoPiatto.SetParent(transform, true);
            plate.SetParent(pernoPiatto, true);
            piattoIniziale = pernoPiatto.rotation;
        }
 
        // 5. Lucina interna (accesa durante la cottura)
        var lgo = new GameObject("Luce_Microonde_Runtime");
        lgo.transform.position = bCorpo.center;
        lgo.transform.SetParent(transform, true);
        luceInterna = lgo.AddComponent<Light>();
        luceInterna.type = LightType.Point;
        luceInterna.range = Mathf.Max(bCorpo.size.x, bCorpo.size.z) * 1.2f;
        luceInterna.intensity = 1.5f;
        luceInterna.color = new Color(1f, 0.9f, 0.6f);
        luceInterna.enabled = false;
 
        pronto = true;
    }
 
    void Update()
    {
        if (!pronto) return;
        if (fase == Fase.Cottura) restante -= Time.deltaTime;
        if (!Vicino() || !TastoPremuto()) return;
 
        if (fase == Fase.Fermo) StartCoroutine(Ciclo());
        else if (fase == Fase.Pronto) StartCoroutine(Svuota());
    }
 
    // Chiamato da PrimoPiano: aprendo il primo piano del microonde parte la cottura
    public void SuPrimoPiano(bool aperto)
    {
        if (aperto && pronto && fase == Fase.Fermo) StartCoroutine(Ciclo());
    }
 
    bool Vicino()
    {
        if (!player) return false;
        Vector3 a = player.position, b = transform.position;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) <= interactionDistance;
    }
 
    bool TastoPremuto() => Keyboard.current != null && Keyboard.current[tasto].wasPressedThisFrame;
 
    // ------------------------------------------------------------------
    // Il ciclo completo
    // ------------------------------------------------------------------
    IEnumerator Ciclo()
    {
        fase = Fase.Carico;
        yield return Porta(true);
 
        // Ciotola: parte davanti al microonde ed entra fino al centro del piatto
        ciotola = CreaCiotola();
        Vector3 dentro = SopraPiatto() + su * (bowlHeight * 0.5f + 0.003f);
        Vector3 fuori = dentro + avanti * 0.35f;
        yield return Muovi(ciotola.transform, fuori, dentro, bowlInsertionSeconds);
        if (pernoPiatto) ciotola.transform.SetParent(pernoPiatto, true);   // gira insieme al piatto
 
        yield return Porta(false);
 
        fase = Fase.Cottura;
        restante = cookingSeconds;
        luceInterna.enabled = true;
        while (restante > 0f)
        {
            if (pernoPiatto) pernoPiatto.Rotate(su, plateDegreesPerSecond * Time.deltaTime, Space.World);
            yield return null;
        }
        luceInterna.enabled = false;
 
        yield return Porta(true);
        AggiungiVapore(ciotola.transform);
        fase = Fase.Pronto;
    }
 
    IEnumerator Svuota()
    {
        fase = Fase.Carico;
        if (ciotola) Destroy(ciotola);
        ciotola = null;
        yield return Porta(false);
        if (pernoPiatto) pernoPiatto.rotation = piattoIniziale;
        fase = Fase.Fermo;
    }
 
    IEnumerator Porta(bool apri)
    {
        Quaternion da = doorHinge.rotation;
        Quaternion a = apri ? Quaternion.AngleAxis(openAngle * segno, su) * pernoChiuso : pernoChiuso;
        for (float t = 0f; t < 1f; t += Time.deltaTime / doorSeconds)
        {
            float k = t * t * (3f - 2f * t);
            doorHinge.rotation = Quaternion.Slerp(da, a, k);
            yield return null;
        }
        doorHinge.rotation = a;
    }
 
    IEnumerator Muovi(Transform obj, Vector3 da, Vector3 a, float secondi)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / secondi)
        {
            float k = t * t * (3f - 2f * t);
            obj.position = Vector3.Lerp(da, a, k);
            yield return null;
        }
        obj.position = a;
    }
 
    Vector3 SopraPiatto()
    {
        if (plate)
        {
            Bounds b = Ingombro(plate);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
        Bounds c = Ingombro(body);
        return new Vector3(c.center.x, c.min.y + 0.03f, c.center.z);
    }
 
    // ------------------------------------------------------------------
    // Corpo e porta "cavi": così dal vetro si vede dentro
    // ------------------------------------------------------------------
    void SvuotaCorpoEPorta(Bounds bCorpo, Bounds bPorta)
    {
        var matInterno = MaterialeColore(coloreInterno);
        var rendCorpo = body.GetComponent<Renderer>();
        var matEsterno = rendCorpo ? rendCorpo.sharedMaterial : matInterno;
 
        // Coordinate "comode": u = di lato, v = in alto, w = in avanti
        float u0 = Min(bCorpo, lato), u1 = Max(bCorpo, lato);
        float v0 = bCorpo.min.y, v1 = bCorpo.max.y;
        float w0 = Min(bCorpo, avanti), w1 = Max(bCorpo, avanti);
        float t = Mathf.Min(bCorpo.size.y, Mathf.Min(bCorpo.size.x, bCorpo.size.z)) * 0.05f;   // spessore pareti
 
        // Pareti del corpo (esterno del colore originale, interno chiaro)
        Pannello("Retro",  u0, u1, v0, v1, w0, w0 + t, matEsterno, transform);
        Pannello("Fianco", u0, u0 + t, v0, v1, w0, w1, matEsterno, transform);
        Pannello("Fianco", u1 - t, u1, v0, v1, w0, w1, matEsterno, transform);
        Pannello("Sopra",  u0, u1, v1 - t, v1, w0, w1, matEsterno, transform);
        Pannello("Sotto",  u0, u1, v0, v0 + t, w0, w1, matEsterno, transform);
        // Rivestimento interno chiaro
        float e = t * 0.2f;
        Pannello("Interno retro", u0 + t, u1 - t, v0 + t, v1 - t, w0 + t, w0 + t + e, matInterno, transform);
        Pannello("Interno sotto", u0 + t, u1 - t, v0 + t, v0 + t + e, w0 + t, w1, matInterno, transform);
 
        // Facciata davanti: cornice intorno allo spazio della porta (il pannello dei comandi resta)
        float pu0 = Min(bPorta, lato), pu1 = Max(bPorta, lato);
        float pv0 = bPorta.min.y, pv1 = bPorta.max.y;
        Cornice("Facciata", u0, u1, v0, v1, pu0, pu1, pv0, pv1, w1 - t, w1, matEsterno, transform);
        if (rendCorpo) rendCorpo.enabled = false;
 
        // Porta: cornice intorno al vetro, figlia della cerniera così si apre con lei
        if (glass)
        {
            Bounds bVetro = Ingombro(glass);
            var rendPorta = door.GetComponent<Renderer>();
            var matPorta = rendPorta ? rendPorta.sharedMaterial : matEsterno;
            Cornice("Porta", pu0, pu1, pv0, pv1,
                    Min(bVetro, lato), Max(bVetro, lato), bVetro.min.y, bVetro.max.y,
                    Min(bPorta, avanti), Max(bPorta, avanti), matPorta, doorHinge);
            if (rendPorta) rendPorta.enabled = false;
        }
    }
 
    // Quattro pezzi intorno a un "buco" rettangolare
    void Cornice(string nome, float u0, float u1, float v0, float v1,
                 float bu0, float bu1, float bv0, float bv1, float w0, float w1, Material mat, Transform genitore)
    {
        Pannello(nome + " sx",    u0, bu0, v0, v1, w0, w1, mat, genitore);
        Pannello(nome + " dx",    bu1, u1, v0, v1, w0, w1, mat, genitore);
        Pannello(nome + " sotto", bu0, bu1, v0, bv0, w0, w1, mat, genitore);
        Pannello(nome + " sopra", bu0, bu1, bv1, v1, w0, w1, mat, genitore);
    }
 
    // Un parallelepipedo dato in coordinate (lato, alto, avanti) del mondo
    void Pannello(string nome, float u0, float u1, float v0, float v1, float w0, float w1, Material mat, Transform genitore)
    {
        if (u1 - u0 < 0.0005f || v1 - v0 < 0.0005f || w1 - w0 < 0.0005f) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome + "_Runtime";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = lato * ((u0 + u1) / 2f) + su * ((v0 + v1) / 2f) + avanti * ((w0 + w1) / 2f);
        go.transform.localScale = Abs(lato) * (u1 - u0) + su * (v1 - v0) + Abs(avanti) * (w1 - w0);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.transform.SetParent(genitore, true);
    }
 
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    static float Min(Bounds b, Vector3 asse) => Mathf.Min(Vector3.Dot(b.min, asse), Vector3.Dot(b.max, asse));
    static float Max(Bounds b, Vector3 asse) => Mathf.Max(Vector3.Dot(b.min, asse), Vector3.Dot(b.max, asse));
 
    void VetroTrasparente()
    {
        Color c = glassTint;
        c.a = glassOpacity;
        var mat = MaterialeTrasparente(c);
        foreach (var r in glass.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
    }
 
    // ------------------------------------------------------------------
    // Ciotola e vapore (misure vere, nel mondo)
    // ------------------------------------------------------------------
    GameObject CreaCiotola()
    {
        var obj = new GameObject("Microwave_Bowl_Runtime");
        var mf = obj.AddComponent<MeshFilter>();
        var mr = obj.AddComponent<MeshRenderer>();
        int seg = 32;
        float h = bowlHeight, r = bowlDiameter / 2f;
        Vector2[] profilo =
        {
            new Vector2(0f, -h / 2f), new Vector2(r * 0.66f, -h / 2f), new Vector2(r, h / 2f),
            new Vector2(r * 0.88f, h / 2f), new Vector2(r * 0.58f, -h / 2f + 0.009f), new Vector2(0f, -h / 2f + 0.009f)
        };
        var vert = new Vector3[profilo.Length * (seg + 1)];
        var tri = new int[(profilo.Length - 1) * seg * 6];
        for (int j = 0; j < profilo.Length; j++)
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                vert[j * (seg + 1) + i] = new Vector3(profilo[j].x * Mathf.Cos(a), profilo[j].y, profilo[j].x * Mathf.Sin(a));
            }
        int ti = 0;
        for (int j = 0; j < profilo.Length - 1; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = j * (seg + 1) + i, b = a + seg + 1, c = a + 1, d = b + 1;
                tri[ti++] = a; tri[ti++] = b; tri[ti++] = c;
                tri[ti++] = b; tri[ti++] = d; tri[ti++] = c;
            }
        var mesh = new Mesh { name = "Ciotola", vertices = vert, triangles = tri };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        mr.sharedMaterial = MaterialeColore(new Color(0.94f, 0.74f, 0.49f));
 
        // Zuppa dentro la ciotola
        var zuppa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(zuppa.GetComponent<Collider>());
        zuppa.transform.SetParent(obj.transform, false);
        zuppa.transform.localPosition = new Vector3(0f, h * 0.25f, 0f);
        zuppa.transform.localScale = new Vector3(r * 1.7f, 0.002f, r * 1.7f);
        zuppa.GetComponent<Renderer>().sharedMaterial = MaterialeColore(new Color(0.9f, 0.45f, 0.2f));
        return obj;
    }
 
    void AggiungiVapore(Transform ciotola)
    {
        if (!ciotola) return;
        var go = new GameObject("Vapore_Runtime");
        go.transform.SetParent(ciotola, false);
        go.transform.localPosition = Vector3.up * bowlHeight * 0.5f;
        go.transform.rotation = Quaternion.LookRotation(Vector3.up);   // il cono emette lungo +Z → in alto
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new Color(1f, 1f, 1f, 0.5f);
        main.maxParticles = 100;
        var em = ps.emission;
        em.rateOverTime = 16f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 12f;
        sh.radius = bowlDiameter * 0.25f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = MaterialeVapore();
        ps.Play();
    }
 
    // ------------------------------------------------------------------
    // Materiali creati al volo
    // ------------------------------------------------------------------
    static Material MaterialeColore(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (!sh) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
        return m;
    }
 
    static Material MaterialeTrasparente(Color c)
    {
        var m = MaterialeColore(c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Smoothness", 0.9f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }
 
    static Material MaterialeVapore()
    {
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!sh) sh = Shader.Find("Particles/Standard Unlit");
        if (!sh) sh = Shader.Find("Sprites/Default");
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 2f)));
            }
        tex.Apply();
        var m = new Material(sh);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }
 
    // ------------------------------------------------------------------
    // Utilità
    // ------------------------------------------------------------------
    static Transform Trova(Transform t, string nome)
    {
        if (string.Equals(t.name, nome, System.StringComparison.OrdinalIgnoreCase)) return t;
        foreach (Transform c in t)
        {
            var f = Trova(c, nome);
            if (f) return f;
        }
        return null;
    }
 
    // Ingombro nel MONDO (solo il renderer dell'oggetto, non dei figli)
    static Bounds Ingombro(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        if (r) return r.bounds;
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var x in rs) b.Encapsulate(x.bounds);
        return b;
    }
 
    void OnGUI()
    {
        if (!showMessage || !pronto) return;
        string msg = null;
        switch (fase)
        {
            case Fase.Fermo:   if (Vicino()) msg = $"Premi {tasto} per scaldare la zuppa nel microonde"; break;
            case Fase.Carico:  if (Vicino()) msg = "Preparazione del microonde..."; break;
            case Fase.Cottura: msg = $"Il microonde finirà in {Mathf.CeilToInt(Mathf.Max(0f, restante))} s"; break;
            case Fase.Pronto:  if (Vicino()) msg = $"Zuppa calda! Premi {tasto} per togliere la ciotola"; break;
        }
        Avviso.Disegna(msg);
    }
}
 
