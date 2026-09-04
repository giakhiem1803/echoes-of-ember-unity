#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-time authoring utility for the CraftPix Knight_1 sheets used by Kael.
/// It creates sliced sprites, animation clips, an animator controller and a prefab.
/// </summary>
public static class KaelAnimationBuilder
{
    private const string SourceFolder = "Assets/Sprites/Characters/Knights/Knight_1";
    private const string OutputFolder = "Assets/Animations/Characters/Kael";
    private const string PrefabPath = "Assets/Prefabs/Characters/Player_Kael.prefab";
    private const int FrameSize = 128;

    private static readonly AnimationSpec[] Specs =
    {
        new("Idle", "Idle", 6f, true),
        new("Walk", "Walk", 10f, true),
        new("Run", "Run", 12f, true),
        new("Jump", "Jump", 10f, false),
        new("Attack 1", "Attack1", 12f, false),
        new("Attack 2", "Attack2", 12f, false),
        new("Attack 3", "Attack3", 12f, false),
        new("Defend", "Defend", 8f, true),
        new("Protect", "Protect", 8f, true),
        new("Hurt", "Hurt", 10f, false),
        new("Dead", "Dead", 8f, false)
    };

    [MenuItem("Echoes of Ember/Setup Kael (Knight 1)", priority = 1)]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            EditorUtility.DisplayDialog("Kael setup", "Knight_1 sprites were not found. Import the CraftPix asset first.", "OK");
            return;
        }

        EnsureFolder(OutputFolder);
        EnsureFolder("Assets/Prefabs/Characters");

        foreach (AnimationSpec spec in Specs)
            ConfigureSheet(spec.SourceName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var clips = new Dictionary<string, AnimationClip>();
        foreach (AnimationSpec spec in Specs)
            clips[spec.ClipName] = CreateClip(spec);

        AnimatorController controller = CreateController(clips);
        CreatePrefab(controller, GetFirstSprite("Idle"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("Echoes of Ember: Kael Knight_1 setup completed.");
    }

    private static void ConfigureSheet(string sourceName)
    {
        string assetPath = $"{SourceFolder}/{sourceName}.png";
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = FrameSize;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

#pragma warning disable CS0618
        Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        int frameCount = sourceTexture == null ? 1 : Mathf.Max(1, Mathf.RoundToInt(sourceTexture.width / (float)FrameSize));
        var metadata = new SpriteMetaData[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            metadata[i] = new SpriteMetaData
            {
                name = $"{sourceName}_{i:00}",
                rect = new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(.5f, 0f)
            };
        }
        importer.spritesheet = metadata;
#pragma warning restore CS0618
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static AnimationClip CreateClip(AnimationSpec spec)
    {
        string path = $"{OutputFolder}/Kael_{spec.ClipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = spec.FrameRate;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = spec.Loops;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        Sprite[] sprites = LoadSprites(spec.SourceName);
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / spec.FrameRate, value = sprites[i] };

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        string path = $"{OutputFolder}/Kael_Controller.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) AssetDatabase.DeleteAsset(path);

        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsDefending", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        var states = new Dictionary<string, AnimatorState>();
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            AnimatorState state = machine.AddState(pair.Key);
            state.motion = pair.Value;
            states[pair.Key] = state;
        }
        machine.defaultState = states["Idle"];

        AddTransition(states["Idle"], states["Walk"], t => t.AddCondition(AnimatorConditionMode.Greater, .1f, "Speed"));
        AddTransition(states["Walk"], states["Idle"], t => t.AddCondition(AnimatorConditionMode.Less, .1f, "Speed"));
        AddTransition(states["Walk"], states["Run"], t => t.AddCondition(AnimatorConditionMode.Greater, .7f, "Speed"));
        AddTransition(states["Run"], states["Walk"], t => t.AddCondition(AnimatorConditionMode.Less, .7f, "Speed"));

        foreach (string locomotion in new[] { "Idle", "Walk", "Run" })
            AddTransition(states[locomotion], states["Jump"], t => t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded"));
        AddTransition(states["Jump"], states["Idle"], t => { t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded"); t.AddCondition(AnimatorConditionMode.Less, .1f, "Speed"); });
        AddTransition(states["Jump"], states["Run"], t => { t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded"); t.AddCondition(AnimatorConditionMode.Greater, .1f, "Speed"); });

        AddAnyStateTransition(machine, states["Attack1"], t => t.AddCondition(AnimatorConditionMode.Equals, 1f, "AttackIndex"));
        AddAnyStateTransition(machine, states["Attack2"], t => t.AddCondition(AnimatorConditionMode.Equals, 2f, "AttackIndex"));
        AddAnyStateTransition(machine, states["Attack3"], t => t.AddCondition(AnimatorConditionMode.Equals, 3f, "AttackIndex"));
        foreach (string attack in new[] { "Attack1", "Attack2", "Attack3" })
            AddTransition(states[attack], states["Idle"], null, .9f);

        AddAnyStateTransition(machine, states["Defend"], t => t.AddCondition(AnimatorConditionMode.If, 0f, "IsDefending"));
        AddTransition(states["Defend"], states["Idle"], t => t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDefending"));
        AddAnyStateTransition(machine, states["Hurt"], t => t.AddCondition(AnimatorConditionMode.If, 0f, "Hurt"));
        AddTransition(states["Hurt"], states["Idle"], null, .9f);
        AddAnyStateTransition(machine, states["Dead"], t => t.AddCondition(AnimatorConditionMode.If, 0f, "Dead"), false);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void CreatePrefab(AnimatorController controller, Sprite idleSprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) AssetDatabase.DeleteAsset(PrefabPath);

        var root = new GameObject("Player_Kael");
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = idleSprite;
        renderer.sortingOrder = 10;
        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        var body = root.AddComponent<Rigidbody2D>();
        body.freezeRotation = true;
        body.gravityScale = 3f;
        var collider = root.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(.55f, .9f);
        collider.offset = new Vector2(0f, .45f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, Action<AnimatorStateTransition> configure, float exitTime = 0f)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = exitTime > 0f;
        transition.exitTime = exitTime;
        transition.duration = .06f;
        configure?.Invoke(transition);
    }

    private static void AddAnyStateTransition(AnimatorStateMachine machine, AnimatorState to, Action<AnimatorStateTransition> configure, bool canTransitionToSelf = false)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.canTransitionToSelf = canTransitionToSelf;
        transition.duration = .03f;
        configure(transition);
    }

    private static Sprite[] LoadSprites(string sourceName)
    {
        string path = $"{SourceFolder}/{sourceName}.png";
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
    }

    private static Sprite GetFirstSprite(string sourceName)
    {
        Sprite[] sprites = LoadSprites(sourceName);
        if (sprites.Length == 0) throw new InvalidOperationException($"No sprites found for {sourceName}.");
        return sprites[0];
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private readonly struct AnimationSpec
    {
        public readonly string SourceName;
        public readonly string ClipName;
        public readonly float FrameRate;
        public readonly bool Loops;
        public AnimationSpec(string sourceName, string clipName, float frameRate, bool loops)
        {
            SourceName = sourceName;
            ClipName = clipName;
            FrameRate = frameRate;
            Loops = loops;
        }
    }
}
#endif
