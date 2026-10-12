using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
 
// Script da EDITOR: costruisce pavimenti e muri della casa a partire dalla piantina.
// Va messo in una cartella chiamata "Editor" (es. Assets/Editor/CostruisciCasa.cs).
// Uso: menu in alto  CasaPulcino → Costruisci casa.
// Puoi rilanciarlo quando vuoi: cancella la vecchia "Casa" e la ricrea da zero.
//
// Coordinate in METRI. 1 quadretto della piantina = 0,5 m.
// X = verso destra nel disegno, Z = verso l'alto nel disegno.
// Origine (0,0) = angolo in basso a sinistra del rettangolo che contiene tutta la casa
// (cioè sotto la cucina, all'altezza della porta d'ingresso).
public static class CostruisciCasa
{
    // =====================================================================
    // SCALA DELLA CASA: ingrandisce TUTTO (stanze, muri, porte e mobili) in proporzione.
    // 1 = misure della piantina, 1.3 = tutto il 30% più grande. Il pulcino resta com'è,
    // quindi in proporzione la casa sembra più ampia.
    // Dopo averla cambiata: rilancia "Costruisci casa" E "Costruisci arredi".
    // =====================================================================
    public const float Scala = 1.6f;
 
    const float AltezzaMuro = 2.5f;
    const float AltezzaParapetto = 1.0f;
    const float AltezzaPorta = 2.1f;
    const float Spessore = 0.15f;
    const string CartellaMateriali = "Assets/Materials/Casa";
 
    static Transform muri;
    static Transform pavimenti;
    static Transform porte;
    static System.Type tipoPorta;
    static Material matVetro;
 
    // Una finestra registrata: su quale muro (linea), da dove a dove, e a che altezza.
    struct Apertura { public string nome; public bool verticale; public float linea, a, b, davanzale, architrave; }
    static readonly List<Apertura> finestre = new List<Apertura>();
 
    [MenuItem("CasaPulcino/Costruisci casa")]
    public static void Costruisci()
    {
        var vecchia = GameObject.Find("Casa");
        if (vecchia != null) Undo.DestroyObjectImmediate(vecchia);
 
        var casa = new GameObject("Casa");
        Undo.RegisterCreatedObjectUndo(casa, "Costruisci casa");
        pavimenti = Figlio(casa.transform, "Pavimenti");
        muri = Figlio(casa.transform, "Muri");
        porte = Figlio(muri, "Porte");   // dentro "Muri": così NascondiMuri nasconde anche le porte
        matVetro = MatVetro("Vetro finestre");
 
        CreaPavimenti();
        CreaFinestre();   // prima dei muri: i muri devono sapere dove lasciare il buco
        CreaMuri();
        CreaPorte();
        PosizionaTelecamera();
        PosizionaPulcino();
 
        EditorSceneManager.MarkSceneDirty(casa.scene);
        Selection.activeGameObject = casa;
        Debug.Log("Casa costruita. Ricordati di salvare la scena (Ctrl+S).");
    }
 
    // ------------------------------------------------------------------
    // PAVIMENTI: un rettangolo per stanza (xMin, zMin, xMax, zMax)
    // ------------------------------------------------------------------
    static void CreaPavimenti()
    {
        var moquette   = Mat("Pavimento camera",   new Color(0.93f, 0.80f, 0.80f));
        var cucina     = Mat("Pavimento cucina",   new Color(0.96f, 0.92f, 0.76f));
        var bagno      = Mat("Pavimento bagno",    new Color(0.74f, 0.87f, 0.95f));
        var sgabuzzino = Mat("Pavimento sgabuzzino", new Color(0.78f, 0.78f, 0.76f));
        var legno      = Mat("Parquet chiaro",     new Color(0.86f, 0.72f, 0.55f));
        var salotto    = Mat("Parquet salotto",    new Color(0.76f, 0.60f, 0.44f));
        var balcone    = Mat("Pavimento balcone",  new Color(0.70f, 0.70f, 0.67f));
 
        Pavimento("Camera",      0f,    4.35f, 4.8f,   7.7f,  moquette);
        Pavimento("Cucina",      0f,    1.9f,  4.8f,   4.35f, cucina);
        Pavimento("Bagno",       4.8f,  5.3f,  7.6f,   7.7f,  bagno);
        Pavimento("WC",          7.6f,  5.3f,  8.95f,  7.7f,  bagno);
        Pavimento("Sgabuzzino",  8.95f, 5.3f,  10.95f, 7.7f,  sgabuzzino);
        Pavimento("Corridoio",   4.8f,  4.35f, 10.95f, 5.3f,  legno);
        Pavimento("Atrio",       4.8f,  0f,    6.85f,  4.35f, legno);
        Pavimento("Salotto",     6.85f, 0f,    10.95f, 4.35f, salotto);
        Pavimento("Balcone",     10.95f, 0f,   12.85f, 4.35f, balcone);
    }
 
