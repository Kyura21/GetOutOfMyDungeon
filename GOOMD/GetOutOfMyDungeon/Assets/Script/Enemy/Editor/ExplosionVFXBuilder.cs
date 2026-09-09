using UnityEditor;
using UnityEngine;

// Genera un prefab di esplosione fatto di schegge d'osso e un lampo.
//
// Tutto a particelle MESH, senza texture ne' materiali trasparenti: e' la
// scelta piu' robusta, perche' una particella billboard senza la texture giusta
// o un materiale trasparente configurato male in URP finisce per non vedersi o
// diventare rosa. Le schegge sono cubetti, il lampo una sfera che si gonfia e
// si richiude: niente da importare, niente shader da indovinare.
//
// Sta in una cartella Editor: non finisce nella build.
public static class ExplosionVFXBuilder
{
    private const string Folder = "Assets/Prefabs/VFX";

    [MenuItem("GOOMD/Crea VFX esplosione")]
    public static void CreateExplosion()
    {
        EnsureFolder();

        Material shardMaterial = CreateMaterial("M_ExplosionShard",
            new Color(0.92f, 0.89f, 0.78f), Color.black);

        Material flashMaterial = CreateMaterial("M_ExplosionFlash",
            new Color(1f, 0.55f, 0.12f), new Color(2.5f, 1.1f, 0.2f));

        Mesh cube = PrimitiveMesh(PrimitiveType.Cube);
        Mesh sphere = PrimitiveMesh(PrimitiveType.Sphere);

        GameObject root = new GameObject("VFX_Explosion");

        BuildFlash(root, sphere, flashMaterial);
        BuildShards(root, cube, shardMaterial);

        string path = AssetDatabase.GenerateUniqueAssetPath($"{Folder}/VFX_Explosion.prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"VFX creato in {path}. Assegnalo a SkullExplosion.explosionVFX " +
                  "(e volendo a EnemyExplosive.explosionVFX e EnemyHealth.deathVFX).", prefab);
    }

    // il lampo: una sfera che si gonfia di colpo e si richiude
    static void BuildFlash(GameObject parent, Mesh mesh, Material material)
    {
        GameObject go = new GameObject("Flash");
        go.transform.SetParent(parent.transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.15f));
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        // gonfia in fretta e si richiude: senza trasparenza e' la dimensione a
        // far sparire il lampo
        AnimationCurve pop = new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f));

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, pop);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // le schegge: cubetti d'osso sparati in giro, che cadono e rimpiccioliscono
    static void BuildShards(GameObject parent, Mesh mesh, Material material)
    {
        GameObject go = new GameObject("Shards");
        go.transform.SetParent(parent.transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.95f, 0.93f, 0.84f), new Color(0.75f, 0.72f, 0.62f));
        main.gravityModifier = new ParticleSystem.MinMaxCurve(1.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        // orientate a caso: cubi tutti allineati si leggono come una griglia
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.dampen = 0.15f;
        drag.limit = new ParticleSystem.MinMaxCurve(6f);

        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 0.9f),
            new Keyframe(1f, 0f));

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Material CreateMaterial(string name, Color color, Color emission)
    {
        string path = $"{Folder}/{name}.mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        // il progetto e' in URP: se per qualche motivo lo shader non c'e',
        // ripiego sullo standard invece di produrre un materiale rosa
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = name;

        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);

        if (emission.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", emission);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Mesh PrimitiveMesh(PrimitiveType type)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        return mesh;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");
    }
}
