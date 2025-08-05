#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ScriptRenamer : EditorWindow
{
    private string oldScriptName = "PlayerController";
    private string newScriptName = "PlayerController";
    private bool includeComments = false;
    private bool testMode = true;
    private Vector2 scrollPosition;
    private List<RenameResult> previewResults = new List<RenameResult>();
    
    [MenuItem("Tools/Script Renamer")]
    public static void ShowWindow()
    {
        GetWindow<ScriptRenamer>("Script Renamer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Unity Script Renamer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        oldScriptName = EditorGUILayout.TextField("Old Script Name:", oldScriptName);
        newScriptName = EditorGUILayout.TextField("New Script Name:", newScriptName);
        
        GUILayout.Space(10);
        includeComments = EditorGUILayout.Toggle("Include Comments", includeComments);
        testMode = EditorGUILayout.Toggle("Test Mode (Preview Only)", testMode);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Preview Changes"))
        {
            PreviewChanges();
        }
        
        if (!testMode && previewResults.Count > 0)
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("APPLY CHANGES (PERMANENT)"))
            {
                ApplyChanges();
            }
            GUI.backgroundColor = Color.white;
        }
        
        if (previewResults.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Found {previewResults.Count} files with changes:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var result in previewResults)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"File: {result.FilePath}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Changes: {result.Changes.Count}");
                
                foreach (var change in result.Changes)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Line {change.LineNumber}:", GUILayout.Width(60));
                    GUILayout.Label($"{change.OldText} -> {change.NewText}");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
            EditorGUILayout.EndScrollView();
        }
    }
    
    private void PreviewChanges()
    {
        previewResults.Clear();
        
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (string filePath in scriptFiles)
        {
            var result = ProcessFile(filePath, true);
            if (result.Changes.Count > 0)
            {
                previewResults.Add(result);
            }
        }
        
        Debug.Log($"Preview complete. Found {previewResults.Count} files that would be modified.");
    }
    
    private void ApplyChanges()
    {
        if (EditorUtility.DisplayDialog("Confirm Changes", 
            $"This will permanently modify {previewResults.Count} files. Make sure you have a backup!", 
            "Apply", "Cancel"))
        {
            foreach (var result in previewResults)
            {
                ProcessFile(result.FilePath, false);
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"Applied changes to {previewResults.Count} files.");
            previewResults.Clear();
        }
    }
    
    private RenameResult ProcessFile(string filePath, bool previewOnly)
    {
        var result = new RenameResult { FilePath = filePath };
        
        string[] lines = File.ReadAllLines(filePath);
        List<string> newLines = new List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string newLine = ProcessLine(line, i + 1, result);
            newLines.Add(newLine);
        }
        
        if (!previewOnly && result.Changes.Count > 0)
        {
            File.WriteAllLines(filePath, newLines);
        }
        
        return result;
    }
    
    private string ProcessLine(string line, int lineNumber, RenameResult result)
    {
        string originalLine = line;
        
        // Skip if it's a comment and we don't want to include comments
        if (!includeComments && (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*")))
        {
            return line;
        }
        
        // Define all the patterns we want to replace
        var patterns = new List<ReplacePattern>
        {
            // Class declarations
            new ReplacePattern($@"\bclass\s+{oldScriptName}\b", $"class {newScriptName}"),
            
            // Variable declarations (exact match)
            new ReplacePattern($@"\b{oldScriptName}\b", newScriptName),
            
            // Camel case versions
            new ReplacePattern($@"\b{ToCamelCase(oldScriptName)}\b", ToCamelCase(newScriptName)),
            
            // List/Array declarations
            new ReplacePattern($@"\bList<{oldScriptName}>\b", $"List<{newScriptName}>"),
            new ReplacePattern($@"\b{oldScriptName}\[\]", $"{newScriptName}[]"),
            
            // Generic collections
            new ReplacePattern($@"\bDictionary<(.+?),\s*{oldScriptName}>\b", $"Dictionary<$1, {newScriptName}>"),
            new ReplacePattern($@"\bDictionary<{oldScriptName},\s*(.+?)>\b", $"Dictionary<{newScriptName}, $1>"),
            
            // Method parameters and return types
            new ReplacePattern($@"\bpublic\s+{oldScriptName}\b", $"public {newScriptName}"),
            new ReplacePattern($@"\bprivate\s+{oldScriptName}\b", $"private {newScriptName}"),
            new ReplacePattern($@"\bprotected\s+{oldScriptName}\b", $"protected {newScriptName}"),
            new ReplacePattern($@"\binternal\s+{oldScriptName}\b", $"internal {newScriptName}"),
            
            // Plural versions
            new ReplacePattern($@"\b{oldScriptName}s\b", $"{newScriptName}s"),
            new ReplacePattern($@"\b{ToCamelCase(oldScriptName)}s\b", $"{ToCamelCase(newScriptName)}s"),
        };
        
        foreach (var pattern in patterns)
        {
            var regex = new Regex(pattern.Pattern);
            if (regex.IsMatch(line))
            {
                string newLine = regex.Replace(line, pattern.Replacement);
                if (newLine != line)
                {
                    result.Changes.Add(new LineChange
                    {
                        LineNumber = lineNumber,
                        OldText = line.Trim(),
                        NewText = newLine.Trim()
                    });
                    line = newLine;
                }
            }
        }
        
        return line;
    }
    
    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}

[System.Serializable]
public class RenameResult
{
    public string FilePath;
    public List<LineChange> Changes = new List<LineChange>();
}

[System.Serializable]
public class LineChange
{
    public int LineNumber;
    public string OldText;
    public string NewText;
}

[System.Serializable]
public class ReplacePattern
{
    public string Pattern;
    public string Replacement;
    
    public ReplacePattern(string pattern, string replacement)
    {
        Pattern = pattern;
        Replacement = replacement;
    }
}
#endif
