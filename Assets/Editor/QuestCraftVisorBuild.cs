using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class QuestCraftVisorBuild
{
    public static void BuildAndroid()
    {
        var outputDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "..", "..", "outputs"));
        var outputPath = Path.Combine(outputDirectory, "Plinth-1.20.1.apk");
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        var useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        var applicationIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        var productName = PlayerSettings.productName;
        var bundleVersion = PlayerSettings.bundleVersion;
        var version = GetVersionArg();

        Directory.CreateDirectory(outputDirectory);

        try
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException("Unable to switch the active build target to Android.");
            }

            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.plinthmc.app");
            PlayerSettings.productName = "Plinth";
            if (!string.IsNullOrEmpty(version))
            {
                PlayerSettings.bundleVersion = version;
            }
            EnableHandTracking();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android build failed with {report.summary.totalErrors} errors.");
            }
        }
        finally
        {
            PlayerSettings.Android.useCustomKeystore = useCustomKeystore;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, applicationIdentifier);
            PlayerSettings.productName = productName;
            PlayerSettings.bundleVersion = bundleVersion;
        }
    }

    // -holocraftVersion <v> on the command line, or HOLOCRAFT_VERSION in the env
    private static string GetVersionArg()
    {
        var args = Environment.GetCommandLineArgs();
        var i = Array.IndexOf(args, "-holocraftVersion");
        var version = i >= 0 && i + 1 < args.Length ? args[i + 1] : Environment.GetEnvironmentVariable("HOLOCRAFT_VERSION");
        return string.IsNullOrWhiteSpace(version) ? null : version.Trim().TrimStart('v');
    }

    private static void EnableHandTracking()
    {
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        foreach (var feature in settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>().Where(feature => feature.name.StartsWith("PICO", StringComparison.Ordinal)))
        {
            feature.enabled = false;
            EditorUtility.SetDirty(feature);
        }

        var features = new UnityEngine.XR.OpenXR.Features.OpenXRFeature[]
        {
            settings.GetFeature<HandTracking>(),
            settings.GetFeature<MetaHandTrackingAim>(),
            settings.GetFeature<MetaQuestTouchPlusControllerProfile>(),
            settings.GetFeature<OculusTouchControllerProfile>()
        };

        foreach (var feature in features)
        {
            if (feature != null)
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }

        AssetDatabase.SaveAssets();
    }
}
