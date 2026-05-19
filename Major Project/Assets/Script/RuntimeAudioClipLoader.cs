using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class RuntimeAudioClipLoader
{
    public static IEnumerator Load(string pathOrResource, Action<AudioClip> onLoaded)
    {
        if (string.IsNullOrWhiteSpace(pathOrResource))
            yield break;

        string trimmedPath = pathOrResource.Trim();

        if (!File.Exists(trimmedPath))
        {
            var resourceClip = Resources.Load<AudioClip>(Path.ChangeExtension(trimmedPath, null));
            if (resourceClip != null)
            {
                onLoaded?.Invoke(resourceClip);
                yield break;
            }
        }

        string url = BuildUrl(trimmedPath);
        if (string.IsNullOrWhiteSpace(url))
            yield break;

        using var request = UnityWebRequestMultimedia.GetAudioClip(url, GuessAudioType(trimmedPath));
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Failed to load audio clip '{trimmedPath}': {request.error}");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            Debug.LogWarning($"Loaded audio clip was empty: {trimmedPath}");
            yield break;
        }

        clip.name = Path.GetFileNameWithoutExtension(trimmedPath);
        onLoaded?.Invoke(clip);
    }

    static string BuildUrl(string pathOrUrl)
    {
        if (File.Exists(pathOrUrl))
            return new Uri(pathOrUrl).AbsoluteUri;

        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri uri))
        {
            if (!string.IsNullOrWhiteSpace(uri.Scheme))
                return uri.IsFile ? uri.AbsoluteUri : pathOrUrl;
        }

        Debug.LogWarning($"Audio file path does not exist: {pathOrUrl}");
        return string.Empty;
    }

    static AudioType GuessAudioType(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".wav" => AudioType.WAV,
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            ".aif" => AudioType.AIFF,
            ".aiff" => AudioType.AIFF,
            _ => AudioType.UNKNOWN
        };
    }
}
