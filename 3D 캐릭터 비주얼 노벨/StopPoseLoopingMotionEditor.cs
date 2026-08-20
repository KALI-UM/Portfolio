using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StopPoseLoopingMotion))]
public class StopPoseLoopingMotionEditor : Editor
{
    private Animator previewAnimator;
    private AnimationClip previewClip;

    SerializedProperty loopPoseOffsetProp;
    SerializedProperty motionIntProp;
    SerializedProperty motionTypeParamProp;
    SerializedProperty motionSpeedParamProp;
    SerializedProperty addictiveLayerParamProp;
    SerializedProperty maxWeightParamProp;

    private bool isPreviewing = false;

    private void OnEnable()
    {
        if (target == null) 
            return;
        
        
        loopPoseOffsetProp = serializedObject.FindProperty("loopPoseOffset");
        motionIntProp = serializedObject.FindProperty("motionInt");
        motionTypeParamProp = serializedObject.FindProperty("motionTypeParam");
        motionSpeedParamProp = serializedObject.FindProperty("motionSpeedParam");
        addictiveLayerParamProp= serializedObject.FindProperty("addictiveLayer");
        maxWeightParamProp= serializedObject.FindProperty("maxWeight");
    }

    private void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. 기본 변수들 그리기
        EditorGUILayout.PropertyField(motionIntProp);
        EditorGUILayout.PropertyField(motionTypeParamProp);
        EditorGUILayout.PropertyField(motionSpeedParamProp);
        EditorGUILayout.PropertyField(addictiveLayerParamProp);
        EditorGUILayout.PropertyField(maxWeightParamProp);

        // ---------------------------------------------------------
        // 여기서부터 미리보기 기능 UI
        // ---------------------------------------------------------
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Pose Preview (Editor Only)", EditorStyles.boldLabel);

        previewAnimator = (Animator)EditorGUILayout.ObjectField("Preview Target (Scene)", previewAnimator, typeof(Animator), true);
        previewClip = (AnimationClip)EditorGUILayout.ObjectField("Preview Clip", previewClip, typeof(AnimationClip), false);

        if (previewAnimator != null && previewClip != null)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            if (isPreviewing)
            {
                buttonStyle.normal.textColor = Color.green;
                buttonStyle.fontStyle = FontStyle.Bold;
            }

            if (GUILayout.Button(isPreviewing ? "Stop Preview" : "Start Preview Mode", buttonStyle))
            {
                if (isPreviewing) StopPreview();
                else StartPreview();
            }

            if (isPreviewing)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("슬라이더를 움직여 실시간으로 확인하세요.", MessageType.Info);

                // [핵심] 변경 감지 시작
                EditorGUI.BeginChangeCheck();

                // 슬라이더 그리기
                EditorGUILayout.Slider(loopPoseOffsetProp, 0f, 1f, new GUIContent("Loop Pose Offset"));

                // [핵심] 값이 조금이라도 변했다면?
                if (EditorGUI.EndChangeCheck())
                {
                    // 1. 데이터 저장
                    serializedObject.ApplyModifiedProperties();
                    
                    // 2. 포즈 즉시 적용
                    SamplePose();
                    
                    // 3. [중요] 씬 뷰 강제 갱신 (이게 없으면 드래그 중에 안 움직임)
                    SceneView.RepaintAll();
                }
            }
            else
            {
                // 미리보기 아닐 때는 그냥 기본 슬라이더만 보여줌
                EditorGUILayout.PropertyField(loopPoseOffsetProp);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(loopPoseOffsetProp);
            EditorGUILayout.HelpBox("씬의 캐릭터와 클립을 넣으면 미리보기가 가능합니다.", MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void StartPreview()
    {
        if (previewAnimator == null || previewClip == null) return;

        AnimationMode.StartAnimationMode();
        isPreviewing = true;
        SamplePose();
    }

    private void StopPreview()
    {
        AnimationMode.StopAnimationMode();
        isPreviewing = false;
    }

    private void SamplePose()
    {
        if (previewAnimator == null || previewClip == null) return;

        float normalizedTime = loopPoseOffsetProp.floatValue;
        float clipTime = normalizedTime * previewClip.length;

        AnimationMode.SampleAnimationClip(previewAnimator.gameObject, previewClip, clipTime);
    }
}