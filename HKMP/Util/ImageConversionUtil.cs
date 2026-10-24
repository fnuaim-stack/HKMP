using System;
using System.Reflection;
using UnityEngine;

namespace Hkmp.Util;

/// <summary>
/// Invokes Unity's byte-array image loader without resolving its ReadOnlySpan overloads.
/// </summary>
internal static class ImageConversionUtil {
    private static readonly Func<Texture2D, byte[], bool, bool> LoadImageBytes = CreateLoadImageBytes();

    public static bool LoadImage(Texture2D texture, byte[] bytes, bool markNonReadable) {
        return LoadImageBytes(texture, bytes, markNonReadable);
    }

    private static Func<Texture2D, byte[], bool, bool> CreateLoadImageBytes() {
        var method = typeof(ImageConversion).GetMethod(
            "LoadImage",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) },
            null);

        if (method == null) {
            throw new MissingMethodException(typeof(ImageConversion).FullName, "LoadImage(Texture2D, byte[], bool)");
        }

        return (Func<Texture2D, byte[], bool, bool>)Delegate.CreateDelegate(
            typeof(Func<Texture2D, byte[], bool, bool>), method);
    }
}
