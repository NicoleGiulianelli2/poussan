using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
 
// =====================================================================================
//  COSTRUISCI PRIMI PIANI  —  script da EDITOR (va in Assets/Editor)
//  Menu:  CasaPulcino → Aggiungi primi piani
//  (Viene lanciato anche in automatico alla fine di "Costruisci arredi", se hai aggiunto
//   la riga  CostruisciPrimiPiani.Aggiungi();  in quello script.)
//
//  Cerca alcuni mobili dentro "Arredi" e a ciascuno aggiunge:
//    - lo script PrimoPiano (dove si mette il pulcino, dove guarda la telecamera)
//    - un "Contenuto" che compare solo nel primo piano: cibo nel frigo, pentola sul fornello...
//
//  OGNI PRIMO PIANO è una riga Prepara(...):
//    "nome"        nome del mobile, uguale a quello in CostruisciArredi
//    davanti       da che parte del mobile si mette il pulcino: 0 nord, 90 est, 180 sud, 270 ovest
//    contenuto     cosa compare (una delle funzioni più sotto: Frigo, Fornello, ...)
//    dietro        quanto sta la telecamera dietro al pulcino (m)
//    altezza       a che altezza da terra sta la telecamera (m)
//    guardaY       quale punto inquadra: 0 = base del mobile, 0.5 = metà, 1 = piano di sopra
//    nascondi      true = nasconde il modello e mostra l'interno (frigo aperto, armadio aperto...)
//    distanza      quanto sta il pulcino davanti al mobile (m)
//    spostaX       sposta il pulcino di lato (m sulla piantina), se davanti c'è qualcosa
// =====================================================================================
public static class CostruisciPrimiPiani
{
    const string Cartella = "Assets/Materials/PrimiPiani";
    delegate void Riempi(Transform c, float w, float h, float d);
 
    static System.Type tipoPrimoPiano, tipoVapore;
    static Material mBianco, mBiancoLucido, mVetro, mVapore, mArancio, mVerde, mVerdeScuro, mBlu,
                    mRosa, mGrigio, mCrema, mGiallo, mRosso, mFiamma, mPentola, mZuppa, mNero,
                    mCarta, mLegno;
    static Material[] mColori;
    static Material mLasagna, mCrosta, mFornoAcceso;
 
    [MenuItem("CasaPulcino/Aggiungi primi piani")]
    public static void Aggiungi()
    {
        tipoPrimoPiano = System.Type.GetType("PrimoPiano, Assembly-CSharp");
        tipoVapore = System.Type.GetType("Vapore, Assembly-CSharp");
        if (tipoPrimoPiano == null)
        {
            Debug.LogWarning("Script 'PrimoPiano' non trovato in Assets/Scripts: primi piani non aggiunti.");
            return;
        }
        CreaMateriali();
 
        //       nome mobile        davanti  contenuto  dietro  altezza  guardaY  nascondi
        Prepara("Frigo",              0f,   Frigo,     1.0f,   1.35f,   0.5f,    true);
        Prepara("Fornello",           0f,   Fornello,  0.55f,  1.65f,   1.0f,    false);
        Prepara("Scrivania",        180f,   Scrivania, 0.7f,   1.6f,    1.0f,    false, spostaX: 0.58f);
        Prepara("Tavolo cucina",    180f,   Tavola,    0.7f,   1.6f,    1.0f,    false);
        Prepara("Tavolo salotto",   270f,   Tavola,    0.15f,  1.9f,    1.0f,    false);
        Prepara("Libreria",           0f,   Libreria,  1.1f,   1.3f,    0.5f,    true);
        Prepara("Armadio",          270f,   Armadio,   1.2f,   1.5f,    0.5f,    true, distanza: 0.35f);
        Prepara("Friggitrice ad aria", 0f,  Niente,    0.5f,   1.75f,   0.5f,    false, distanza: 0.7f);
        CollegaCassettoFriggitrice();
        Prepara("Bollitore",          0f,   Niente,    0.5f,   1.75f,   0.5f,    false, distanza: 0.7f);
        CollegaBollitore();
        AggiungiForno();
 
        AssetDatabase.SaveAssets();
        var arredi = GameObject.Find("Arredi");
        if (arredi != null) EditorSceneManager.MarkSceneDirty(arredi.scene);
        Debug.Log("Primi piani aggiunti.");
    }
 
