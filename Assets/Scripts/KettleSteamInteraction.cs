using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
 
/// <summary>
/// Bollitore: crea da solo il vapore (Particle System) che esce dal beccuccio.
///  - parte da solo quando si apre il primo piano del bollitore (messaggio SuPrimoPiano)
///  - oppure premendo il tasto scelto (G) quando il pulcino è vicino.
/// Lo attacca CostruisciPrimiPiani al modello Kettle e collega da solo il Player.
/// </summary>
public class KettleSteamInteraction : MonoBehaviour
{
    [Header("Optional references (usually found automatically)")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform spout;
 
    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.6f;
    // Tasto per accendere/spegnere. Non F (friggitrice), non Q/E (telecamera), non B (forno).
    [SerializeField] private Key tasto = Key.G;
    [SerializeField, Min(0.1f)] private float steamDuration = 5f;
    [SerializeField] private bool showHint = true;
 
    [Header("Steam appearance")]
    [SerializeField] private Color steamColor = new Color(1f, 1f, 1f, 0.58f);
    [SerializeField, Range(1f, 50f)] private float particlesPerSecond = 22f;
    [SerializeField] private Vector3 steamPositionAdjustment = Vector3.zero;
 
    private ParticleSystem steam;
    private GameObject steamObject;
    private Material steamMaterial;
    private Texture2D steamTexture;
    private bool isSteaming;
    private float timeRemaining;
 
    private void Start()
    {
        if (player == null)
        {
            GameObject chick = GameObject.Find("Pulcino");
            if (chick == null) chick = GameObject.Find("Pulcino_Player");
            if (chick == null) chick = GameObject.Find("Player");
            if (chick == null) chick = GameObject.Find("Chick");
            if (chick != null) player = chick.transform;
        }
 
        if (spout == null)
            spout = FindNamedChild(transform, "spout", "beccuccio", "cone");   // nel modello il beccuccio si chiama "Cone"
 
        if (player == null)
            Debug.LogWarning("Kettle: drag the pulcino (Pulcino_Player) into Player.", this);
        if (spout == null)
            Debug.LogWarning("Kettle: cannot find Kettle_Spout. Drag the spout into Spout.", this);
 
        CreateSteam();
    }
 
    private void Update()
    {
        if (isSteaming)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f) StopSteam();
        }
 
