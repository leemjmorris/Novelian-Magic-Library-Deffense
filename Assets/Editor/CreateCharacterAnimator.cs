using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateCharacterAnimator
{
    [MenuItem("Tools/Create Character Animator Controller")]
    public static void CreateAnimatorController()
    {
        // 애니메이터 컨트롤러 생성
        var controller = AnimatorController.CreateAnimatorControllerAtPath(
            "Assets/Animations/CharacterAnimatorController.controller");

        // 파라미터 추가
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Victory", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackSpeed", AnimatorControllerParameterType.Float);

        // 레이어 가져오기
        var rootStateMachine = controller.layers[0].stateMachine;

        // 애니메이션 클립 로드
        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Layer Lab/3D CharactersCasual/Animation/Anim@Stand_Idle1.FBX");
        var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Layer Lab/3D CharactersCasual/Animation/Anim@Action_Punch.FBX");
        var dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Layer Lab/3D CharactersCasual/Animation/Anim@Reaction_Knockout.FBX");
        var victoryClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Layer Lab/3D CharactersCasual/Animation/Anim@Dance_1.FBX");

        // State 생성
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        var attackState = rootStateMachine.AddState("Attack", new Vector3(550, 100, 0));
        var dieState = rootStateMachine.AddState("Die", new Vector3(300, 250, 0));
        var victoryState = rootStateMachine.AddState("Victory", new Vector3(550, 250, 0));

        // 클립 할당
        if (idleClip != null) idleState.motion = idleClip;
        if (attackClip != null) attackState.motion = attackClip;
        if (dieClip != null) dieState.motion = dieClip;
        if (victoryClip != null) victoryState.motion = victoryClip;

        // 기본 State 설정
        rootStateMachine.defaultState = idleState;

        // Transition: Idle -> Attack (Attack trigger)
        var idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        idleToAttack.hasExitTime = false;
        idleToAttack.duration = 0.1f;

        // Transition: Attack -> Idle (자동 복귀)
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        // Transition: Any State -> Die (Die trigger)
        var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.1f;

        // Transition: Any State -> Victory (Victory trigger)
        var anyToVictory = rootStateMachine.AddAnyStateTransition(victoryState);
        anyToVictory.AddCondition(AnimatorConditionMode.If, 0, "Victory");
        anyToVictory.hasExitTime = false;
        anyToVictory.duration = 0.1f;

        // Attack 속도 조절용 - AttackSpeed 파라미터 연결
        attackState.speedParameterActive = true;
        attackState.speedParameter = "AttackSpeed";

        // 저장
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateCharacterAnimator] CharacterAnimatorController created successfully!");
        Selection.activeObject = controller;
    }
}
