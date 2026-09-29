using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    public static class PlaytestBuild
    {
        public static readonly string[] ReleaseScenes =
        {
            PlaytestSessionController.TitleScene, PlaytestSessionController.GameplayScene,
            "Assets/Scenes/BlueArea_Tutorial.unity", "Assets/Scenes/PinkArea_Perimeters.unity"
        };

        [Serializable]
        private sealed class BuildInfo
        {
            public string buildId, builtAtUtc, unityVersion, target, commit, playerLog;
            public bool dirty;
        }

        [MenuItem("TIC/Build/Windows Playtest")]
        public static void BuildWindows()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows build support is not installed.");
            foreach (var path in ReleaseScenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new InvalidOperationException("Missing release scene: " + path);
            if (Resources.Load<GameObject>("Runtime/PlaytestSession") == null)
                throw new InvalidOperationException("Run the frontend setup command first.");
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var buildId = "playtest-" + stamp;
            var output = Path.GetFullPath("Builds/" + buildId + "/Windows");
            Directory.CreateDirectory(output);
            PlayerSettings.bundleVersion = buildId;
            PlayerSettings.productName = "CardMetroidvania";
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = ReleaseScenes,
                locationPathName = Path.Combine(output, "CardMetroidvania.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Playtest build failed: " + report.summary.result);
            var info = new BuildInfo
            {
                buildId = buildId, builtAtUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                target = "Windows x64", commit = Git("rev-parse HEAD"), dirty = Git("status --porcelain").Length > 0,
                playerLog = $"%USERPROFILE%/AppData/LocalLow/{PlayerSettings.companyName}/{PlayerSettings.productName}/Player.log"
            };
            File.WriteAllText(Path.Combine(output, "build-info.json"), JsonUtility.ToJson(info, true));
            foreach (var name in new[] { "README", "FEEDBACK", "KNOWN_ISSUES" })
                File.Copy($"playtest/{name}-template.txt", Path.Combine(output, name + ".txt"), true);
            Directory.CreateDirectory(".utmp/playtest");
            File.WriteAllText(".utmp/playtest/latest-build.txt", output);
            Debug.Log("PLAYTEST_BUILD_SUCCESS: " + output);
        }

        private static string Git(string arguments)
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            });
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException("Could not capture build Git metadata.");
            return result;
        }
    }
}
