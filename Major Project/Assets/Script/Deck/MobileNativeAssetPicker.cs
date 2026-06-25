using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class MobileNativeAssetPicker
{
    static Action<string> pendingGalleryImagePicked;
    static Action<string> pendingCameraImagePicked;
    static Action<string> pendingAudioPicked;
    static Action<string> pendingModelPicked;

    public static bool TryPickImageFromGallery(Action<string> onPicked, out string error)
    {
        pendingGalleryImagePicked = onPicked;
        return TryInvokePicker(
            "NativeGallery",
            "GetImageFromGallery",
            "MediaPickCallback",
            nameof(OnGalleryImagePicked),
            new object[] { "Select Card Image", "image/*" },
            out error);
    }

    public static bool TryTakePhoto(Action<string> onPicked, out string error)
    {
        pendingCameraImagePicked = onPicked;
        return TryInvokePicker(
            "NativeCamera",
            "TakePicture",
            "CameraCallback",
            nameof(OnCameraImagePicked),
            new object[] { -1, true },
            out error);
    }

    public static bool TryPickAudioFile(Action<string> onPicked, out string error)
    {
        pendingAudioPicked = onPicked;
        return TryInvokePicker(
            "NativeFilePicker",
            "PickFile",
            "FilePickedCallback",
            nameof(OnAudioPicked),
            new object[] { new[] { "audio/*", ".mp3", ".wav", ".ogg" } },
            out error);
    }

    public static bool TryPickModelFile(Action<string> onPicked, out string error)
    {
        pendingModelPicked = onPicked;
        return TryInvokePicker(
            "NativeFilePicker",
            "PickFile",
            "FilePickedCallback",
            nameof(OnModelPicked),
            new object[] { new[] { ".obj", "text/plain", "application/octet-stream" } },
            out error);
    }

    static void OnGalleryImagePicked(string path)
    {
        pendingGalleryImagePicked?.Invoke(path);
        pendingGalleryImagePicked = null;
    }

    static void OnCameraImagePicked(string path)
    {
        pendingCameraImagePicked?.Invoke(path);
        pendingCameraImagePicked = null;
    }

    static void OnAudioPicked(string path)
    {
        pendingAudioPicked?.Invoke(path);
        pendingAudioPicked = null;
    }

    static void OnModelPicked(string path)
    {
        pendingModelPicked?.Invoke(path);
        pendingModelPicked = null;
    }

    static bool TryInvokePicker(string typeName, string methodName, string callbackTypeName, string callbackMethodName, object[] preferredArguments, out string error)
    {
        error = string.Empty;

        Type pickerType = FindType(typeName);
        if (pickerType == null)
        {
            error = $"{typeName} was not found. Add a native picker plugin or pass a file path/URL directly.";
            return false;
        }

        Type callbackType = pickerType.GetNestedType(callbackTypeName, BindingFlags.Public | BindingFlags.NonPublic);
        if (callbackType == null)
        {
            error = $"{typeName}.{callbackTypeName} was not found.";
            return false;
        }

        MethodInfo callbackMethod = typeof(MobileNativeAssetPicker).GetMethod(callbackMethodName, BindingFlags.Static | BindingFlags.NonPublic);
        Delegate callbackDelegate = Delegate.CreateDelegate(callbackType, callbackMethod);

        MethodInfo[] methods = pickerType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == methodName)
            .ToArray();

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0 || !parameters[0].ParameterType.IsAssignableFrom(callbackType))
                continue;

            object[] invokeArgs = BuildArguments(parameters, callbackDelegate, preferredArguments);
            method.Invoke(null, invokeArgs);
            return true;
        }

        error = $"{typeName}.{methodName} has an unsupported signature.";
        return false;
    }

    static object[] BuildArguments(ParameterInfo[] parameters, Delegate callbackDelegate, object[] preferredArguments)
    {
        object[] args = new object[parameters.Length];
        args[0] = callbackDelegate;

        int preferredIndex = 0;
        for (int i = 1; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            if (preferredArguments != null && preferredIndex < preferredArguments.Length)
            {
                object preferred = preferredArguments[preferredIndex];
                if (CanAssign(parameter.ParameterType, preferred))
                {
                    args[i] = preferred;
                    preferredIndex++;
                    continue;
                }
            }

            args[i] = DefaultValueFor(parameter);
        }

        return args;
    }

    static object DefaultValueFor(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        Type type = parameter.ParameterType;
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(int))
            return -1;
        if (type == typeof(bool))
            return false;
        if (type == typeof(string[]))
            return Array.Empty<string>();
        if (type.IsEnum)
            return Activator.CreateInstance(type);
        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return null;
    }

    static bool CanAssign(Type type, object value)
    {
        if (value == null)
            return !type.IsValueType;

        return type.IsInstanceOfType(value);
    }

    static Type FindType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }
}
