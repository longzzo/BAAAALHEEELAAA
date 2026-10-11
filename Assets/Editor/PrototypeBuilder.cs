using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valhalla;
using Valhalla.Campaign;
using BodyPart = Valhalla.BodyPart;

public static class PrototypeBuilder
{
    const string LegacyScenePath = "Assets/Scenes/AshenThreshold.unity";
    static readonly (string path, SceneRole role)[] CampaignScenes =
    {
        ("Assets/Scenes/Lobby.unity", SceneRole.Lobby),
        ("Assets/Scenes/MainVillage.unity", SceneRole.Village),
        ("Assets/Scenes/DungeonForest.unity", SceneRole.Forest),
        ("Assets/Scenes/DungeonMine.unity", SceneRole.Mine),
        ("Assets/Scenes/DungeonSanctum.unity", SceneRole.Sanctum),
    };

    [MenuItem("Valhalla/Create prototype scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        BuildScene(LegacyScenePath, SceneRole.Legacy);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LegacyScenePath, true) };
        ConfigurePlayerSettings();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Valhalla/Create campaign")]
    public static void CreateCampaign()
    {
        Directory.CreateDirectory("Assets/Scenes");
        BuildScene(LegacyScenePath, SceneRole.Legacy);
        foreach (var s in CampaignScenes) BuildScene(s.path, s.role);
        var entries = new List<EditorBuildSettingsScene>();
        // Lobby is scene index 0 so the campaign build's default startup scene is the lobby.
        foreach (var s in CampaignScenes) entries.Add(new EditorBuildSettingsScene(s.path, true));
        entries.Add(new EditorBuildSettingsScene(LegacyScenePath, true));
        EditorBuildSettings.scenes = entries.ToArray();
        ConfigurePlayerSettings();
        AssetDatabase.SaveAssets();
    }

    static void BuildScene(string path, SceneRole role)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("Valhalla Prototype").AddComponent<Game>();
        game.sceneRole = role;
        EditorSceneManager.SaveScene(scene, path);
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Barbarian Workshop"; PlayerSettings.productName = "BAAAALHEEELAAA Prototype";
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
    }
    [MenuItem("Valhalla/Validate combat rules")]
    public static void Validate()
    {
        var b = new BodyBuild();
        foreach (TattooPower power in Enum.GetValues(typeof(TattooPower)))
        {
            var tattoo = ScriptableObject.CreateInstance<TattooDefinition>(); tattoo.power = power; tattoo.bodyPart = (power == TattooPower.Ram || power == TattooPower.Frost) ? BodyPart.Legs : BodyPart.RightArm; b.tattoos.Add(tattoo);
        }
        Require(b.Has(TattooPower.Thunder) && b.Has(TattooPower.Blood), "Healthy tattoos");
        b.arm.AddTrauma(29); Require(b.arm.Stage < 2 && b.arm.adaptation == Adaptation.None, "No adaptation before threshold");
        b.arm.AddTrauma(1); Require(b.arm.Stage == 2 && b.arm.adaptation == Adaptation.Fury && b.HeavyDamage == 56, "Crossing 30 trauma applies Fury automatically, no popup");
        b.arm.AddTrauma(10); Require(b.arm.adaptation == Adaptation.Fury, "Adaptation applies once per limb and persists through further trauma");
        b.arm.AddTrauma(15); Require(b.arm.Stage == 3 && !b.Has(TattooPower.Thunder) && !b.Has(TattooPower.Blood), "All arm tattoos disabled at severe injury");
        Require(b.Has(TattooPower.Ram), "Other part independent");
        Require(b.RecoveryMultiplier == 1.9f, "Severe penalty overrides mitigation");
        Require(b.HeavyDamage > 35f && b.HeavyDamage < 56f, "Fury persists as risk/reward through severe injury");
        b.legs.AddTrauma(55);
        Require(!b.Has(TattooPower.Ram) && b.DashSpeed == 10 && b.MoveMultiplier == .48f && b.legs.adaptation == Adaptation.Fury, "Leg severity overrides dash speed but still auto-adapts independently");
        b.Reset(); Require(b.arm.Stage == 0 && b.legs.Stage == 0 && b.arm.adaptation == Adaptation.None && b.Has(TattooPower.Blood), "Run reset clears injury and adaptation");
        b.arm.AddTrauma(56); Require(b.arm.Stage == 3 && b.arm.adaptation == Adaptation.Fury && !b.arm.TattoosEnabled, "Large hit crosses directly to severe while still auto-adapting");
        Require(!b.arm.NeedsChoice, "Rules layer never requires a popup/choice");
        Debug.Log("VALHALLA RULE VALIDATION: 12 checks passed.");
    }
    static void Require(bool condition, string name) { if (!condition) throw new Exception("Rule check failed: " + name); }
    public static void Build()
    {
        Validate(); CreateCampaign();
        string output = Path.GetFullPath("../WindowsBuild/Valhalla.exe"); Directory.CreateDirectory(Path.GetDirectoryName(output));
        var scenes = new string[CampaignScenes.Length + 1];
        for (int i = 0; i < CampaignScenes.Length; i++) scenes[i] = CampaignScenes[i].path;
        scenes[CampaignScenes.Length] = LegacyScenePath;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        Debug.Log("VALHALLA BUILD SUCCESS: " + output);
    }
}