        if (steam == null || !PlayerIsNear() || !TastoPremuto()) return;
        if (isSteaming) StopSteam();
        else StartSteam();
    }
 
    private bool PlayerIsNear()
    {
        if (player == null) return false;
        Vector3 a = player.position;
        Vector3 b = transform.position;
        // Only horizontal distance: the kettle may be on a kitchen counter.
        a.y = b.y = 0f;
        return Vector3.Distance(a, b) <= interactionDistance;
    }
 
    private bool TastoPremuto()
    {
        return Keyboard.current != null && Keyboard.current[tasto].wasPressedThisFrame;
    }
 
    // Chiamato da PrimoPiano: quando si apre il primo piano del bollitore, parte il vapore
    public void SuPrimoPiano(bool aperto)
    {
        if (aperto && steam != null && !isSteaming) StartSteam();
    }
 
    private void CreateSteam()
    {
        steamObject = new GameObject("Steam_Automatico");
        steamObject.transform.SetParent(transform, true);
        steamObject.transform.position = FindSteamTip() + steamPositionAdjustment;
        // A Unity particle cone emits along its local +Z; turn it toward world +Y.
        steamObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
 
        steam = steamObject.AddComponent<ParticleSystem>();
        // Appena creato, il Particle System parte da solo: lo fermo SUBITO,
        // altrimenti Unity non permette di cambiargli la durata
        steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = steam.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Il modello è scalato (×100 da Blender e poi adattato allo spazio): così il vapore
        // usa la scala vera nel mondo e non quella "strana" del genitore
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
        main.startColor = steamColor;
        main.gravityModifier = 0f;
        main.maxParticles = 140;
 
        var emission = steam.emission;
        emission.enabled = true;
        emission.rateOverTime = particlesPerSecond;
 
        var shape = steam.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = 0.008f;
 
        var sizeOverLifetime = steam.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.5f, 1.0f),
            new Keyframe(1f, 1.55f)));
 
        var colorOverLifetime = steam.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f, 0.2f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;
 
        var noise = steam.noise;
        noise.enabled = true;
        noise.strength = 0.035f;
        noise.frequency = 0.4f;
 
        var renderer = steam.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        MakeSoftParticleMaterial(renderer);
 
        steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
 
    // Automatically places steam near the outward tip of the spout mesh.
    // You can fine-tune this from the inspector via Steam Position Adjustment.
    private Vector3 FindSteamTip()
    {
        if (spout == null)
            return transform.position + Vector3.up * 0.35f;
 
        Transform body = FindNamedChild(transform, "body", "corpo");
        Renderer bodyRenderer = body != null ? body.GetComponentInChildren<Renderer>() : null;
        Renderer spoutRenderer = spout.GetComponentInChildren<Renderer>();
        Vector3 bodyCenter = bodyRenderer != null ? bodyRenderer.bounds.center : transform.position;
        Vector3 spoutCenter = spoutRenderer != null ? spoutRenderer.bounds.center : spout.position;
        Vector3 direction = spoutCenter - bodyCenter;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.00001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }
        direction.Normalize();
 
        MeshFilter filter = spout.GetComponentInChildren<MeshFilter>();
        // Leggere i vertici si può solo se il modello ha "Read/Write" attivo nell'import:
        // se non ce l'ha, uso la scatola (bounds) del beccuccio, che va bene lo stesso
        if (filter != null && filter.sharedMesh != null && filter.sharedMesh.isReadable)
        {
            Vector3[] vertices = filter.sharedMesh.vertices;
            float maxDistance = float.NegativeInfinity;
            Vector3 tip = spoutCenter;
            // Spout meshes are normally small. Find the farthest point from the body.
            foreach (Vector3 vertex in vertices)
            {
                Vector3 point = filter.transform.TransformPoint(vertex);
                float distance = Vector3.Dot(point - bodyCenter, direction);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    tip = point;
                }
            }
            return tip + direction * 0.005f + Vector3.up * 0.006f;
        }
 
        if (spoutRenderer != null)
        {
            Bounds b = spoutRenderer.bounds;
            float length = Mathf.Abs(direction.x) * b.extents.x + Mathf.Abs(direction.z) * b.extents.z;
            return b.center + direction * length + Vector3.up * 0.01f;
        }
        return spout.position + direction * 0.045f;
    }
 
    private void MakeSoftParticleMaterial(ParticleSystemRenderer renderer)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return; // Keep the default material if none is present.
 
        steamTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        steamTexture.name = "Steam_Soft_Circle_Runtime";
        steamTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float radiusSq = dx * dx + dy * dy;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - radiusSq), 2f);
                steamTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        steamTexture.Apply();
 
        steamMaterial = new Material(shader) { name = "Steam_Material_Runtime" };
        if (steamMaterial.HasProperty("_BaseMap")) steamMaterial.SetTexture("_BaseMap", steamTexture);
        if (steamMaterial.HasProperty("_MainTex")) steamMaterial.SetTexture("_MainTex", steamTexture);
        if (steamMaterial.HasProperty("_BaseColor")) steamMaterial.SetColor("_BaseColor", Color.white);
        if (steamMaterial.HasProperty("_Color")) steamMaterial.SetColor("_Color", Color.white);
        if (steamMaterial.HasProperty("_Surface"))
        {
            steamMaterial.SetFloat("_Surface", 1f); // Transparent in URP.
            steamMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (steamMaterial.HasProperty("_SrcBlend")) steamMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (steamMaterial.HasProperty("_DstBlend")) steamMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (steamMaterial.HasProperty("_ZWrite")) steamMaterial.SetFloat("_ZWrite", 0f);
        steamMaterial.renderQueue = (int)RenderQueue.Transparent;
        renderer.sharedMaterial = steamMaterial;
    }
 
    private static Transform FindNamedChild(Transform root, params string[] names)
    {
        foreach (Transform child in root)
        {
            string lower = child.name.ToLowerInvariant();
            foreach (string part in names)
                if (lower.Contains(part)) return child;
            Transform found = FindNamedChild(child, names);
            if (found != null) return found;
        }
        return null;
    }
 
    private void StartSteam()
    {
        steam.Clear(true);
        steam.Play(true);
        isSteaming = true;
        timeRemaining = steamDuration;
    }
 
    private void StopSteam()
    {
        steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        isSteaming = false;
    }
 
    private void OnGUI()
    {
        if (!showHint || !PlayerIsNear()) return;
        string message = isSteaming ? $"Premi {tasto} per fermare il vapore" : $"Premi {tasto} per accendere il bollitore";
        Avviso.Disegna(message);
    }
 
    private void OnDestroy()
    {
        if (steamMaterial != null) Destroy(steamMaterial);
        if (steamTexture != null) Destroy(steamTexture);
    }
}
 
