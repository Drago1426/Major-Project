using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class RuntimeDeckAssetStore
{
    public const string CardImagesFolder = "deck_card_images";
    public const string CardAudioFolder = "deck_card_audio";
    public const string CardModelsFolder = "deck_card_models";

    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    static readonly string[] AudioExtensions = { ".mp3", ".wav", ".ogg", ".aif", ".aiff" };
    static readonly string[] ModelExtensions = { ".obj" };

    public static bool TryCopyImageFile(string sourcePath, string cardName, out string storedPath, out string error)
    {
        return TryCopyFile(sourcePath, CardImagesFolder, cardName, "image", ImageExtensions, out storedPath, out error);
    }

    public static bool TryCopyAudioFile(string sourcePath, string cardName, string slot, out string storedPath, out string error)
    {
        return TryCopyFile(sourcePath, CardAudioFolder, cardName, slot, AudioExtensions, out storedPath, out error);
    }

    public static bool TryCopyModelFile(string sourcePath, string cardName, out string storedPath, out string error)
    {
        return TryCopyFile(sourcePath, CardModelsFolder, cardName, "model", ModelExtensions, out storedPath, out error);
    }

    public static bool TrySaveTextureAsPng(Texture2D texture, string cardName, out string storedPath, out string error)
    {
        storedPath = string.Empty;
        error = string.Empty;

        if (texture == null)
        {
            error = "No texture was provided.";
            return false;
        }

        Texture2D readableTexture = EnsureReadable(texture, out bool createdCopy);
        if (readableTexture == null)
        {
            error = "Could not prepare a readable image.";
            return false;
        }

        byte[] pngData = readableTexture.EncodeToPNG();
        if (createdCopy)
            UnityEngine.Object.Destroy(readableTexture);

        if (pngData == null || pngData.Length == 0)
        {
            error = "Could not encode image as PNG.";
            return false;
        }

        string filePath = BuildUniquePersistentPath(CardImagesFolder, cardName, "image", ".png");
        File.WriteAllBytes(filePath, pngData);
        storedPath = filePath;
        return true;
    }

    public static IEnumerator DownloadFile(string url, string folderName, string fileStem, string slot, string[] allowedExtensions, Action<string> onSaved, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            onError?.Invoke("URL is required.");
            yield break;
        }

        using var request = UnityWebRequest.Get(url.Trim());
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"Download failed: {request.error}");
            yield break;
        }

        byte[] data = request.downloadHandler?.data;
        if (data == null || data.Length == 0)
        {
            onError?.Invoke("Download returned no data.");
            yield break;
        }

        string extension = GuessExtension(url, request.GetResponseHeader("Content-Type"), allowedExtensions);
        if (!IsAllowedExtension(extension, allowedExtensions))
        {
            onError?.Invoke($"Downloaded file extension '{extension}' is not supported.");
            yield break;
        }

        string filePath = BuildUniquePersistentPath(folderName, fileStem, slot, extension);
        File.WriteAllBytes(filePath, data);
        onSaved?.Invoke(filePath);
    }

    public static IEnumerator DownloadImage(string url, string cardName, Action<string> onSaved, Action<string> onError)
    {
        yield return DownloadFile(url, CardImagesFolder, cardName, "image", ImageExtensions, onSaved, onError);
    }

    public static IEnumerator DownloadSummonAudio(string url, string cardName, Action<string> onSaved, Action<string> onError)
    {
        yield return DownloadFile(url, CardAudioFolder, cardName, "summon", AudioExtensions, onSaved, onError);
    }

    public static IEnumerator DownloadFireballAudio(string url, string cardName, Action<string> onSaved, Action<string> onError)
    {
        yield return DownloadFile(url, CardAudioFolder, cardName, "fireball", AudioExtensions, onSaved, onError);
    }

    public static IEnumerator DownloadModel(string url, string cardName, Action<string> onSaved, Action<string> onError)
    {
        yield return DownloadFile(url, CardModelsFolder, cardName, "model", ModelExtensions, onSaved, onError);
    }

    static bool TryCopyFile(string sourcePath, string folderName, string cardName, string slot, string[] allowedExtensions, out string storedPath, out string error)
    {
        storedPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            error = "File path is required.";
            return false;
        }

        string trimmedPath = sourcePath.Trim();
        if (!File.Exists(trimmedPath))
        {
            error = $"File does not exist: {trimmedPath}";
            return false;
        }

        string extension = Path.GetExtension(trimmedPath).ToLowerInvariant();
        if (!IsAllowedExtension(extension, allowedExtensions))
        {
            error = $"Unsupported file extension '{extension}'.";
            return false;
        }

        string destinationPath = BuildUniquePersistentPath(folderName, cardName, slot, extension);
        File.Copy(trimmedPath, destinationPath, true);
        storedPath = destinationPath;
        return true;
    }

    static Texture2D EnsureReadable(Texture2D source, out bool createdCopy)
    {
        createdCopy = false;
        if (source == null)
            return null;

        if (source.isReadable)
            return source;

        RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, temporaryRenderTexture);
        RenderTexture.active = temporaryRenderTexture;

        var readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTexture.ReadPixels(new Rect(0, 0, temporaryRenderTexture.width, temporaryRenderTexture.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporaryRenderTexture);

        createdCopy = true;
        return readableTexture;
    }

    static string BuildUniquePersistentPath(string folderName, string fileStem, string slot, string extension)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        Directory.CreateDirectory(folderPath);

        string safeStem = SanitizeFileName(fileStem);
        string safeSlot = SanitizeFileName(slot);
        return Path.Combine(folderPath, $"{safeStem}_{safeSlot}_{Guid.NewGuid():N}{extension}");
    }

    static string GuessExtension(string url, string contentType, string[] allowedExtensions)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            string extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
            if (IsAllowedExtension(extension, allowedExtensions))
                return extension;
        }

        string normalizedContentType = contentType?.ToLowerInvariant() ?? string.Empty;
        if (normalizedContentType.Contains("png"))
            return ".png";
        if (normalizedContentType.Contains("jpeg") || normalizedContentType.Contains("jpg"))
            return ".jpg";
        if (normalizedContentType.Contains("mpeg") || normalizedContentType.Contains("mp3"))
            return ".mp3";
        if (normalizedContentType.Contains("wav"))
            return ".wav";
        if (normalizedContentType.Contains("ogg"))
            return ".ogg";
        if (normalizedContentType.Contains("obj"))
            return ".obj";

        return allowedExtensions != null && allowedExtensions.Length > 0 ? allowedExtensions[0] : string.Empty;
    }

    static bool IsAllowedExtension(string extension, string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(extension) || allowedExtensions == null || allowedExtensions.Length == 0)
            return false;

        for (int i = 0; i < allowedExtensions.Length; i++)
        {
            if (string.Equals(extension, allowedExtensions[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "asset";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = value.Trim();
        for (int i = 0; i < invalidChars.Length; i++)
            safe = safe.Replace(invalidChars[i].ToString(), string.Empty);

        return string.IsNullOrWhiteSpace(safe) ? "asset" : safe.Replace(" ", "_");
    }
}
