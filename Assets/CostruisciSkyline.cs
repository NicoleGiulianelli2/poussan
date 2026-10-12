using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
 
// =====================================================================================
//  COSTRUISCI SKYLINE  —  script da EDITOR (va in Assets/Editor)
//  Menu:  CasaPulcino → Costruisci skyline
//
//  Mette intorno alla casa un grande cilindro con la tua foto panoramica stampata
//  sul lato interno: dalle finestre (e dall'alto) si vede il panorama.
//  La foto va messa in Assets/Textures e chiamata "Skyline" (es. Skyline.jpg).
//  La foto viene ripetuta 2 volte intorno (una dritta e una a specchio), così non si deforma.
// =====================================================================================
public static class CostruisciSkyline
{
    const string NomeFoto = "Skyline";
 
    const float Raggio = 18f;          // distanza del panorama dal centro della casa (m)
    const float Base = -4f;            // il bordo basso della foto sta 4 m sotto terra (nasconde la ringhiera)
    const int Ripetizioni = 2;         // quante volte la foto fa il giro (2 = dritta + specchio)
    const float AngoloCentro = 90f;    // dove sta il CENTRO della foto: 0 nord, 90 est (balcone), 180 sud, 270 ovest
    const int Segmenti = 96;           // più segmenti = cilindro più liscio
 
    [MenuItem("CasaPulcino/Costruisci skyline")]
    public static void Costruisci()
    {
        var foto = TrovaFoto();
        if (foto == null)
        {
            Debug.LogWarning($"Foto '{NomeFoto}' non trovata: mettila in Assets/Textures e chiamala {NomeFoto}.");
            return;
        }
        SistemaImportazione(foto);
 
        var vecchio = GameObject.Find("Skyline");
        if (vecchio != null) Undo.DestroyObjectImmediate(vecchio);
 
        // Centro della casa (dai pavimenti)
        Vector3 centro = Vector3.zero;
        var casa = GameObject.Find("Casa");
        if (casa != null)
        {
            var rs = casa.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                centro = new Vector3(b.center.x, 0f, b.center.z);
            }
        }
 
        // Altezza giusta perché la foto non si deformi:
        // ogni ripetizione copre (giro / Ripetizioni) di circonferenza
        float proporzione = (float)foto.width / foto.height;
        float larghezzaFoto = 2f * Mathf.PI * Raggio / Ripetizioni;
        float altezza = larghezzaFoto / proporzione;
 
        var go = new GameObject("Skyline");
        Undo.RegisterCreatedObjectUndo(go, "Costruisci skyline");
        go.transform.position = centro;
 
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreaCilindro(altezza);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Materiale(foto);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // non deve fare ombra sulla casa
        mr.receiveShadows = false;
 
        // Prato sotto la casa, fino al panorama
        var prato = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        prato.name = "Prato";
        Object.DestroyImmediate(prato.GetComponent<Collider>());
        prato.transform.SetParent(go.transform, false);
        prato.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        prato.transform.localScale = new Vector3(Raggio * 2f, 0.02f, Raggio * 2f);
        prato.GetComponent<Renderer>().sharedMaterial = MatPrato();
 
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log($"Skyline costruito: raggio {Raggio} m, alto {altezza:0.0} m.");
    }
 
    // Cilindro aperto (senza tappi) con le facce rivolte verso l'INTERNO
    static Mesh CreaCilindro(float altezza)
    {
        var v = new Vector3[(Segmenti + 1) * 2];
        var uv = new Vector2[v.Length];
        var tri = new int[Segmenti * 6];
 
        for (int i = 0; i <= Segmenti; i++)
        {
            float f = (float)i / Segmenti;                       // 0 → 1 lungo il giro
            // Il centro della foto (u = 0.5 della prima ripetizione) cade su AngoloCentro
            float gradi = AngoloCentro + (f - 0.5f / Ripetizioni) * 360f;
            float rad = gradi * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));   // 0° = nord (+Z), 90° = est (+X)
 
            v[i * 2]     = dir * Raggio + Vector3.up * Base;
            v[i * 2 + 1] = dir * Raggio + Vector3.up * (Base + altezza);
            // Guardando da dentro, girando in senso orario la foto va da sinistra a destra
            uv[i * 2]     = new Vector2(f * Ripetizioni, 0f);
            uv[i * 2 + 1] = new Vector2(f * Ripetizioni, 1f);
        }
        for (int i = 0; i < Segmenti; i++)
        {
            int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
            // ordine dei vertici scelto perché la faccia "davanti" guardi verso il centro
            tri[i * 6 + 0] = a; tri[i * 6 + 1] = b; tri[i * 6 + 2] = c;
            tri[i * 6 + 3] = c; tri[i * 6 + 4] = b; tri[i * 6 + 5] = d;
        }
 
        var mesh = new Mesh { name = "Cilindro skyline", vertices = v, uv = uv, triangles = tri };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // La salvo come file, altrimenti alla riapertura della scena sparirebbe
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = "Assets/Materials/Skyline_cilindro.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
 
    // Unlit = la foto si vede con i suoi colori, senza luci e ombre del gioco
    static Material Materiale(Texture2D foto)
    {
        string path = "Assets/Materials/Skyline.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", foto);
        else mat.mainTexture = foto;
        EditorUtility.SetDirty(mat);
        return mat;
    }
 
    static Material MatPrato()
    {
        string path = "Assets/Materials/Prato.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", new Color(0.36f, 0.5f, 0.3f));
        mat.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
 
    static Texture2D TrovaFoto()
    {
        foreach (var guid in AssetDatabase.FindAssets(NomeFoto + " t:Texture2D"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == NomeFoto)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }
 
    // Foto grande e "a specchio" ai bordi, così le ripetizioni si uniscono senza stacchi
    static void SistemaImportazione(Texture2D foto)
    {
        string p = AssetDatabase.GetAssetPath(foto);
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp == null) return;
        bool cambiato = false;
        if (imp.wrapModeU != TextureWrapMode.Mirror) { imp.wrapModeU = TextureWrapMode.Mirror; cambiato = true; }
        if (imp.wrapModeV != TextureWrapMode.Clamp) { imp.wrapModeV = TextureWrapMode.Clamp; cambiato = true; }
        if (imp.maxTextureSize < 4096) { imp.maxTextureSize = 4096; cambiato = true; }
        if (imp.npotScale != TextureImporterNPOTScale.None) { imp.npotScale = TextureImporterNPOTScale.None; cambiato = true; }
        if (cambiato) imp.SaveAndReimport();
    }
}
 