    // ------------------------------------------------------------------
    // Prepara un mobile
    // ------------------------------------------------------------------
    static void Prepara(string nome, float davanti, Riempi riempi, float dietro, float altezza,
                        float guardaY, bool nascondi, float distanza = 0.4f, float spostaX = 0f)
    {
        var radice = TrovaMobile(nome);
        if (radice == null) { Debug.Log($"Primo piano: mobile '{nome}' non trovato in Arredi."); return; }
        var col = radice.GetComponent<BoxCollider>();
        if (col == null) return;
 
        // Tolgo quello che avevo messo l'ultima volta
        foreach (var n in new[] { "Contenuto", "Posto", "Guarda" })
        {
            var vecchio = radice.Find(n);
            if (vecchio != null) Object.DestroyImmediate(vecchio.gameObject);
        }
        var vecchioComp = radice.GetComponent(tipoPrimoPiano);
        if (vecchioComp != null) Object.DestroyImmediate(vecchioComp);
 
        // Misure del mobile (la radice non è né ruotata né scalata, quindi il collider è in metri veri)
        Vector3 centro = radice.TransformPoint(col.center);
        Vector3 misure = col.size;
        Vector3 dir = Quaternion.Euler(0f, davanti, 0f) * Vector3.forward;   // dal mobile verso il pulcino
        bool lungoX = Mathf.Abs(dir.x) > 0.5f;
        float w = lungoX ? misure.z : misure.x;   // larghezza vista da davanti
        float d = lungoX ? misure.x : misure.z;   // profondità
        float h = misure.y;
        Vector3 basso = new Vector3(centro.x, centro.y - h / 2f, centro.z);
 
        // Contenuto: origine al centro della base, +Z = davanti, +X = destra (vista da davanti... del mobile)
        var cont = new GameObject("Contenuto").transform;
        cont.SetParent(radice, false);
        cont.position = basso;
        cont.rotation = Quaternion.LookRotation(dir);
        riempi(cont, w, h, d);
        cont.gameObject.SetActive(false);
 
        var posto = new GameObject("Posto").transform;
        posto.SetParent(radice, false);
        posto.position = new Vector3(basso.x + spostaX * CostruisciCasa.Scala, 0f, basso.z) + dir * (d / 2f + distanza);
        posto.rotation = Quaternion.LookRotation(-dir);
 
        var guarda = new GameObject("Guarda").transform;
        guarda.SetParent(radice, false);
        guarda.position = basso + Vector3.up * (h * guardaY);
 
        var comp = radice.gameObject.AddComponent(tipoPrimoPiano);
        var so = new SerializedObject(comp);
        so.FindProperty("posto").objectReferenceValue = posto;
        so.FindProperty("guarda").objectReferenceValue = guarda;
        so.FindProperty("contenuto").objectReferenceValue = cont.gameObject;
        var modello = radice.Find("Modello");
        if (modello == null) modello = radice.Find("Segnaposto");
        so.FindProperty("modello").objectReferenceValue = modello != null ? modello.gameObject : null;
        so.FindProperty("nascondiModello").boolValue = nascondi;
        so.FindProperty("dietro").floatValue = dietro;
        so.FindProperty("altezza").floatValue = altezza;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
 
    static Transform TrovaMobile(string nome)
    {
        var arredi = GameObject.Find("Arredi");
        if (arredi == null) return null;
        foreach (var t in arredi.GetComponentsInChildren<Transform>(true))
            if (t.name == nome && t.GetComponent<BoxCollider>() != null) return t;
        return null;
    }
 
    // =====================================================================
    // CONTENUTI. Coordinate locali: X = larghezza (da -w/2 a w/2), Y = altezza (da 0 a h),
    // Z = profondità (da -d/2 dietro a d/2 davanti, verso il pulcino). Misure in metri.
    // =====================================================================
 
    // FRIGO APERTO: pareti, ripiani di vetro, cibo, anta aperta, lucina
    static void Frigo(Transform c, float w, float h, float d)
    {
        float t = 0.04f;
        Scatola(c, w, h, d, t, mBianco);
        float basso = 0.14f;
        Box(c, "Base", new Vector3(0f, basso / 2f, 0f), new Vector3(w, basso, d), mBianco);
 
        float[] r = { h * 0.32f, h * 0.52f, h * 0.72f };
        foreach (var y in r) Box(c, "Ripiano", new Vector3(0f, y, 0.01f), new Vector3(w - 2 * t, 0.015f, d - t - 0.03f), mVetro);
 
        // Cassetto delle verdure
        Box(c, "Cassetto", new Vector3(0f, basso + 0.08f, 0.02f), new Vector3(w - 2 * t - 0.04f, 0.16f, d * 0.7f), mVetro);
        Cil(c, "Carota 1", new Vector3(-w * 0.22f, basso + 0.04f, 0.02f), new Vector3(0.035f, 0.08f, 0.035f), mArancio, new Vector3(0f, 20f, 90f));
        Cil(c, "Carota 2", new Vector3(-w * 0.15f, basso + 0.04f, -0.03f), new Vector3(0.035f, 0.08f, 0.035f), mArancio, new Vector3(0f, -15f, 90f));
        Sfera(c, "Insalata", new Vector3(w * 0.15f, basso + 0.07f, 0f), new Vector3(0.14f, 0.11f, 0.14f), mVerde);
 
        // Ripiano 1: latte, succo, yogurt
        float y1 = r[0] + 0.008f;
        Box(c, "Latte", new Vector3(-w * 0.25f, y1 + 0.11f, 0f), new Vector3(0.08f, 0.22f, 0.08f), mBiancoLucido);
        Box(c, "Tappo latte", new Vector3(-w * 0.25f, y1 + 0.235f, 0f), new Vector3(0.084f, 0.03f, 0.084f), mBlu);
        Box(c, "Succo", new Vector3(-w * 0.08f, y1 + 0.1f, 0.01f), new Vector3(0.08f, 0.2f, 0.08f), mArancio);
        Cil(c, "Yogurt 1", new Vector3(w * 0.14f, y1 + 0.04f, 0.05f), new Vector3(0.07f, 0.04f, 0.07f), mRosa);
        Cil(c, "Yogurt 2", new Vector3(w * 0.24f, y1 + 0.04f, 0.05f), new Vector3(0.07f, 0.04f, 0.07f), mRosa);
 
        // Ripiano 2: uova e formaggio
        float y2 = r[1] + 0.008f;
        Box(c, "Portauova", new Vector3(-w * 0.15f, y2 + 0.015f, 0.02f), new Vector3(0.24f, 0.03f, 0.13f), mGrigio);
        for (int i = 0; i < 6; i++)
            Sfera(c, "Uovo", new Vector3(-w * 0.15f - 0.075f + (i % 3) * 0.075f, y2 + 0.055f, 0.02f - 0.03f + (i / 3) * 0.06f),
                  new Vector3(0.045f, 0.06f, 0.045f), mCrema);
        Box(c, "Formaggio", new Vector3(w * 0.2f, y2 + 0.05f, 0.03f), new Vector3(0.14f, 0.1f, 0.1f), mGiallo);
 
        // Ripiano 3: mele e bottiglia
        float y3 = r[2] + 0.008f;
        for (int i = 0; i < 3; i++)
            Sfera(c, "Mela", new Vector3(-w * 0.25f + i * 0.09f, y3 + 0.04f, 0.04f), Vector3.one * 0.08f, mRosso);
        Cil(c, "Bottiglia", new Vector3(w * 0.22f, y3 + 0.14f, -0.02f), new Vector3(0.07f, 0.14f, 0.07f), mVerdeScuro);
        Cil(c, "Collo bottiglia", new Vector3(w * 0.22f, y3 + 0.31f, -0.02f), new Vector3(0.03f, 0.04f, 0.03f), mVerdeScuro);
 
        // Anta aperta: cardine sul lato destro davanti
        var cardine = new GameObject("Anta frigo").transform;
        cardine.SetParent(c, false);
        cardine.localPosition = new Vector3(w / 2f, 0f, d / 2f);
        cardine.localRotation = Quaternion.Euler(0f, 110f, 0f);
        Box(cardine, "Anta", new Vector3(-w / 2f, h / 2f, 0.03f), new Vector3(w, h, 0.06f), mBianco);
        Box(cardine, "Ripiano anta", new Vector3(-w / 2f, h * 0.4f, -0.04f), new Vector3(w * 0.8f, 0.02f, 0.08f), mVetro);
        Cil(cardine, "Ketchup", new Vector3(-w * 0.35f, h * 0.4f + 0.09f, -0.04f), new Vector3(0.05f, 0.08f, 0.05f), mRosso);
        Cil(cardine, "Acqua", new Vector3(-w * 0.6f, h * 0.4f + 0.13f, -0.04f), new Vector3(0.06f, 0.12f, 0.06f), mVetro);
 
        // Lucina del frigo
        var luce = new GameObject("Luce frigo");
        luce.transform.SetParent(c, false);
        luce.transform.localPosition = new Vector3(0f, h * 0.85f, 0f);
        var l = luce.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 1.5f;
        l.intensity = 1.2f;
        l.color = new Color(1f, 0.97f, 0.9f);
    }
 
    // FORNELLO: pentola che bolle con vapore + padella con uovo
    static void Fornello(Transform c, float w, float h, float d)
    {
        // Pentola sul fuoco dietro a sinistra
        Vector3 p = new Vector3(-w * 0.22f, h, -d * 0.12f);
        Cil(c, "Fiamma", p + new Vector3(0f, 0.006f, 0f), new Vector3(0.2f, 0.004f, 0.2f), mFiamma);
        Cil(c, "Pentola", p + new Vector3(0f, 0.095f, 0f), new Vector3(0.26f, 0.075f, 0.26f), mPentola);
        Box(c, "Manico sx", p + new Vector3(-0.15f, 0.14f, 0f), new Vector3(0.05f, 0.02f, 0.05f), mNero);
        Box(c, "Manico dx", p + new Vector3(0.15f, 0.14f, 0f), new Vector3(0.05f, 0.02f, 0.05f), mNero);
        Cil(c, "Zuppa", p + new Vector3(0f, 0.16f, 0f), new Vector3(0.24f, 0.004f, 0.24f), mZuppa);
        Cil(c, "Coperchio", p + new Vector3(0.06f, 0.2f, 0f), new Vector3(0.27f, 0.006f, 0.27f), mPentola, new Vector3(0f, 0f, 14f));
        Sfera(c, "Pomello coperchio", p + new Vector3(0.055f, 0.225f, 0f), Vector3.one * 0.035f, mNero);
 
        // Vapore: palline che salgono (le muove lo script Vapore)
        var v = new GameObject("Vapore").transform;
        v.SetParent(c, false);
        v.localPosition = p + new Vector3(-0.06f, 0.18f, 0f);
        for (int i = 0; i < 8; i++) Sfera(v, "Sbuffo", Vector3.zero, Vector3.one * 0.05f, mVapore);
        if (tipoVapore != null) v.gameObject.AddComponent(tipoVapore);
 
        // Padella con uovo sul fuoco davanti a destra
        Vector3 q = new Vector3(w * 0.22f, h, d * 0.15f);
        Cil(c, "Fiamma 2", q + new Vector3(0f, 0.006f, 0f), new Vector3(0.18f, 0.004f, 0.18f), mFiamma);
        Cil(c, "Padella", q + new Vector3(0f, 0.03f, 0f), new Vector3(0.24f, 0.015f, 0.24f), mPentola);
        Box(c, "Manico padella", q + new Vector3(0.2f, 0.04f, 0.05f), new Vector3(0.18f, 0.02f, 0.03f), mNero, new Vector3(0f, -15f, 0f));
        Cil(c, "Albume", q + new Vector3(0f, 0.048f, 0f), new Vector3(0.12f, 0.004f, 0.1f), mBiancoLucido);
        Sfera(c, "Tuorlo", q + new Vector3(0f, 0.055f, 0f), new Vector3(0.045f, 0.025f, 0.045f), mGiallo);
    }
 
    // SCRIVANIA: foglio, penna e busta per la lettera
    static void Scrivania(Transform c, float w, float h, float d)
    {
        Box(c, "Foglio", new Vector3(-0.05f, h + 0.002f, 0.02f), new Vector3(0.21f, 0.003f, 0.297f), mCarta, new Vector3(0f, 8f, 0f));
        Cil(c, "Penna", new Vector3(0.14f, h + 0.008f, 0.03f), new Vector3(0.012f, 0.075f, 0.012f), mBlu, new Vector3(90f, 25f, 0f));
        Box(c, "Busta", new Vector3(-0.32f, h + 0.003f, -0.05f), new Vector3(0.22f, 0.005f, 0.11f), mCrema, new Vector3(0f, -10f, 0f));
    }
 
    // TAVOLO: piatto, forchetta, bicchiere
    static void Tavola(Transform c, float w, float h, float d)
    {
        float z = d * 0.15f;
        Cil(c, "Piatto", new Vector3(0f, h + 0.006f, z), new Vector3(0.26f, 0.006f, 0.26f), mBiancoLucido);
        Box(c, "Forchetta", new Vector3(-0.17f, h + 0.004f, z), new Vector3(0.02f, 0.005f, 0.18f), mGrigio);
        Cil(c, "Bicchiere", new Vector3(0.22f, h + 0.06f, z - 0.05f), new Vector3(0.07f, 0.06f, 0.07f), mVetro);
    }
 
    // LIBRERIA APERTA: ripiani pieni di libri colorati
    static void Libreria(Transform c, float w, float h, float d)
    {
        float t = 0.03f;
        Scatola(c, w, h, d, t, mLegno);
        int n = 5;
        float passo = (h - 0.1f) / n;
        var caso = new System.Random(7);   // sempre gli stessi "a caso", così non cambia ogni volta
        for (int k = 0; k < n; k++)
        {
            float y = 0.05f + k * passo;
            Box(c, "Ripiano", new Vector3(0f, y, 0f), new Vector3(w - 2 * t, 0.025f, d - t), mLegno);
            float x = -w / 2f + t + 0.01f;
            while (x < w / 2f - t - 0.05f)
            {
                float bw = 0.025f + (float)caso.NextDouble() * 0.03f;
                float bh = (passo - 0.05f) * (0.65f + (float)caso.NextDouble() * 0.3f);
                var mat = mColori[caso.Next(mColori.Length)];
                Box(c, "Libro", new Vector3(x + bw / 2f, y + 0.0125f + bh / 2f, 0f), new Vector3(bw, bh, d * 0.7f), mat);
                x += bw + 0.004f;
                if (caso.NextDouble() < 0.08) x += 0.05f;   // ogni tanto un buco
            }
        }
    }
 
    // ARMADIO APERTO: asta con vestiti appesi e ante aperte
    static void Armadio(Transform c, float w, float h, float d)
    {
        float t = 0.03f;
        Scatola(c, w, h, d, t, mLegno);
        Box(c, "Base", new Vector3(0f, 0.04f, 0f), new Vector3(w, 0.08f, d), mLegno);
        Box(c, "Ripiano alto", new Vector3(0f, h * 0.88f, 0f), new Vector3(w - 2 * t, 0.025f, d - t), mLegno);
 
        float yAsta = h * 0.8f;
        Cil(c, "Asta", new Vector3(0f, yAsta, 0f), new Vector3(0.025f, w / 2f - t, 0.025f), mGrigio, new Vector3(0f, 0f, 90f));
        var caso = new System.Random(3);
        int n = Mathf.Max(2, Mathf.FloorToInt((w - 0.3f) / 0.12f));
        for (int i = 0; i < n; i++)
        {
            float x = -w / 2f + 0.15f + i * (w - 0.3f) / (n - 1);
            float lung = 0.6f + (float)caso.NextDouble() * 0.45f;
            var mat = mColori[caso.Next(mColori.Length)];
            Box(c, "Vestito", new Vector3(x, yAsta - 0.04f - lung / 2f, 0f), new Vector3(0.05f, lung, d * 0.7f), mat);
            Box(c, "Gruccia", new Vector3(x, yAsta - 0.02f, 0f), new Vector3(0.02f, 0.04f, d * 0.5f), mGrigio);
        }
        // Maglioni piegati sul ripiano alto
        for (int i = 0; i < 4; i++)
            Box(c, "Maglione", new Vector3(-w * 0.3f + i * w * 0.2f, h * 0.88f + 0.06f, 0f),
                new Vector3(w * 0.15f, 0.1f, d * 0.6f), mColori[(i * 3) % mColori.Length]);
 
        // Due ante aperte
        var sx = new GameObject("Anta sinistra").transform;
        sx.SetParent(c, false);
        sx.localPosition = new Vector3(-w / 2f, 0f, d / 2f);
        sx.localRotation = Quaternion.Euler(0f, -100f, 0f);
        Box(sx, "Pannello", new Vector3(w / 4f, h / 2f, 0.02f), new Vector3(w / 2f, h, 0.04f), mLegno);
        var dx = new GameObject("Anta destra").transform;
        dx.SetParent(c, false);
        dx.localPosition = new Vector3(w / 2f, 0f, d / 2f);
        dx.localRotation = Quaternion.Euler(0f, 100f, 0f);
        Box(dx, "Pannello", new Vector3(-w / 4f, h / 2f, 0.02f), new Vector3(w / 2f, h, 0.04f), mLegno);
    }
 
    // BOLLITORE: attacca KettleSteamInteraction al modello Kettle e gli collega il pulcino
    static void CollegaBollitore()
    {
        var tipo = System.Type.GetType("KettleSteamInteraction, Assembly-CSharp");
        var radice = TrovaMobile("Bollitore");
        if (tipo == null || radice == null) return;
 
        Transform modello = null;
        foreach (var t in radice.GetComponentsInChildren<Transform>(true))
            if (t.Find("Kettle_Body") != null) modello = t;
        if (modello == null)
        {
            Debug.Log("Bollitore: modello Kettle non trovato (è ancora un blocco?).");
            return;
        }
 
        var comp = modello.GetComponent(tipo);
        if (comp == null) comp = modello.gameObject.AddComponent(tipo);
        var so = new SerializedObject(comp);
        var pulcino = GameObject.Find("Pulcino");
        if (pulcino != null) so.FindProperty("player").objectReferenceValue = pulcino.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
 
    // FORNO: la parte sotto ai fuochi del Fornello. Luce arancione durante la cottura,
    // lasagne fumanti che compaiono sul tavolo della cucina quando sono pronte.
    static void AggiungiForno()
    {
        var tipo = System.Type.GetType("Forno, Assembly-CSharp");
        var fornello = TrovaMobile("Fornello");
        if (tipo == null || fornello == null) return;
 
        var vecchia = fornello.Find("Luce forno");
        if (vecchia != null) Object.DestroyImmediate(vecchia.gameObject);
 
        // Il forno guarda a nord (+Z): lo sportello è sul lato "fronte" del fornello
        var col = fornello.GetComponent<BoxCollider>();
        Vector3 c = fornello.TransformPoint(col.center);
        Vector3 s = col.size;
        var lucina = new GameObject("Luce forno").transform;
        lucina.SetParent(fornello, false);
        lucina.position = new Vector3(c.x, c.y - s.y / 2f, c.z + s.z / 2f);
        Box(lucina, "Vetro forno acceso", new Vector3(0f, s.y * 0.38f, 0.008f),
            new Vector3(s.x * 0.55f, s.y * 0.25f, 0.012f), mFornoAcceso);
        var lgo = new GameObject("Luce");
        lgo.transform.SetParent(lucina, false);
        lgo.transform.localPosition = new Vector3(0f, s.y * 0.38f, 0.25f);
        var l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 1.2f;
        l.intensity = 1.4f;
        l.color = new Color(1f, 0.55f, 0.2f);
        lucina.gameObject.SetActive(false);
 
        // Lasagne sul tavolo della cucina, verso il muro (il piatto del primo piano sta dall'altra parte)
        GameObject lasagne = null;
        var tavolo = TrovaMobile("Tavolo cucina");
        if (tavolo != null)
        {
            var v = tavolo.Find("Lasagne");
            if (v != null) Object.DestroyImmediate(v.gameObject);
            var tc = tavolo.GetComponent<BoxCollider>();
            Vector3 tcen = tavolo.TransformPoint(tc.center);
            Vector3 ts = tc.size;
 
            var g = new GameObject("Lasagne").transform;
            g.SetParent(tavolo, false);
            g.position = new Vector3(tcen.x, tcen.y + ts.y / 2f, tcen.z + ts.z * 0.15f);
            Box(g, "Teglia", new Vector3(0f, 0.03f, 0f), new Vector3(0.34f, 0.06f, 0.26f), mPentola);
            Box(g, "Lasagna", new Vector3(0f, 0.058f, 0f), new Vector3(0.31f, 0.025f, 0.23f), mLasagna);
            var caso = new System.Random(11);
            for (int i = 0; i < 7; i++)
                Sfera(g, "Sugo", new Vector3(((float)caso.NextDouble() - 0.5f) * 0.24f, 0.071f, ((float)caso.NextDouble() - 0.5f) * 0.17f),
                      new Vector3(0.05f, 0.008f, 0.04f), mZuppa);
            for (int i = 0; i < 6; i++)
                Sfera(g, "Crosticina", new Vector3(((float)caso.NextDouble() - 0.5f) * 0.26f, 0.071f, ((float)caso.NextDouble() - 0.5f) * 0.19f),
                      new Vector3(0.035f, 0.006f, 0.03f), mCrosta);
            var vap = new GameObject("Vapore").transform;
            vap.SetParent(g, false);
            vap.localPosition = new Vector3(0f, 0.07f, 0f);
            for (int i = 0; i < 8; i++) Sfera(vap, "Sbuffo", Vector3.zero, Vector3.one * 0.05f, mVapore);
            if (tipoVapore != null) vap.gameObject.AddComponent(tipoVapore);
            g.gameObject.SetActive(false);
            lasagne = g.gameObject;
        }
 
        var comp = fornello.GetComponent(tipo);
        if (comp == null) comp = fornello.gameObject.AddComponent(tipo);
        var so = new SerializedObject(comp);
        so.FindProperty("lucina").objectReferenceValue = lucina.gameObject;
        so.FindProperty("lasagne").objectReferenceValue = lasagne;
        var pulcino = GameObject.Find("Pulcino");
        if (pulcino != null) so.FindProperty("pulcino").objectReferenceValue = pulcino.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
 
    // FRIGGITRICE: non aggiungo niente, il cassetto si apre da solo (AirFryerDrawerController)
    static void Niente(Transform c, float w, float h, float d) { }
 
    // Attacca AirFryerDrawerController al modello della friggitrice e gli collega cassetto e pulcino
    static void CollegaCassettoFriggitrice()
    {
        var tipo = System.Type.GetType("AirFryerDrawerController, Assembly-CSharp");
        var radice = TrovaMobile("Friggitrice ad aria");
        if (tipo == null || radice == null) return;
 
        Transform cassetto = null, modelloFriggitrice = null;
        foreach (var t in radice.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "AirFryer_Drawer") cassetto = t;
            if (t.Find("AirFryer_Drawer") != null) modelloFriggitrice = t;
        }
        if (cassetto == null || modelloFriggitrice == null)
        {
            Debug.Log("Friggitrice: modello AirFryer non trovato (è ancora un blocco?).");
            return;
        }
 
        var comp = modelloFriggitrice.GetComponent(tipo);
        if (comp == null) comp = modelloFriggitrice.gameObject.AddComponent(tipo);
        var so = new SerializedObject(comp);
        so.FindProperty("drawer").objectReferenceValue = cassetto;
        var pulcino = GameObject.Find("Pulcino");
        if (pulcino != null) so.FindProperty("player").objectReferenceValue = pulcino.transform;
        // Nel file di Blender il "davanti" è l'asse -Y del genitore: il cassetto esce da lì
        so.FindProperty("openOffset").vector3Value = new Vector3(0f, -1f, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();
 
        AggiungiPatatine(cassetto);
    }
 
    // Un mucchietto di patatine sopra il cestello, verso il davanti: escono insieme al cassetto
    static void AggiungiPatatine(Transform cassetto)
    {
        var cestello = cassetto.Find("AirFryer_Basket");
        var rend = cestello != null ? cestello.GetComponent<Renderer>() : null;
        if (rend == null) return;
        var vecchie = cestello.Find("Patatine");
        if (vecchie != null) Object.DestroyImmediate(vecchie.gameObject);
 
        var mat = Mat("Patatina", new Color(0.98f, 0.78f, 0.32f), liscio: 0.6f);
        Bounds b = rend.bounds;                                   // misure del cestello nel mondo
        Vector3 davanti = cassetto.parent.TransformDirection(Vector3.down);   // -Y di Blender = davanti
        davanti.y = 0f;
        davanti.Normalize();
        Vector3 lato = Vector3.Cross(Vector3.up, davanti);
        float larg = Mathf.Min(b.size.x, b.size.z);
        float k = larg / 0.26f;                                   // quanto è stata ingrandita la friggitrice
 
        var gruppo = new GameObject("Patatine").transform;
        gruppo.position = b.center;
        var caso = new System.Random(5);
        for (int i = 0; i < 30; i++)
        {
            float u = ((float)caso.NextDouble() - 0.5f) * larg * 0.7f;   // destra/sinistra
            float v = (float)caso.NextDouble() * larg * 0.4f;            // verso il davanti
            Vector3 pos = new Vector3(b.center.x, 0f, b.center.z) + lato * u + davanti * v;
            pos.y = b.max.y + 0.006f * k + (float)caso.NextDouble() * 0.035f * k;
 
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Patatina";
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.transform.SetParent(gruppo, true);
            p.transform.position = pos;
            p.transform.rotation = Quaternion.Euler((float)caso.NextDouble() * 30f - 15f,
                                                    (float)caso.NextDouble() * 360f,
                                                    (float)caso.NextDouble() * 30f - 15f);
            p.transform.localScale = new Vector3(0.012f, 0.012f, 0.07f + (float)caso.NextDouble() * 0.02f) * k;
            p.GetComponent<Renderer>().sharedMaterial = mat;
        }
        gruppo.SetParent(cestello, true);   // figlie del cestello: si muovono con il cassetto
    }
 
    // =====================================================================
    // Forme e materiali
    // =====================================================================
 
    // Una "scatola" aperta davanti: fondo, due fianchi, cielo
    static void Scatola(Transform c, float w, float h, float d, float t, Material mat)
    {
        Box(c, "Fondo",   new Vector3(0f, h / 2f, -d / 2f + t / 2f), new Vector3(w, h, t), mat);
        Box(c, "Fianco sx", new Vector3(-w / 2f + t / 2f, h / 2f, 0f), new Vector3(t, h, d), mat);
        Box(c, "Fianco dx", new Vector3(w / 2f - t / 2f, h / 2f, 0f), new Vector3(t, h, d), mat);
        Box(c, "Cielo",   new Vector3(0f, h - t / 2f, 0f), new Vector3(w, t, d), mat);
    }
 
    static GameObject Forma(PrimitiveType tipo, Transform parent, string nome, Vector3 pos, Vector3 scala,
                            Material mat, Vector3 rot)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nome;
        Object.DestroyImmediate(go.GetComponent<Collider>());   // solo da guardare
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        go.transform.localScale = scala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
 
    static GameObject Box(Transform p, string n, Vector3 pos, Vector3 s, Material m, Vector3 rot = default)
        => Forma(PrimitiveType.Cube, p, n, pos, s, m, rot);
    static GameObject Sfera(Transform p, string n, Vector3 pos, Vector3 s, Material m, Vector3 rot = default)
        => Forma(PrimitiveType.Sphere, p, n, pos, s, m, rot);
    // Cilindro: scala.y è MEZZA altezza (il cilindro di Unity è alto 2)
    static GameObject Cil(Transform p, string n, Vector3 pos, Vector3 s, Material m, Vector3 rot = default)
        => Forma(PrimitiveType.Cylinder, p, n, pos, s, m, rot);
 
    static void CreaMateriali()
    {
        mBianco       = Mat("Bianco frigo",   new Color(0.95f, 0.96f, 0.97f));
        mBiancoLucido = Mat("Bianco lucido",  new Color(1f, 1f, 1f), liscio: 0.8f);
        mArancio      = Mat("Arancione",      new Color(0.98f, 0.55f, 0.15f));
        mVerde        = Mat("Verde insalata", new Color(0.45f, 0.8f, 0.3f));
        mVerdeScuro   = Mat("Verde bottiglia",new Color(0.15f, 0.45f, 0.25f), liscio: 0.8f);
        mBlu          = Mat("Blu",            new Color(0.2f, 0.4f, 0.85f));
        mRosa         = Mat("Rosa",           new Color(0.98f, 0.7f, 0.8f));
        mGrigio       = Mat("Grigio",         new Color(0.7f, 0.7f, 0.72f), metallo: 0.5f);
        mCrema        = Mat("Crema",          new Color(0.98f, 0.92f, 0.8f));
        mGiallo       = Mat("Giallo",         new Color(1f, 0.82f, 0.2f));
        mRosso        = Mat("Rosso",          new Color(0.85f, 0.15f, 0.15f), liscio: 0.7f);
        mPentola      = Mat("Pentola",        new Color(0.35f, 0.36f, 0.4f), metallo: 0.8f, liscio: 0.7f);
        mZuppa        = Mat("Zuppa",          new Color(0.9f, 0.45f, 0.2f));
        mNero         = Mat("Nero",           new Color(0.1f, 0.1f, 0.1f));
        mCarta        = Mat("Carta",          new Color(0.99f, 0.99f, 0.97f));
        mLegno        = Mat("Legno chiaro",   new Color(0.82f, 0.65f, 0.45f));
        mFiamma       = Mat("Fiamma",         new Color(0.3f, 0.5f, 1f), emissione: new Color(0.3f, 0.55f, 1f) * 2f);
        mVetro        = MatTrasparente("Vetro primi piani", new Color(0.8f, 0.92f, 1f, 0.35f));
        mVapore       = MatTrasparente("Vapore",            new Color(1f, 1f, 1f, 0.55f));
        mLasagna      = Mat("Lasagna",        new Color(0.95f, 0.72f, 0.38f));
        mCrosta       = Mat("Crosticina",     new Color(0.55f, 0.3f, 0.12f));
        mFornoAcceso  = Mat("Forno acceso",   new Color(1f, 0.5f, 0.15f), emissione: new Color(1f, 0.45f, 0.1f) * 2.5f);
        mColori = new[]
        {
            Mat("Tinta rossa",  new Color(0.8f, 0.25f, 0.25f)),
            Mat("Tinta blu",    new Color(0.25f, 0.4f, 0.75f)),
            Mat("Tinta verde",  new Color(0.3f, 0.6f, 0.35f)),
            Mat("Tinta gialla", new Color(0.95f, 0.78f, 0.3f)),
            Mat("Tinta viola",  new Color(0.55f, 0.35f, 0.7f)),
            Mat("Tinta crema",  new Color(0.93f, 0.88f, 0.78f)),
            Mat("Tinta rosa",   new Color(0.95f, 0.6f, 0.7f)),
            Mat("Tinta azzurra",new Color(0.5f, 0.75f, 0.9f)),
        };
    }
 
    // Crea il materiale la prima volta; dopo lo riusa (i colori li cambi dall'Inspector).
    static Material Mat(string nome, Color colore, float metallo = 0f, float liscio = 0.4f, Color? emissione = null)
    {
        CreaCartella();
        string path = $"{Cartella}/{nome}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
 
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", colore);
        mat.SetFloat("_Metallic", metallo);
        mat.SetFloat("_Smoothness", liscio);
        if (emissione.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissione.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
 
    static Material MatTrasparente(string nome, Color colore)
    {
        CreaCartella();
        string path = $"{Cartella}/{nome}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
 
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", colore);
        mat.SetFloat("_Smoothness", 0.9f);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
 
    static void CreaCartella()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(Cartella))
            AssetDatabase.CreateFolder("Assets/Materials", "PrimiPiani");
    }
}
 
