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
        int minColors = 4;
        int maxColors = 7;
        int obstacleCount = 0;
        int maxBranchLen = 6;
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
            minColors = EditorGUILayout.IntSlider("Min colors (cities)", minColors, 2, 12);
            maxColors = EditorGUILayout.IntSlider("Max colors (cities)", maxColors, minColors, 12);
            obstacleCount = EditorGUILayout.IntSlider("Obstacles", obstacleCount, 0, 6);
            maxBranchLen = EditorGUILayout.IntSlider("Max branch length", maxBranchLen, 3, 12);

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
                var level = LevelGenerator.Generate(width, height, minColors, maxColors, obstacleCount, rng, maxBranchLen);
                if (level == null)
                {
                    Debug.LogWarning($"Could not generate a solvable {width}x{height} level (try fewer constraints).");
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
                if (Solver.Solve(level) != null) { ok++; continue; }
                Debug.LogWarning($"{Path.GetFileName(file)} (level {level.levelId}): NO solution found");
            }
            Debug.Log($"Validation: {ok}/{files.Length} levels are solvable.");
        }
    }
}