    // ------------------------------------------------------------------
    // MURI: segmenti da (x1, z1) a (x2, z2). Le porte sono i buchi tra un segmento e l'altro.
    // ------------------------------------------------------------------
    static void CreaMuri()
    {
        var m = Mat("Muro", new Color(0.97f, 0.94f, 0.88f));
        var p = Mat("Parapetto", new Color(0.80f, 0.66f, 0.50f));
 
        // --- Perimetro esterno ---
        Muro("Nord",                0f,     7.7f,  10.95f, 7.7f,  m);
        Muro("Ovest",               0f,     1.9f,  0f,     7.7f,  m);
        Muro("Sud cucina",          0f,     1.9f,  4.8f,   1.9f,  m);
        Muro("Ovest atrio",         4.8f,   0f,    4.8f,   3.35f, m); // porta cucina: z 3.35 → 4.35
        Muro("Sud atrio A",         4.8f,   0f,    5.35f,  0f,    m); // ingresso: x 5.35 → 6.4
        Muro("Sud atrio B",         6.4f,   0f,    6.85f,  0f,    m);
        Muro("Sud salotto",         6.85f,  0f,    10.95f, 0f,    m);
        Muro("Est sgabuzzino",      10.95f, 4.35f, 10.95f, 7.7f,  m);
        Muro("Est salotto A",       10.95f, 2.95f, 10.95f, 4.35f, m); // portafinestra: z 1.3 → 2.95
        Muro("Est salotto B",       10.95f, 0f,    10.95f, 1.3f,  m);
 
        // --- Muri interni ---
        Muro("Camera-Cucina",       0f,     4.35f, 4.8f,   4.35f, m);
        Muro("Camera-Bagno",        4.8f,   5.3f,  4.8f,   7.7f,  m); // porta camera: z 4.35 → 5.3
        Muro("Bagno sud A",         4.8f,   5.3f,  6.25f,  5.3f,  m); // porta bagno: x 6.25 → 6.95
        Muro("Bagno sud B",         6.95f,  5.3f,  7.6f,   5.3f,  m);
        Muro("Bagno-WC",            7.6f,   5.3f,  7.6f,   7.7f,  m);
        Muro("WC sud",              7.6f,   5.3f,  8.05f,  5.3f,  m); // porta WC: x 8.05 → 8.95
        Muro("WC-Sgabuzzino",       8.95f,  5.3f,  8.95f,  7.7f,  m);
        Muro("Sgabuzzino sud A",    8.95f,  5.3f,  10.0f,  5.3f,  m); // porta sgabuzzino: x 10.0 → 10.8
        Muro("Sgabuzzino sud B",    10.8f,  5.3f,  10.95f, 5.3f,  m);
        Muro("Corridoio-Atrio",     4.8f,   4.35f, 5.95f,  4.35f, m); // passaggio: x 5.95 → 6.85
        Muro("Corridoio-Salotto",   6.85f,  4.35f, 10.95f, 4.35f, m);
        Muro("Atrio-Salotto A",     6.85f,  3.2f,  6.85f,  4.35f, m); // porta doppia: z 1.35 → 3.2
        Muro("Atrio-Salotto B",     6.85f,  0f,    6.85f,  1.35f, m);
 
        // --- Balcone (parapetto basso) ---
        Muro("Parapetto nord",      10.95f, 4.35f, 12.85f, 4.35f, p, AltezzaParapetto);
        Muro("Parapetto est",       12.85f, 0f,    12.85f, 4.35f, p, AltezzaParapetto);
        Muro("Parapetto sud",       10.95f, 0f,    12.85f, 0f,    p, AltezzaParapetto);
    }
 
