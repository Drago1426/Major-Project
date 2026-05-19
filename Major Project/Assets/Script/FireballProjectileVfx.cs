using System.Collections;
using UnityEngine;

public class FireballProjectileVfx : MonoBehaviour
{
    ParticleSystem core;
    ParticleSystem trail;
    ParticleSystem embers;
    ParticleSystem impact;

    public static void Play(Transform owner, Vector3 hitWorldPosition, float distance, float duration)
    {
        if (owner == null)
            return;

        var go = new GameObject("Runtime Fireball Projectile VFX");
        var fireball = go.AddComponent<FireballProjectileVfx>();
        fireball.Begin(owner, hitWorldPosition, Mathf.Max(0.05f, distance), Mathf.Max(0.05f, duration));
    }

    void Begin(Transform owner, Vector3 hitWorldPosition, float distance, float duration)
    {
        EnsureParticleSystems();

        Vector3 start = GetLaunchPoint(owner);
        Vector3 direction = GetLaunchDirection(owner, start, hitWorldPosition);
        Vector3 end = start + direction * distance;

        transform.position = start;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        core.Play(true);
        trail.Play(true);
        embers.Play(true);
        StartCoroutine(Travel(start, end, duration));
    }

    void EnsureParticleSystems()
    {
        if (core != null)
            return;

        core = CreateSystem("Fireball Core", RuntimeParticleAssets.FlameTexture, ConfigureCore);
        trail = CreateSystem("Fireball Trail", RuntimeParticleAssets.FlameTexture, ConfigureTrail);
        embers = CreateSystem("Fireball Embers", RuntimeParticleAssets.SoftDiscTexture, ConfigureEmbers);
        impact = CreateSystem("Fireball Impact", RuntimeParticleAssets.SoftDiscTexture, ConfigureImpact);
    }

    ParticleSystem CreateSystem(string systemName, Texture2D texture, System.Action<ParticleSystem> configure)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform, false);

        var system = go.AddComponent<ParticleSystem>();
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.material = RuntimeParticleAssets.CreateParticleMaterial($"Runtime {systemName} Material", texture, Color.white);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 2f;

        configure(system);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    void ConfigureCore(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.06f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.18f, 0.02f, 1f), new Color(1f, 0.95f, 0.2f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 140f;

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.025f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeFireGradient());

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.35f, 1.15f),
            new Keyframe(1f, 0.2f)));

        var noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.05f;
        noise.frequency = 3.5f;
    }

    void ConfigureTrail(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.12f, 0.02f, 0.9f), new Color(1f, 0.6f, 0.04f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 90f;

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.018f;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = -0.14f;
        velocity.radial = 0.025f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeFireGradient());
    }

    void ConfigureEmbers(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.012f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.35f, 0.02f, 1f), new Color(1f, 0.95f, 0.18f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 50f;

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;
        shape.randomDirectionAmount = 0.7f;
    }

    void ConfigureImpact(ParticleSystem system)
    {
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.18f, 0.03f, 1f), new Color(1f, 0.85f, 0.18f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)42) });

        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.025f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(MakeImpactGradient());
    }

    IEnumerator Travel(Vector3 start, Vector3 end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            Vector3 position = Vector3.Lerp(start, end, p);
            position += Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.035f;
            transform.position = position;
            yield return null;
        }

        transform.position = end;
        core.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        embers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        impact.Play(true);

        yield return new WaitForSeconds(0.8f);
        Destroy(gameObject);
    }

    Vector3 GetLaunchPoint(Transform owner)
    {
        var renderers = owner.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return owner.position + owner.up * 0.04f;

        return bounds.center + owner.up * Mathf.Max(0.01f, bounds.extents.y * 0.2f);
    }

    Vector3 GetLaunchDirection(Transform owner, Vector3 start, Vector3 hitWorldPosition)
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 towardCamera = camera.transform.position - start;
            if (towardCamera.sqrMagnitude > 0.0001f)
                return towardCamera.normalized;
        }

        Vector3 towardHit = hitWorldPosition - start;
        if (towardHit.sqrMagnitude > 0.0001f)
            return towardHit.normalized;

        return owner.forward.sqrMagnitude > 0.0001f ? owner.forward.normalized : Vector3.forward;
    }

    Gradient MakeFireGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.22f, 0.03f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.01f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.75f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    Gradient MakeImpactGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.18f, 0.03f), 0.55f),
                new GradientColorKey(new Color(0.3f, 0.03f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }
}
