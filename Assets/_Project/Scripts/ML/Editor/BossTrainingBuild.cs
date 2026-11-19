using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MatriculaDeHonor.ML.Editor
{
    /// <summary>
    /// Compila la escena de entrenamiento como ejecutable de Windows para que mlagents-learn la
    /// lance con --env (mucho más rápido que entrenar contra el editor).
    /// </summary>
    public static class BossTrainingBuild
    {
        public const string ExecutableName = "BossTraining.exe";
        private const string PathArgument = "-mdhBuildPath";

        /// <summary>Fuera del repo (y de OneDrive): %LOCALAPPDATA%\MatriculaDeHonor\Builds\BossTraining.</summary>
        public static string DefaultOutputFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MatriculaDeHonor", "Builds", "BossTraining");

        [MenuItem("Tools/MDH/ML/Build de entrenamiento (Windows)", priority = 101)]
        private static void BuildFromMenu()
        {
            var report = Build(DefaultOutputFolder);
            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(report.summary.outputPath);
            }
        }

        /// <summary>
        /// Modo batch: <c>-executeMethod MatriculaDeHonor.ML.Editor.BossTrainingBuild.BuildFromCommandLine
        /// [-mdhBuildPath carpeta]</c>. Sale con código 0 si el build va bien.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            var report = Build(ArgumentValue(PathArgument) ?? DefaultOutputFolder);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static BuildReport Build(string outputFolder)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BossTrainingSceneBuilder.ScenePath) == null)
            {
                BossTrainingSceneBuilder.Create(BossTrainingSceneBuilder.ScenePath);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { BossTrainingSceneBuilder.ScenePath },
                locationPathName = Path.Combine(outputFolder, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[MDH] Build de entrenamiento: {report.summary.result} → {options.locationPathName} " +
                      $"({report.summary.totalTime.TotalSeconds:0} s, {report.summary.totalErrors} errores)");
            return report;
        }

        private static string ArgumentValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
