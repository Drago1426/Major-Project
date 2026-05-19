using UnityEngine;
using UnityEngine.Rendering;

public static class RuntimeParticleAssets
{
    static Texture2D softDiscTexture;
    static Texture2D flameTexture;

    public static Texture2D SoftDiscTexture
    {
        get
        {
            if (softDiscTexture == null)
                softDiscTexture = CreateSoftDiscTexture();

            return softDiscTexture;
        }
    }

    public static Texture2D FlameTexture
    {
        get
        {
            if (flameTexture == null)
                flameTexture = CreateFlameTexture();

            return flameTexture;
        }
    }

    public static Material CreateParticleMaterial(string materialName, Texture2D texture, Color tint, bool additive = true)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.name = materialName;
        material.mainTexture = texture;
        material.color = tint;

        SetTexture(material, "_BaseMap", texture);
        SetTexture(material, "_MainTex", texture);
        SetColor(material, "_BaseColor", tint);
        SetColor(material, "_TintColor", tint);
        ConfigureTransparency(material, additive);

        return material;
    }

    static void ConfigureTransparency(Material material, bool additive)
    {
        if (material == null)
            return;

        SetFloat(material, "_Surface", 1f);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Blend", additive ? 2f : 0f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    static void SetFloat(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    static void SetTexture(Material material, string propertyName, Texture texture)
    {
        if (material != null && material.HasProperty(propertyName))
            material.SetTexture(propertyName, texture);
    }

    static void SetColor(Material material, string propertyName, Color color)
    {
        if (material != null && material.HasProperty(propertyName))
            material.SetColor(propertyName, color);
    }

    static Texture2D CreateSoftDiscTexture()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Soft Particle Disc";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(u * u + v * v);
                float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.18f, 1f, radius));
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    static Texture2D CreateFlameTexture()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Flame Particle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size;
                float taper = Mathf.Lerp(0.9f, 0.12f, Mathf.Pow(v, 1.35f));
                float horizontal = Mathf.Abs(u) / Mathf.Max(0.001f, taper);
                float body = Mathf.Clamp01(1f - Mathf.SmoothStep(0.1f, 1f, horizontal));
                float verticalFade = Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI);
                float noise = Mathf.PerlinNoise(x * 0.12f, y * 0.18f) * 0.28f + 0.86f;
                float alpha = Mathf.Clamp01(body * verticalFade * noise);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }
}