    // ------------------------------------------------------------------
    // FINESTRE: Finestra("nome", x1, z1, x2, z2, davanzale, architrave)
    //   x1,z1 → x2,z2   tratto di muro dove sta la finestra (deve stare SU un muro)
    //   davanzale       altezza da terra del bordo basso (facoltativo, 0.9 m)
    //   architrave      altezza del bordo alto (facoltativo, 2.1 m)
    // Il muro si apre da solo: non serve modificare le righe Muro(...).
    // Per spostare una finestra cambia i numeri; per toglierla cancella la riga.
    // ------------------------------------------------------------------
    static void CreaFinestre()
    {
        finestre.Clear();
        Finestra("Finestra camera",     0f,    5.4f, 0f,    6.6f);         // muro ovest della camera
        Finestra("Finestra cucina",     0f,    2.9f, 0f,    3.9f, 1.1f);   // sopra il lavandino
        Finestra("Finestra bagno",      4.95f, 7.7f, 5.55f, 7.7f, 1.3f);   // sopra la vasca, piccola e alta
        // Il salotto non ha finestre: la sua unica apertura è la portafinestra sul balcone (vedi CreaPorte)
    }
 
    // ------------------------------------------------------------------
    // PORTE: Porta("nome", x1, z1, x2, z2, ...)
    //   x1,z1 → x2,z2   il buco della porta nel muro (il cardine sta in x1,z1)
    //   doppia: true    due ante, una per lato (porte larghe)
    //   vetro: true     anta trasparente (portafinestra)
    //   bloccata: true  non si apre mai (es. ingresso: fuori non c'è niente!)
    // Le porte si aprono da sole quando il pulcino si avvicina (script Porta).
    // ------------------------------------------------------------------
    static void CreaPorte()
    {
        tipoPorta = System.Type.GetType("Porta, Assembly-CSharp");
        if (tipoPorta == null)
        {
            Debug.LogWarning("Script 'Porta' non trovato in Assets/Scripts: le porte non vengono create.");
            return;
        }
        var legnoPorta = Mat("Porta",    new Color(0.80f, 0.62f, 0.42f));
        var maniglia   = Mat("Maniglia", new Color(0.75f, 0.75f, 0.78f));
 
        Porta("Porta cucina",     4.8f,  4.35f, 4.8f,  3.35f, legnoPorta, maniglia);
        Porta("Porta camera",     4.8f,  5.3f,  4.8f,  4.35f, legnoPorta, maniglia);
        Porta("Porta corridoio",  6.85f, 4.35f, 5.95f, 4.35f, legnoPorta, maniglia);
        Porta("Porta bagno",      6.25f, 5.3f,  6.95f, 5.3f,  legnoPorta, maniglia);
        Porta("Porta WC",         8.05f, 5.3f,  8.95f, 5.3f,  legnoPorta, maniglia);
        Porta("Porta sgabuzzino", 10.0f, 5.3f,  10.8f, 5.3f,  legnoPorta, maniglia);
        Porta("Porta salotto",    6.85f, 1.35f, 6.85f, 3.2f,  legnoPorta, maniglia, doppia: true);
        // Portafinestra: vetro fino a terra, due ante. Se non vuoi che si apra, aggiungi  , bloccata: true
        Porta("Portafinestra",    10.95f, 1.3f, 10.95f, 2.95f, legnoPorta, maniglia, doppia: true, vetro: true);
        Porta("Ingresso",         5.35f, 0f,    6.4f,  0f,    legnoPorta, maniglia, bloccata: true);
    }
 
    // ------------------------------------------------------------------
    // Funzioni di supporto
    // ------------------------------------------------------------------
    static void Finestra(string nome, float x1, float z1, float x2, float z2,
                         float davanzale = 0.9f, float architrave = 2.1f)
    {
        bool vert = Mathf.Abs(x1 - x2) < 0.001f;
        finestre.Add(new Apertura
        {
            nome = nome, verticale = vert,
            linea = vert ? x1 : z1,
            a = vert ? Mathf.Min(z1, z2) : Mathf.Min(x1, x2),
            b = vert ? Mathf.Max(z1, z2) : Mathf.Max(x1, x2),
            davanzale = davanzale, architrave = architrave
        });
    }
 
