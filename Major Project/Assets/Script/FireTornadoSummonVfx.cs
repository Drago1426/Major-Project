using UnityEngine;

[DisallowMultipleComponent]
public class FireTornadoSummonVfx : MonoBehaviour
{
    [Header("Size")]
    public float baseRadius = 0.04f;
    public float height = 0.22f;

    [Header("Timing")]
    public float warmupBurstCount = 40f;

    ParticleSystem flames;
    ParticleSystem embers;
    ParticleSystem smoke;
    Material flameMaterial;
    Material emberMaterial;
    Material smokeMaterial;

    public static FireTornadoSummonVfx Ensure(Transform parent, string childName = "FireTornadoSummonVFX")
    {
        if (parent == null)
            return null;

        var existing = parent.GetComponentInChildren<FireTornadoSummonVfx>(true);
        if (existing != null)
            return existing;

        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go.AddComponent<FireTornadoSummonVfx>();
    }

    void Awake()
    {
        EnsureParticleSystems();
        Stop(true);
    }

    public void Play()
    {
        EnsureParticleSystems();

        gameObject.SetActive(true);
        Restart(flames);
        Restart(embers);
        Restart(smoke);
    }

    public void Stop(bool clear = false)
    {
        var stopMode = clear
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        if (flames != null) flames.Stop(true, stopMode);
        if (embers != null) embers.Stop(true, stopMode);
        if (smoke != null) smoke.Stop(true, stopMode);
    }

    void Restart(ParticleSystem system)
    {
        if (system == null)
            return;

        system.Clear(true);
        system.Play(true);
    }

    void EnsureParticleSystems()
    {
        if (flameMaterial == null)
            flameMaterial = RuntimeParticleAssets.CreateParticleMaterial(
                "Runtime Fire Tornado Flame",
                RuntimeParticleAssets.FlameTexture,
                Color.white);

        if (emberMaterial == null)
            emberMaterial = RuntimeParticleAssets.CreateParticleMaterial(
                "Runtime Fire Tornado Ember",
                RuntimeParticleAssets.SoftDiscTexture,
                Color.white);

        if (smokeMaterial == null)
            smokeMaterial = RuntimeParticleAssets.CreateParticleMaterial(
                "Runtime Fire Tornado Smoke",
                RuntimeParticleAssets.SoftDiscTexture,
                Color.white,
                false);

        if (flames == null)
            flames = CreateChildSystem("Flame Spiral", flameMaterial);

        if (embers == null)
            embers = CreateChildSystem("Ember Sparks", emberMaterial);

        if (smoke == null)
            smoke = CreateChildSystem("Heat Smoke", smokeMaterial);

        ConfigureFlames(flames);
        ConfigureEmbers(embers);
        ConfigureSmoke(smoke);
    }

    ParticleSystem CreateChildSystem(string childName, Material material)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);

        var system = go.AddComponent<ParticleSystem>();
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 1f;

        return system;
    }

    void ConfigureFlames(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.18f, 0.02f, 0.95f),
            new Color(1f, 0.85f, 0.14f, 0.95f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 230f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(warmupBurstCount), 0, short.MaxValue)) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = baseRadius;
        shape.arc = 360f;
        shape.randomDirectionAmount = 0.35f;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = height * 1.25f;
        velocity.z = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 15f;
        velocity.orbitalZ = 0f;
        velocity.radial = baseRadius * 0.28f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeFireGradient());

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.25f, 1.25f),
            new Keyframe(0.75f, 0.65f),
            new Keyframe(1f, 0f)));

        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 3f;
        noise.scrollSpeed = 2.2f;

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.8f;
        renderer.velocityScale = 0.08f;
    }

    void ConfigureEmbers(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.0035f, 0.008f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.35f, 0.04f, 1f),
            new Color(1f, 0.95f, 0.25f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 70f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = baseRadius * 1.15f;
        shape.randomDirectionAmount = 0.6f;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = height * 1.2f;
        velocity.z = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 7f;
        velocity.orbitalZ = 0f;
        velocity.radial = baseRadius * 0.8f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeEmberGradient());
    }

    void ConfigureSmoke(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1.8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.035f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.12f, 0.1f, 0.09f, 0.2f),
            new Color(0.28f, 0.22f, 0.18f, 0.28f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 22f;

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = baseRadius * 0.8f;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = height * 0.5f;
        velocity.z = 0f;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 3.5f;
        velocity.orbitalZ = 0f;
        velocity.radial = baseRadius * 0.35f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeSmokeGradient());
    }

    Gradient MakeFireGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.22f, 0.03f), 0.45f),
                new GradientColorKey(new Color(0.32f, 0.02f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.7f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    Gradient MakeEmberGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.25f), 0f),
                new GradientColorKey(new Color(1f, 0.32f, 0.04f), 0.65f),
                new GradientColorKey(new Color(0.4f, 0.04f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    Gradient MakeSmokeGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.12f, 0.1f, 0.09f), 0f),
                new GradientColorKey(new Color(0.28f, 0.22f, 0.18f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.22f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }
}
