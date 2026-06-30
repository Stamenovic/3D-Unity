using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class SlideAnimationCleaner
{
    private const string SourceClipPath = "Assets/_Game/Animations/Ch48_nonPBR@Running Slide.fbx";
    private const string CleanClipPath = "Assets/_Game/Animations/RunningSlide_InPlace.anim";
    private const string ControllerPath = "Assets/_Game/Animations/PlayerAnimator.controller";
    private const string SlideStateName = "Slide";

    static SlideAnimationCleaner()
    {
        EditorApplication.delayCall += EnsureCleanSlideClip;
    }

    [MenuItem("Tools/Animation/Fix Running Slide In Place")]
    public static void EnsureCleanSlideClip()
    {
        AnimationClip sourceClip = LoadSourceClip();
        if (sourceClip == null)
        {
            Debug.LogWarning("Running Slide source clip was not found.");
            return;
        }

        AnimationClip cleanClip = CreateCleanClip(sourceClip);
        SaveCleanClip(cleanClip);
        AssignSlideState();
    }

    private static AnimationClip LoadSourceClip()
    {
        return AssetDatabase.LoadAllAssetsAtPath(SourceClipPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == SlideStateName) ??
            AssetDatabase.LoadAllAssetsAtPath(SourceClipPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal));
    }

    private static AnimationClip CreateCleanClip(AnimationClip sourceClip)
    {
        AnimationClip cleanClip = Object.Instantiate(sourceClip);
        cleanClip.name = "RunningSlide_InPlace";
        cleanClip.wrapMode = WrapMode.Once;

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(cleanClip, settings);

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(sourceClip))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (curve == null)
            {
                continue;
            }

            if (ShouldLockHorizontalCurve(binding))
            {
                curve = MakeConstantCurve(curve);
            }

            AnimationUtility.SetEditorCurve(cleanClip, binding, curve);
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
            AnimationUtility.SetObjectReferenceCurve(cleanClip, binding, keys);
        }

        return cleanClip;
    }

    private static bool ShouldLockHorizontalCurve(EditorCurveBinding binding)
    {
        string property = binding.propertyName;
        string path = binding.path;

        bool horizontalRoot =
            property == "RootT.x" ||
            property == "RootT.z" ||
            property == "MotionT.x" ||
            property == "MotionT.z" ||
            property == "m_LocalPosition.x" && string.IsNullOrEmpty(path) ||
            property == "m_LocalPosition.z" && string.IsNullOrEmpty(path);

        bool horizontalHips =
            IsHipsPath(path) &&
            (property == "m_LocalPosition.x" || property == "m_LocalPosition.z");

        return horizontalRoot || horizontalHips;
    }

    private static bool IsHipsPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string lastPart = path.Split('/').Last();
        return lastPart == "Hips" || lastPart.EndsWith(":Hips", System.StringComparison.Ordinal);
    }

    private static AnimationCurve MakeConstantCurve(AnimationCurve source)
    {
        if (source.length == 0)
        {
            return source;
        }

        float value = source.keys[0].value;
        Keyframe[] keys = source.keys
            .Select(key => new Keyframe(key.time, value, 0f, 0f))
            .ToArray();
        return new AnimationCurve(keys);
    }

    private static void SaveCleanClip(AnimationClip cleanClip)
    {
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CleanClipPath);
        if (existingClip == null)
        {
            AssetDatabase.CreateAsset(cleanClip, CleanClipPath);
        }
        else
        {
            EditorUtility.CopySerialized(cleanClip, existingClip);
            EditorUtility.SetDirty(existingClip);
        }

        AssetDatabase.SaveAssets();
    }

    private static void AssignSlideState()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        AnimationClip cleanClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CleanClipPath);
        if (controller == null || cleanClip == null)
        {
            return;
        }

        foreach (ChildAnimatorState childState in controller.layers[0].stateMachine.states)
        {
            if (childState.state.name != SlideStateName)
            {
                continue;
            }

            childState.state.motion = cleanClip;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return;
        }
    }
}