    // Un muro dritto. Se sul suo tratto c'è una finestra, lo spezza in pezzi:
    // muro pieno – (sotto la finestra + vetro + sopra la finestra) – muro pieno.
    static void Muro(string nome, float x1, float z1, float x2, float z2, Material mat, float h = AltezzaMuro)
    {
        bool vert = Mathf.Abs(x1 - x2) < 0.001f;
        float linea = vert ? x1 : z1;
        float a = vert ? Mathf.Min(z1, z2) : Mathf.Min(x1, x2);
        float b = vert ? Mathf.Max(z1, z2) : Mathf.Max(x1, x2);
 
        var qui = finestre.FindAll(f => f.verticale == vert && Mathf.Abs(f.linea - linea) < 0.01f
                                        && f.a >= a - 0.01f && f.b <= b + 0.01f && f.architrave < h);
        qui.Sort((p, q) => p.a.CompareTo(q.a));
 
        float cursore = a;
        bool primo = true;
        int n = 0;
        foreach (var f in qui)
        {
            if (f.a > cursore + 0.001f)
                Blocco($"{nome} {++n}", vert, linea, cursore, f.a, 0f, h, mat, primo, false);
            Blocco($"{nome} sotto {f.nome}", vert, linea, f.a, f.b, 0f, f.davanzale, mat, false, false);
            Blocco($"{nome} sopra {f.nome}", vert, linea, f.a, f.b, f.architrave, h, mat, false, false);
            Vetro(f);
            cursore = f.b;
            primo = false;
        }
        if (b > cursore + 0.001f)
            Blocco(qui.Count > 0 ? $"{nome} {++n}" : nome, vert, linea, cursore, b, 0f, h, mat, primo, true);
    }
 
    // Un parallelepipedo di muro, in coordinate della piantina (la Scala la applica lui).
    // estendi...: allunga di mezzo spessore quell'estremità, così negli angoli non restano fessure.
    static void Blocco(string nome, bool vert, float linea, float da, float a, float y0, float y1,
                       Material mat, bool estendiInizio, bool estendiFine)
    {
        float S = Scala;
        float inizio = da * S - (estendiInizio ? Spessore / 2f : 0f);
        float fine   = a * S  + (estendiFine   ? Spessore / 2f : 0f);
        float c = (inizio + fine) / 2f, lung = fine - inizio;
        float yc = (y0 + y1) / 2f * S, alt = (y1 - y0) * S;
 
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // ha già un BoxCollider: il muro è solido
        go.name = nome;
        go.transform.SetParent(muri, false);
        go.transform.localPosition = vert ? new Vector3(linea * S, yc, c) : new Vector3(c, yc, linea * S);
        go.transform.localScale    = vert ? new Vector3(Spessore, alt, lung) : new Vector3(lung, alt, Spessore);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
 
    static void Vetro(Apertura f)
    {
        float S = Scala;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Vetro " + f.nome;
        Object.DestroyImmediate(go.GetComponent<Collider>());  // il vetro è solo da guardare
        go.transform.SetParent(muri, false);
        float c = (f.a + f.b) / 2f * S, lung = (f.b - f.a) * S;
        float yc = (f.davanzale + f.architrave) / 2f * S, alt = (f.architrave - f.davanzale) * S;
        go.transform.localPosition = f.verticale ? new Vector3(f.linea * S, yc, c) : new Vector3(c, yc, f.linea * S);
        go.transform.localScale    = f.verticale ? new Vector3(0.02f, alt, lung) : new Vector3(lung, alt, 0.02f);
        go.GetComponent<Renderer>().sharedMaterial = matVetro;
    }
 
    static void Porta(string nome, float x1, float z1, float x2, float z2, Material legno, Material maniglia,
                      bool doppia = false, bool vetro = false, bool bloccata = false)
    {
        float S = Scala;
        Vector3 A = new Vector3(x1 * S, 0f, z1 * S);
        Vector3 B = new Vector3(x2 * S, 0f, z2 * S);
        float alt = AltezzaPorta * S;
        Material matAnta = vetro ? matVetro : legno;
 
        var gruppo = new GameObject(nome).transform;
        gruppo.SetParent(porte, false);
 
        // Pezzo di muro sopra la porta (l'anta è alta 2.1 m, il muro 2.5 m)
        bool vert = Mathf.Abs(x1 - x2) < 0.001f;
        Blocco(nome + " architrave", vert, vert ? x1 : z1,
               vert ? Mathf.Min(z1, z2) : Mathf.Min(x1, x2), vert ? Mathf.Max(z1, z2) : Mathf.Max(x1, x2),
               AltezzaPorta, AltezzaMuro, Mat("Muro", new Color(0.97f, 0.94f, 0.88f)), false, false);
 
        if (doppia)
        {
            Vector3 M = (A + B) / 2f;
            Anta(gruppo, "Anta 1", A, M, alt, matAnta, maniglia, bloccata);
            Anta(gruppo, "Anta 2", B, M, alt, matAnta, maniglia, bloccata);
        }
        else
        {
            Anta(gruppo, "Anta", A, B, alt, matAnta, maniglia, bloccata);
        }
    }
 
    // Un'anta: un "perno" sul cardine (è lui che ruota) con dentro il pannello e la maniglia.
    static void Anta(Transform gruppo, string nome, Vector3 cardine, Vector3 fine, float alt,
                     Material mat, Material maniglia, bool bloccata)
    {
        Vector3 dir = fine - cardine;
        float w = dir.magnitude - 0.02f;   // 1 cm di gioco per lato
        dir.Normalize();
 
        var perno = new GameObject(nome).transform;
        perno.SetParent(gruppo, false);
        perno.localPosition = cardine + dir * 0.01f;
        perno.localRotation = Quaternion.LookRotation(dir);   // l'anta si estende lungo la "Z" del perno
 
        var pannello = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pannello.name = "Pannello";
        pannello.transform.SetParent(perno, false);
        pannello.transform.localPosition = new Vector3(0f, alt / 2f, w / 2f);
        pannello.transform.localScale = new Vector3(0.05f, alt, w);
        pannello.GetComponent<Renderer>().sharedMaterial = mat;
        // Trigger = non blocca il pulcino (tanto si apre prima che arrivi).
        // Se è bloccata invece deve essere solida.
        pannello.GetComponent<BoxCollider>().isTrigger = !bloccata;
 
        var pomello = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pomello.name = "Maniglia";
        Object.DestroyImmediate(pomello.GetComponent<Collider>());
        pomello.transform.SetParent(perno, false);
        pomello.transform.localPosition = new Vector3(0f, 1.0f * Scala, w - 0.1f);
        pomello.transform.localScale = new Vector3(0.14f, 0.06f, 0.06f);
        pomello.GetComponent<Renderer>().sharedMaterial = maniglia;
 
        var c = perno.gameObject.AddComponent(tipoPorta);
        var so = new SerializedObject(c);
        so.FindProperty("larghezza").floatValue = w;
        so.FindProperty("bloccata").boolValue = bloccata;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
 
    // Materiale trasparente (URP Lit con Surface Type = Transparent).
    // Se esiste già non lo tocca: trasparenza e colore si cambiano dall'Inspector.
    static Material MatVetro(string nome)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CartellaMateriali))
            AssetDatabase.CreateFolder("Assets/Materials", "Casa");
 
