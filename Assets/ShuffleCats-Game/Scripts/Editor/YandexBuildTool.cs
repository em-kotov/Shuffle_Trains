using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class YandexBuildTool
{
    private const string BuildFolder = "Builds/YandexGames";
    private const string ArchiveFile = "Builds/ShuffleCats-Yandex.zip";
    private const string TemplateName = "PROJECT:YandexGames";

    private const long MaxUncompressedBytes = 100L * 1024L * 1024L;

    [MenuItem("Tools/Yandex Games/Build WebGL + ZIP")]
    public static void Build()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, BuildFolder);
        string archivePath = Path.Combine(projectRoot, ArchiveFile);

        ApplyYandexPlayerSettings();
        // Localization tables are Addressables and must be rebuilt alongside the player.
        LocalizationSetup.BuildAddressables();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"YandexBuildTool - build {report.summary.result}, {report.summary.totalErrors} error(s)");
            return;
        }

        long uncompressed = DirectorySize(outputPath);
        Archive(outputPath, archivePath);

        Debug.Log($"YandexBuildTool - build ready\n" +
                    $"  folder:  {outputPath} ({ToMegabytes(uncompressed)} MB uncompressed)\n" +
                    $"  archive: {archivePath} ({ToMegabytes(new FileInfo(archivePath).Length)} MB)");

        if (uncompressed > MaxUncompressedBytes)
        {
            Debug.LogError($"YandexBuildTool - the console rejects archives whose contents exceed " +
                            $"{ToMegabytes(MaxUncompressedBytes)} MB; this one is {ToMegabytes(uncompressed)} MB");
        }
    }

    public static void BuildFromCommandLine()
    {
        Build();
    }

    private static void ApplyYandexPlayerSettings()
    {
        PlayerSettings.WebGL.template = TemplateName;

        // The host does not set Content-Encoding; compressed builds need the JS fallback decoder.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        PlayerSettings.WebGL.dataCaching = true;

        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);

        AssetDatabase.SaveAssets();
    }

    private static string[] EnabledScenes()
    {
        List<string> scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToList();

        if (scenes.Count == 0)
        {
            Debug.LogError("YandexBuildTool - no scenes enabled in Build Settings");
        }

        return scenes.ToArray();
    }

    // The upload archive must contain index.html at its root.
    private static void Archive(string sourcePath, string archivePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath));

        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        ZipFile.CreateFromDirectory(sourcePath, archivePath, System.IO.Compression.CompressionLevel.Optimal, false);
    }

    private static long DirectorySize(string path)
    {
        return new DirectoryInfo(path)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(file => file.Length);
    }

    private static string ToMegabytes(long bytes)
    {
        return (bytes / (1024f * 1024f)).ToString("0.0");
    }
}
