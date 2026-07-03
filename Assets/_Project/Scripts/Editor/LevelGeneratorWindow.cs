using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyTownRoads.EditorTools
{
    /// <summary>
    /// Editor tool for generating solver-validated levels into Resources/Levels,
    /// and for re-validating everything already there.
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        int width = 7;
        int height = 7;
        int minPairs = 4;
        int maxPairs = 7;
        int obstacleCount = 0;
        int maxPathLen = 10;
        int count = 5;
        int startId = 100;
        int seed = 0;
        string outputFolder = "Assets/_Project/Resources/Levels";

        [MenuItem("Tools/Tiny Town Roads/Level Generator")]
        static void Open()
        {
            GetWindow<LevelGeneratorWindow>("Level Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("Board", EditorStyles.boldLabel);
            width = EditorGUILayout.IntSlider("Width", width, 5, 14);
            height = EditorGUILayout.IntSlider("Height", height, 5, 14);
            minPairs = EditorGUILayout.IntSlider("Min pairs", minPairs, 3, 12);
            maxPairs = EditorGUILayout.IntSlider("Max pairs", maxPairs, minPairs, 12);
            obstacleCount = EditorGUILayout.IntSlider("Obstacles", obstacleCount, 0, 6);
            maxPathLen = EditorGUILayout.IntSlider("Max path length", maxPathLen, 5, 99);

            GUILayout.Space(8);
            GUILayout.Label("Output", EditorStyles.boldLabel);
            count = EditorGUILayout.IntField("Level count", count);
            startId = EditorGUILayout.IntField("Start levelId", startId);
            seed = EditorGUILayout.IntField("Seed (0 = random)", seed);
            outputFolder = EditorGUILayout.TextField("Folder", outputFolder);

            GUILayout.Space(12);
            if (GUILayout.Button("Generate")) Generate();
            if (GUILayout.Button("Validate all levels in folder")) ValidateAll();
        }

        void Generate()
        {
            Directory.CreateDirectory(outputFolder);
            var rng = seed == 0 ? new System.Random() : new System.Random(seed);
            int written = 0;
            for (int i = 0; i < count; i++)
            {
                EditorUtility.DisplayProgressBar("Generating levels", $"Level {i + 1}/{count}", i / (float)count);
                var level = LevelGenerator.Generate(width, height, minPairs, maxPairs, obstacleCount, rng, maxPathLen);
                if (level == null)
                {
                    Debug.LogWarning($"Could not generate a unique-solution {width}x{height} level (try fewer constraints).");
                    continue;
                }
                level.levelId = startId + i;
                File.WriteAllText(Path.Combine(outputFolder, $"level_{level.levelId:000}.json"), level.ToJson());
                written++;
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"Level generator: wrote {written}/{count} levels to {outputFolder}");
        }

        void ValidateAll()
        {
            var files = Directory.GetFiles(outputFolder, "*.json");
            int ok = 0;
            foreach (var file in files)
            {
                var level = LevelData.FromJson(File.ReadAllText(file));
                int solutions = Solver.CountSolutions(level, 2);
                if (solutions == 1) { ok++; continue; }
                string verdict = solutions == 0 ? "NO solution"
                    : solutions < 0 ? "budget exhausted (unknown)"
                    : "MULTIPLE solutions";
                Debug.LogWarning($"{Path.GetFileName(file)} (level {level.levelId}): {verdict}");
            }
            Debug.Log($"Validation: {ok}/{files.Length} levels have a unique solution.");
        }
    }
}
