using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
 
// =====================================================================================
//  COSTRUISCI ARREDI  —  script da EDITOR (va in Assets/Editor)
//  Menu:  CasaPulcino → Costruisci arredi.   Si può rilanciare quando vuoi:
//  cancella l'oggetto "Arredi" e lo ricrea da zero (le modifiche fatte a mano si perdono!).
// =====================================================================================
//
//  OGNI MOBILE è una riga  Pezzo(...)  con questi parametri, in ordine:
//
//    "Nome"                 come apparirà nella Hierarchy (e il nome mostrato al tocco)
//    xMin, zMin, xMax, zMax lo spazio che occupa sulla pianta, in metri
//                           (X verso destra, Z verso l'alto nel disegno, come CostruisciCasa)
//    yMin, yMax             da che altezza a che altezza (yMin > 0 = appoggiato su qualcosa)
//    materiale              colore del blocco, usato solo se il modello non c'è
//    toccabile              true = il pulcino può interagirci (riceve Interagibile)
//    "modello"              nome del file 3D (senza .fbx). Basta l'inizio del nome.
//                           Con | si danno alternative: "bedSingle|bed_double"
//                           Se lo lasci vuoto o non lo trova: blocco colorato.
//    rotazione              verso dove guarda il DAVANTI del mobile:
//                             0 = nord (alto nel disegno)   180 = sud (verso la telecamera)
//                            90 = est (destra)              270 = ovest (sinistra)
//    adatta                 Adatta.Proporzioni = non deforma il modello: lo rimpicciolisce/
//                                                ingrandisce finché entra, e lo appoggia
//                                                con la schiena al lato "dietro" dello spazio
//                           Adatta.Riempi      = lo stira per riempire esattamente lo spazio
//                                                (buono per mobili squadrati: tavoli, cucina)
//    specchia               true = lo ribalta destra/sinistra (es. penisola del divano sull'altro lato)
//
//  PARAMETRI FACOLTATIVI CON NOME (si aggiungono in fondo, in qualsiasi ordine):
//    cambia: "carpet", con: blu   sostituisce, SOLO in questo mobile, il materiale del modello
//                                 che si chiama "carpet" con il materiale 'blu'.
//                                 (Per sapere come si chiamano i materiali di un modello:
//                                  selezionalo nella scena → Mesh Renderer → Materials.)
//    seduta: 0.45f                il pulcino ci si può sedere; il numero è l'altezza del sedile (m).
//                                 Si siede al centro dello spazio, guardando verso 'rotazione'.
//    postoX: 10.2f, postoZ: 3.8f  (facoltativi) punto preciso dove sedersi, se non è il centro
//    sdraiato: true               si sdraia invece di sedersi (letto)
//    Per vedere dove si siederà: seleziona il mobile nella Hierarchy, compare un pallino giallo.
//
//  I modelli vengono cercati in TUTTA la cartella Assets (KayKit, Kenney, quelli che aggiungerai).
// =====================================================================================
 
public enum Adatta { Proporzioni, Riempi }
 
public static class CostruisciArredi
{
    // Correzione per pacchetto: nei modelli Kenney il "davanti" è dalla parte opposta
    // (lo schienale di sedie e divani sta verso +Z), quindi li giro di 180°.
    // Se TUTTI i modelli di un pacchetto risultano girati al contrario, cambia questo numero.
    const float RotazioneKenney = 180f;
    const float RotazioneKayKit = 0f;
 
    // Correzioni per singoli modelli orientati diversamente dagli altri del loro pacchetto.
    // Esempio: { "nomeModello", 90f },   (per ora nessuna: tutti i Kenney usati seguono la stessa regola)
    static readonly System.Collections.Generic.Dictionary<string, float> CorrezioniModelli =
        new System.Collections.Generic.Dictionary<string, float>
        {
        };
 
    const string CartellaMateriali = "Assets/Materials/Arredi";
    const string NomeMaterialeKayKit = "M_KayKit";
 
