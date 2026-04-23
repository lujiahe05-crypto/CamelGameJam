using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class GameJamAnimatorSetup
{
    const string ControllerPath = "Assets/Games/GameJam/assets/gamemodules/animation/animator/Anim_Medium_Oaks.controller";
    const string ClipFolder = "Assets/Games/GameJam/assets/AnimationClip/";

    [MenuItem("GameJam/Setup Animator")]
    public static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[AnimatorSetup] Controller not found: {ControllerPath}");
            return;
        }

        var rootSM = controller.layers[0].stateMachine;

        AddParamIfMissing(controller, "Saw", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(controller, "Dig", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(controller, "Sow", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(controller, "Drill", AnimatorControllerParameterType.Trigger);
        AddParamIfMissing(controller, "IsWorkingLoop", AnimatorControllerParameterType.Bool);

        BuildSequence(rootSM, "CutTree_Seq", "Cuttree",
            "CutTreeStart_0", "CutTreeLoop_0", "CutTreeEnd_0", "CutTreeStand_0",
            new Vector3(200, 200, 0));

        BuildSequence(rootSM, "Stone_Seq", "Stone",
            "StoneStart_0", "StoneLoop_0", "StoneEnd_0", "StoneStand_0",
            new Vector3(200, 300, 0));

        BuildSequence(rootSM, "Saw_Seq", "Saw",
            "SawStart_0", "SawLoop_0", "SawEnd_0", "SawStand_0",
            new Vector3(200, 400, 0));

        BuildOneShot(rootSM, "Drilling_0", "Drill", new Vector3(400, 200, 0));
        BuildOneShot(rootSM, "Digging", "Dig", new Vector3(400, 300, 0));
        BuildOneShot(rootSM, "Sow_0", "Sow", new Vector3(400, 400, 0));

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[AnimatorSetup] Animator configured successfully.");
    }

    static void AddParamIfMissing(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        if (ctrl.parameters.Any(p => p.name == name))
            return;
        ctrl.AddParameter(name, type);
    }

    static void BuildSequence(AnimatorStateMachine rootSM, string seqName, string triggerParam,
        string startClip, string loopClip, string endClip, string standClip, Vector3 position)
    {
        if (rootSM.stateMachines.Any(sm => sm.stateMachine.name == seqName))
        {
            Debug.Log($"[AnimatorSetup] Sub-state machine '{seqName}' already exists, skipping.");
            return;
        }

        var subSM = rootSM.AddStateMachine(seqName, position);

        var startState = subSM.AddState(startClip, new Vector3(250, 0, 0));
        startState.motion = LoadClip(startClip);

        var loopState = subSM.AddState(loopClip, new Vector3(500, 0, 0));
        loopState.motion = LoadClip(loopClip);

        var endState = subSM.AddState(endClip, new Vector3(750, 0, 0));
        endState.motion = LoadClip(endClip);

        var standState = subSM.AddState(standClip, new Vector3(1000, 0, 0));
        standState.motion = LoadClip(standClip);

        subSM.defaultState = startState;

        // Start -> Loop (exit time)
        var t1 = startState.AddTransition(loopState);
        t1.hasExitTime = true;
        t1.exitTime = 0.9f;
        t1.duration = 0.1f;
        t1.hasFixedDuration = true;

        // Loop -> Loop (self, while IsWorkingLoop == true)
        var tLoop = loopState.AddTransition(loopState);
        tLoop.hasExitTime = true;
        tLoop.exitTime = 0.95f;
        tLoop.duration = 0.05f;
        tLoop.hasFixedDuration = true;
        tLoop.AddCondition(AnimatorConditionMode.If, 0, "IsWorkingLoop");

        // Loop -> End (when IsWorkingLoop == false)
        var t2 = loopState.AddTransition(endState);
        t2.hasExitTime = true;
        t2.exitTime = 0.95f;
        t2.duration = 0.1f;
        t2.hasFixedDuration = true;
        t2.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWorkingLoop");

        // End -> Stand (exit time)
        var t3 = endState.AddTransition(standState);
        t3.hasExitTime = true;
        t3.exitTime = 0.9f;
        t3.duration = 0.15f;
        t3.hasFixedDuration = true;

        // Stand -> Exit (exit time)
        var tExit = standState.AddExitTransition();
        tExit.hasExitTime = true;
        tExit.exitTime = 0.9f;
        tExit.duration = 0.2f;
        tExit.hasFixedDuration = true;

        // AnyState -> subSM default state (trigger)
        var anyT = rootSM.AddAnyStateTransition(startState);
        anyT.hasExitTime = false;
        anyT.duration = 0.15f;
        anyT.hasFixedDuration = true;
        anyT.canTransitionToSelf = false;
        anyT.AddCondition(AnimatorConditionMode.If, 0, triggerParam);
    }

    static void BuildOneShot(AnimatorStateMachine rootSM, string clipName, string triggerParam, Vector3 position)
    {
        if (rootSM.states.Any(s => s.state.name == clipName))
        {
            Debug.Log($"[AnimatorSetup] State '{clipName}' already exists, skipping.");
            return;
        }

        var state = rootSM.AddState(clipName, position);
        state.motion = LoadClip(clipName);

        // Exit via exit time back to default
        var tExit = state.AddExitTransition();
        tExit.hasExitTime = true;
        tExit.exitTime = 0.9f;
        tExit.duration = 0.2f;
        tExit.hasFixedDuration = true;

        // AnyState -> state (trigger)
        var anyT = rootSM.AddAnyStateTransition(state);
        anyT.hasExitTime = false;
        anyT.duration = 0.15f;
        anyT.hasFixedDuration = true;
        anyT.canTransitionToSelf = false;
        anyT.AddCondition(AnimatorConditionMode.If, 0, triggerParam);
    }

    static AnimationClip LoadClip(string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + clipName + ".anim");
        if (clip == null)
            Debug.LogWarning($"[AnimatorSetup] Clip not found: {ClipFolder}{clipName}.anim");
        return clip;
    }
}