        string path = $"{CartellaMateriali}/{nome}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
 
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);   // 1 = Transparent
        mat.SetFloat("_Blend", 0f);     // 0 = Alpha
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetFloat("_Smoothness", 0.9f);
        mat.SetColor("_BaseColor", new Color(0.75f, 0.88f, 0.95f, 0.3f));  // ultimo numero = opacità
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
 
    static void Pavimento(string nome, float xMin, float zMin, float xMax, float zMax, Material mat)
    {
        xMin *= Scala; zMin *= Scala; xMax *= Scala; zMax *= Scala;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        go.transform.SetParent(pavimenti, false);
        const float sp = 0.05f;
        go.transform.localPosition = new Vector3((xMin + xMax) / 2f, -sp / 2f, (zMin + zMax) / 2f);
        go.transform.localScale = new Vector3(xMax - xMin, sp, zMax - zMin);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
 
    // Crea il materiale come file in Assets/Materials/Casa.
    // Se esiste già NON lo sovrascrive: puoi cambiare i colori dall'Inspector e rilanciare lo script.
    static Material Mat(string nome, Color colore)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CartellaMateriali))
            AssetDatabase.CreateFolder("Assets/Materials", "Casa");
 
        string path = $"{CartellaMateriali}/{nome}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colore);
            else mat.color = colore;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }
 
    static Transform Figlio(Transform parent, string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(parent, false);
        return go.transform;
    }
 
    // Telecamera dall'alto e inclinata, stile Animal Crossing, che inquadra tutta la casa.
    // Più avanti la faremo seguire il pulcino.
    static void PosizionaTelecamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Undo.RecordObject(cam.transform, "Posiziona telecamera");
        cam.transform.position = new Vector3(6.4f * Scala, 12f, -4.5f * Scala);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
    }
 
    // Rimette il Pulcino nell'atrio, vicino all'ingresso (serve quando cambia la Scala,
    // altrimenti potrebbe ritrovarsi fuori casa o dentro un muro).
    static void PosizionaPulcino()
    {
        var pulcino = GameObject.Find("Pulcino");
        if (pulcino == null) return;
        Undo.RecordObject(pulcino.transform, "Posiziona pulcino");
        pulcino.transform.position = new Vector3(5.8f * Scala, 0.5f, 1.5f * Scala);
    }
}
 