    static Transform stanza;
    static System.Type tipoInteragibile;
    static System.Type tipoSeduta;
    static Material matKayKit;
    static Shader shaderURP;
 
    [MenuItem("CasaPulcino/Costruisci arredi")]
    public static void Costruisci()
    {
        var vecchi = GameObject.Find("Arredi");
        if (vecchi != null) Undo.DestroyObjectImmediate(vecchi);
 
        var arredi = new GameObject("Arredi");
        Undo.RegisterCreatedObjectUndo(arredi, "Costruisci arredi");
 
        tipoInteragibile = System.Type.GetType("Interagibile, Assembly-CSharp");
        tipoSeduta = System.Type.GetType("Seduta, Assembly-CSharp");
        if (tipoSeduta == null)
            Debug.LogWarning("Script 'Seduta' non trovato: sedie e divano non saranno 'sedibili'.");
        if (tipoInteragibile == null)
            Debug.LogWarning("Script 'Interagibile' non trovato: i mobili vengono creati senza.");
        matKayKit = TrovaMateriale(NomeMaterialeKayKit);
        shaderURP = Shader.Find("Universal Render Pipeline/Lit");
 
        // --- Colori dei blocchi (usati solo dove manca il modello) ---
        var legno       = Mat("Legno",          new Color(0.72f, 0.53f, 0.36f));
        var bianco      = Mat("Bianco",         new Color(0.95f, 0.95f, 0.95f));
        var acciaio     = Mat("Acciaio",        new Color(0.62f, 0.64f, 0.68f));
        var pianoLavoro = Mat("Piano lavoro",   new Color(0.86f, 0.84f, 0.78f));
        var ceramica    = Mat("Ceramica",       new Color(0.99f, 0.99f, 1.00f));
        var tessuto     = Mat("Tessuto divano", new Color(0.95f, 0.62f, 0.38f));
        var ferro       = Mat("Ferro balcone",  new Color(0.35f, 0.42f, 0.38f));
        var vetro       = Mat("Vetro",          new Color(0.75f, 0.85f, 0.90f));
        var blu         = Mat("Tessuto blu",    new Color(0.30f, 0.45f, 0.80f));
        var cuscini     = Mat("Tessuto cuscini",new Color(0.98f, 0.82f, 0.35f));
        var nero        = Mat("Nero",           new Color(0.15f, 0.15f, 0.17f));
        var specchio    = Mat("Specchio",       new Color(0.92f, 0.95f, 0.97f));
        var rosso       = Mat("Rosso",          new Color(0.85f, 0.30f, 0.30f));
        var verde       = Mat("Verde",          new Color(0.35f, 0.70f, 0.40f));
        var giallo      = Mat("Giallo",         new Color(1.00f, 0.90f, 0.40f));
        var viola       = Mat("Viola",          new Color(0.60f, 0.40f, 0.80f));
        specchio.SetFloat("_Metallic", 1f);      // metallico + liscio = riflette come uno specchio
        specchio.SetFloat("_Smoothness", 0.97f);
        EditorUtility.SetDirty(specchio);
 
        // --- Camera ---
        stanza = Figlio(arredi.transform, "Camera");
        Pezzo("Scrivania",        0.5f,  6.95f, 1.6f,  7.6f,  0f, 0.75f, legno,  true,  "desk",               180, Adatta.Riempi);
        Pezzo("Sedia scrivania",  0.8f,  6.15f, 1.35f, 6.7f,  0f, 0.45f, legno,  false, "chairDesk",            0, seduta: 0.48f);
        Pezzo("Letto",            1.85f, 5.4f,  3.65f, 7.6f,  0f, 0.5f,  bianco, true,  "bedDouble",          180, seduta: 0.5f, sdraiato: true);
        Pezzo("Armadio",          4.12f, 5.45f, 4.72f, 7.6f,  0f, 2.1f,  legno,  true,  "bookcaseClosedDoors", 270, Adatta.Riempi);
        Pezzo("Cassettiera",      0.1f,  4.43f, 1.45f, 4.95f, 0f, 0.85f, legno,  true,  "cabinetBedDrawer",     0, Adatta.Riempi);
        Pezzo("Attaccapanni",     1.8f,  4.43f, 3.1f,  4.55f, 1.4f, 1.55f, legno, false);
 
        // --- Cucina ---
        stanza = Figlio(arredi.transform, "Cucina");
        Pezzo("Tavolo cucina",    1.5f,  3.65f, 3.2f,  4.27f, 0f, 0.75f, legno,       true,  "table",           0, Adatta.Riempi);
        Pezzo("Sedia cucina 1",   1.0f,  3.71f, 1.45f, 4.21f, 0f, 0.45f, legno, false, "chair",  90, seduta: 0.45f);
        Pezzo("Sedia cucina 2",   3.25f, 3.71f, 3.7f,  4.21f, 0f, 0.45f, legno, false, "chair", 270, seduta: 0.45f);
        Pezzo("Fornello",         1.0f,  1.98f, 1.9f,  2.58f, 0f, 0.9f,  acciaio,     true,  "kitchenStove",    0, Adatta.Riempi);
        Pezzo("Lavatrice",        1.95f, 1.98f, 2.6f,  2.58f, 0f, 0.85f, bianco,      true,  "washer",          0);
        Pezzo("Piano di lavoro 1",2.65f, 1.98f, 3.3f,  2.58f, 0f, 0.9f,  pianoLavoro, false, "kitchenCabinet",  0, Adatta.Riempi);
        Pezzo("Piano di lavoro 2",3.3f,  1.98f, 3.95f, 2.58f, 0f, 0.9f,  pianoLavoro, false, "kitchenCabinet",  0, Adatta.Riempi);
        Pezzo("Mobile cucina 1",  0.08f, 2.35f, 0.68f, 3.0f,  0f, 0.9f,  pianoLavoro, false, "kitchenCabinet", 90, Adatta.Riempi);
        Pezzo("Lavandino cucina", 0.08f, 3.0f,  0.68f, 3.65f, 0f, 0.9f,  pianoLavoro, true,  "kitchenSink",    90, Adatta.Riempi);
        Pezzo("Mobile cucina 2",  0.08f, 3.65f, 0.68f, 4.27f, 0f, 0.9f,  pianoLavoro, false, "kitchenCabinet", 90, Adatta.Riempi);
        // Asse che copre lavatrice e mobili alla stessa altezza (la lavatrice è più bassa).
        // Sopra ci stanno friggitrice, bollitore e microonde: per questo partono tutti da 0.94.

        Pezzo("Asse",             1.95f, 1.98f, 3.95f, 2.62f, 0.9f, 0.94f, pianoLavoro, false);
        Pezzo("Friggitrice ad aria", 2.1f, 2.1f, 2.45f, 2.45f, 0.94f, 1.26f, nero, true, "AirFryer", 0);
        Pezzo("Bollitore", 2.7f, 2.15f, 3.0f, 2.45f, 0.94f, 1.25f, acciaio, true, "Kettle", 0); // se beccuccio guarda muro metti 180 invece che 0
        Pezzo("Microonde", 3.3f, 2.05f, 3.85f, 2.45f, 0.94f, 1.24f, bianco, true, "Microwave", 0);
        Pezzo("Frigo",            4.0f,  1.98f, 4.7f,  2.63f, 0f, 1.8f,  bianco,      true,  "kitchenFridge",    0);
 
        // --- Bagno ---
        stanza = Figlio(arredi.transform, "Bagno");
        Pezzo("Vasca",            4.88f, 5.9f,  5.6f,  7.62f, 0f, 0.55f, ceramica, true, "bathtub",         90);
        Pezzo("Lavandino bagno",  6.2f,  7.1f,  6.9f,  7.62f, 0f, 0.85f, ceramica, true, "bathroomSink",   180);
        Pezzo("Mobiletto",        6.95f, 7.2f,  7.5f,  7.62f, 0f, 0.8f,  legno,    true, "bathroomCabinet", 180);
        // Specchio grande sopra il lavandino: la parte "metal" del modello diventa materiale Specchio.
        Pezzo("Specchio",         6.05f, 7.4f,  7.05f, 7.62f, 1.0f, 2.15f, specchio, true, "bathroomMirror", 180, Adatta.Riempi,
              cambia: "metal", con: specchio);
        // "Fotocamera a 360°" che cattura il bagno all'avvio, così lo specchio riflette la stanza.
        SondaRiflessi("Riflessi bagno", new Vector3(6.2f, 1.25f, 6.5f), new Vector3(2.8f, 2.5f, 2.4f));
 
        // --- WC ---
        stanza = Figlio(arredi.transform, "WC");
        Pezzo("WC",               7.95f, 6.9f,  8.6f,  7.62f, 0f, 0.85f, ceramica, true, "toilet",          180, seduta: 0.42f, postoZ: 7.2f);
 
        // --- Sgabuzzino ---
        stanza = Figlio(arredi.transform, "Sgabuzzino");
        Pezzo("Boiler",           9.3f,  7.15f, 9.9f,  7.62f, 1.0f, 1.9f, bianco, true);
        Pezzo("Mobile sgabuzzino",10.45f, 5.9f, 10.87f, 7.1f, 0f, 1.8f,  legno,  true, "bookcaseClosedDoors", 270, Adatta.Riempi);
 
        // --- Atrio ---
        stanza = Figlio(arredi.transform, "Atrio");
        Pezzo("Scarpiera",        4.88f, 1.2f,  5.25f, 2.85f, 0f, 1.0f,  legno, true, "cabinetBedDrawer", 90, Adatta.Riempi);
 
        // --- Salotto ---
        stanza = Figlio(arredi.transform, "Salotto");
        Pezzo("Tavolo salotto",   7.4f,  3.1f,  8.6f,  3.8f,  0f, 0.75f, vetro,   true,  "tableGlass",       0, Adatta.Riempi);
        Pezzo("Sedia salotto 1",  7.5f,  3.82f, 8.0f,  4.27f, 0f, 0.45f, verde,   false, "chair",          180, seduta: 0.45f, cambia: "wood", con: verde);
        Pezzo("Sedia salotto 2",  8.05f, 3.82f, 8.55f, 4.27f, 0f, 0.45f, legno,   false, "chair",          180, seduta: 0.45f, cambia: "wood", con: rosso);
        Pezzo("Sedia salotto 3",  7.5f,  2.6f,  8.0f,  3.08f, 0f, 0.45f, legno,   false, "chair",            0, seduta: 0.45f);
        Pezzo("Sedia salotto 4",  8.05f, 2.6f,  8.55f, 3.08f, 0f, 0.45f, legno,   false, "chair",            0, seduta: 0.45f);
       
        Pezzo("Divano",           8.9f,  2.25f, 10.85f, 4.27f, 0f, 0.8f, blu, true,  "loungeSofaLong", 180, Adatta.Proporzioni,
              specchia: true, cambia: "carpet", con: blu,
              seduta: 0.42f, postoX: 10.2f, postoZ: 3.8f);
        // Cuscini appoggiati sulla seduta, contro lo schienale.
        // Se fluttuano o affondano nel divano, aggiusta yMin (0.46); se sono troppo avanti/indietro, zMin e zMax.
        Pezzo("Cuscino 1",        9.25f, 3.58f, 9.7f,  3.76f, 0.46f, 0.9f, cuscini, false, "pillow", 180, cambia: "carpet", con: cuscini);
        Pezzo("Cuscino 2",        10.05f,3.58f, 10.5f, 3.76f, 0.46f, 0.9f, cuscini, false, "pillow", 180, cambia: "carpet", con: cuscini);
        Pezzo("Puff",             9.7f,  0.3f,  10.7f, 1.25f, 0f, 0.45f, tessuto, true,  "loungeSofaOttoman", 0, cambia: "carpet", con: tessuto, seduta: 0.42f);
        Pezzo("Mobile lungo",     7.0f,  0.08f, 8.5f,  0.5f,  0f, 0.6f,  legno,   true,  "cabinetTelevision", 0, Adatta.Riempi);
        Pezzo("Libreria",         8.65f, 0.08f, 9.4f,  0.45f, 0f, 1.8f,  legno,   true,  "bookcaseClosedWide", 0, Adatta.Riempi);
 
        // --- Balcone ---
        stanza = Figlio(arredi.transform, "Balcone");
        Pezzo("Sedia balcone 1",  11.75f, 3.0f, 12.3f, 3.55f, 0f, 0.45f, ferro, false, "chairRounded", 180, seduta: 0.45f);
        Pezzo("Tavolino balcone", 11.6f, 1.75f, 12.4f, 2.45f, 0f, 0.5f,  ferro, true,  "tableRound",     0);
        Pezzo("Sedia balcone 2",  11.8f, 0.65f, 12.35f, 1.2f, 0f, 0.45f, ferro, false, "chairRounded",   0, seduta: 0.45f);
 
        CostruisciPrimiPiani.Aggiungi();
        AssetDatabase.SaveAssets(); // salva i materiali convertiti
        EditorSceneManager.MarkSceneDirty(arredi.scene);
        Selection.activeGameObject = arredi;
        Debug.Log("Arredi costruiti. Ricordati di salvare la scena.");
    }
 
    // ------------------------------------------------------------------
    // Un mobile
    // ------------------------------------------------------------------
    static void Pezzo(string nome, float xMin, float zMin, float xMax, float zMax,
                      float yMin, float yMax, Material matBlocco, bool interagibile,
                      string modello = null, float rotazione = 0f,
                      Adatta adatta = Adatta.Proporzioni, bool specchia = false,
                      string cambia = null, Material con = null,
                      float seduta = -1f, bool sdraiato = false,
                      float postoX = float.NaN, float postoZ = float.NaN)
    {
        float S = CostruisciCasa.Scala;
        xMin *= S; 
        zMin *= S; 
        xMax *= S; 
        zMax *= S; 
        yMin *= S; 
        yMax *= S;

        if (seduta >= 0f) seduta *= S;
        if (!float.IsNaN(postoX)) postoX *= S;
        if (!float.IsNaN(postoZ)) postoZ *= S;

        float w = xMax - xMin, h = yMax - yMin, d = zMax - zMin;
 
        // Contenitore del mobile: NON scalato, così il modello dentro non si deforma.
        var radice = new GameObject(nome);
        radice.transform.SetParent(stanza, false);
        radice.transform.localPosition = new Vector3((xMin + xMax) / 2f, yMin, (zMin + zMax) / 2f);
        var collider = radice.AddComponent<BoxCollider>();
 
        string percorso = string.IsNullOrEmpty(modello) ? null : TrovaModello(modello);
 
        if (percorso != null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(percorso);
            Bounds b = MettiModello(radice.transform, prefab, percorso, w, h, d, rotazione, adatta, specchia, cambia, con);
            collider.center = radice.transform.InverseTransformPoint(b.center);
            collider.size = b.size;
        }
        else
        {
            if (!string.IsNullOrEmpty(modello))
                Debug.Log($"{nome}: nessun modello '{modello}' trovato, uso il blocco.");
 
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "Segnaposto";
            Object.DestroyImmediate(cubo.GetComponent<Collider>()); // il collider sta sulla radice
            cubo.transform.SetParent(radice.transform, false);
            cubo.transform.localPosition = new Vector3(0f, h / 2f, 0f);
            cubo.transform.localScale = new Vector3(w, h, d);
            cubo.GetComponent<Renderer>().sharedMaterial = matBlocco;
 
            collider.center = new Vector3(0f, h / 2f, 0f);
            collider.size = new Vector3(w, h, d);
        }
 
        bool siSiede = seduta >= 0f;
 
        // Dove ci si siede bisogna poter cliccare: serve anche Interagibile.
        if ((interagibile || siSiede) && tipoInteragibile != null)
        {
            var c = radice.AddComponent(tipoInteragibile);
            var so = new SerializedObject(c);
            var campoNome = so.FindProperty("nome");
            if (campoNome != null)
            {
                campoNome.stringValue = nome;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
 
        if (siSiede && tipoSeduta != null)
        {
            float cx = (xMin + xMax) / 2f, cz = (zMin + zMax) / 2f;
            float px = float.IsNaN(postoX) ? cx : postoX;
            float pz = float.IsNaN(postoZ) ? cz : postoZ;
 
            var c = radice.AddComponent(tipoSeduta);
            var so = new SerializedObject(c);
            so.FindProperty("puntoLocale").vector3Value = new Vector3(px - cx, seduta - yMin, pz - cz);
            so.FindProperty("rotazione").floatValue = rotazione;
            so.FindProperty("sdraiato").boolValue = sdraiato;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
 
    // Reflection Probe: cattura la stanza quando parte il gioco (non vede cose che si muovono dopo).
    static void SondaRiflessi(string nome, Vector3 centro, Vector3 misure)
    {
        centro *= CostruisciCasa.Scala;
        misure *= CostruisciCasa.Scala;
        var go = new GameObject(nome);
        go.transform.SetParent(stanza, false);
        go.transform.localPosition = centro;
        var sonda = go.AddComponent<ReflectionProbe>();
        sonda.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        sonda.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
        sonda.size = misure;
        sonda.boxProjection = true;
    }
 
    // Mette il modello dentro la radice: lo gira, lo adatta, lo posiziona.
    // Restituisce il suo ingombro finale (serve per il collider).
    static Bounds MettiModello(Transform radice, GameObject prefab, string percorso,
                               float w, float h, float d, float rotazione,
                               Adatta adatta, bool specchia, string cambia, Material con)
    {
        bool kaykit = percorso.Contains("KayKit");
        float baseRot = kaykit ? RotazioneKayKit : (percorso.Contains("Kenney") ? RotazioneKenney : 0f);
        string nomeFile = System.IO.Path.GetFileNameWithoutExtension(percorso);
        if (CorrezioniModelli.TryGetValue(nomeFile, out float correzione)) baseRot += correzione;
        float angolo = rotazione + baseRot;
 
        var contenitore = new GameObject("Modello").transform;
        contenitore.SetParent(radice, false);
        contenitore.localRotation = Quaternion.Euler(0f, angolo, 0f);
 
        var istanza = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contenitore);
        SistemaMateriali(istanza, kaykit, cambia, con);
 
        // 1. Misuro il modello così com'è (già girato)
        Bounds b = Ingombro(istanza);
        if (b.size.x < 0.001f || b.size.y < 0.001f || b.size.z < 0.001f) return b;
 
        // 2. Calcolo la scala (nelle direzioni del MONDO)
        Vector3 scalaMondo;
        if (adatta == Adatta.Riempi)
        {
            scalaMondo = new Vector3(w / b.size.x, h / b.size.y, d / b.size.z);
        }
        else
        {
            float s = Mathf.Min(w / b.size.x, d / b.size.z);
            scalaMondo = new Vector3(s, s, s);
        }
        // Se il contenitore è girato di 90°/270°, la sua X locale corrisponde alla Z del mondo.
        bool scambia = Mathf.Abs(Mathf.Repeat(angolo, 180f) - 90f) < 1f;
        Vector3 scala = scambia ? new Vector3(scalaMondo.z, scalaMondo.y, scalaMondo.x) : scalaMondo;
        if (specchia) scala.x = -scala.x;
        contenitore.localScale = scala;
 
        // 3. Posizione: centrato sullo spazio, appoggiato a terra.
        //    Con Proporzioni lo spingo con la schiena contro il lato "dietro" (di solito il muro).
        b = Ingombro(istanza);
        Vector3 p = radice.position;
        float xMin = p.x - w / 2f, xMax = p.x + w / 2f, zMin = p.z - d / 2f, zMax = p.z + d / 2f;
        float dx = p.x - b.center.x, dz = p.z - b.center.z;
        if (adatta == Adatta.Proporzioni)
        {
            float r = Mathf.Repeat(rotazione, 360f);
            if      (Mathf.Abs(r - 0f)   < 1f) dz = zMin - b.min.z;  // guarda a nord → schiena a sud
            else if (Mathf.Abs(r - 180f) < 1f) dz = zMax - b.max.z;  // guarda a sud  → schiena a nord
            else if (Mathf.Abs(r - 90f)  < 1f) dx = xMin - b.min.x;  // guarda a est  → schiena a ovest
            else if (Mathf.Abs(r - 270f) < 1f) dx = xMax - b.max.x;  // guarda a ovest → schiena a est
        }
        contenitore.position += new Vector3(dx, p.y - b.min.y, dz);
 
        return Ingombro(istanza);
    }
 
    // KayKit: un solo materiale con la texture a sfumature.
    // Kenney (e altri pacchetti vecchi): materiali con lo shader "Standard", che in URP
    // appaiono rosa. Li converto a URP Lit tenendo il colore.
    // Se 'cambia' è indicato, il materiale con quel nome viene sostituito da 'con' (solo in questo mobile).
    static void SistemaMateriali(GameObject istanza, bool kaykit, string cambia, Material con)
    {
        foreach (var r in istanza.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (kaykit && matKayKit != null) { mats[i] = matKayKit; continue; }
 
                if (con != null && mats[i] != null && mats[i].name == cambia) { mats[i] = con; continue; }
 
                var m = mats[i];
                if (m == null || shaderURP == null || m.shader == null) continue;
                if (m.shader.name != "Standard") continue;
                if (!AssetDatabase.IsOpenForEdit(m) || !AssetDatabase.Contains(m)) continue;
 
                Color colore = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                m.shader = shaderURP;
                m.SetColor("_BaseColor", colore);
                EditorUtility.SetDirty(m);
            }
            r.sharedMaterials = mats;
        }
    }
 
    static Bounds Ingombro(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
 
    // Cerca in Assets un modello: prima con il nome ESATTO, poi il nome più corto che INIZIA così.
    // Restituisce il percorso del file, o null.
    static string TrovaModello(string chiavi)
    {
        var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
 
        foreach (var chiave in chiavi.Split('|'))
        {
            string migliore = null;
            int lunghezzaMigliore = int.MaxValue;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(file, chiave, System.StringComparison.OrdinalIgnoreCase)) return path;
                if (!file.StartsWith(chiave, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (file.Length < lunghezzaMigliore) { migliore = path; lunghezzaMigliore = file.Length; }
            }
            if (migliore != null) return migliore;
        }
        return null;
    }
 
    static Material TrovaMateriale(string nome)
    {
        foreach (var guid in AssetDatabase.FindAssets(nome + " t:Material"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == nome)
                return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        return null;
    }
 
    // Materiali dei blocchi: creati una volta, poi riusati.
    static Material Mat(string nome, Color colore)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(CartellaMateriali))
            AssetDatabase.CreateFolder("Assets/Materials", "Arredi");
 
        string path = $"{CartellaMateriali}/{nome}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = shaderURP != null ? shaderURP : Shader.Find("Standard");
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
}