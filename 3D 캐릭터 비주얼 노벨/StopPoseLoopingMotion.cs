using UnityEngine;

public class StopPoseLoopingMotion : StateMachineBehaviour
{
    [SerializeField][Range(0f,1f)] private float loopPoseOffset; // 멈출 지점 (0~1)
    [SerializeField] private int motionInt; // 이 값과 BodyInt가 다르면 재개
    [SerializeField] private string motionTypeParam = "FaceInt"; 
    [SerializeField] private string motionSpeedParam = "FaceSpeed";
    
    [SerializeField] private string addictiveLayer = "Blink";
    [SerializeField] private float maxWeight=0.15f;

    private int motionTypeHash;
    private int motionSpeedHash;
    private int addictiveLayerIndex;
    private int addictiveLayerTriggerHash;
    
    private bool isPausing = false;
    private bool isPaused = false;
    private float t = 0f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        motionTypeHash = Animator.StringToHash(motionTypeParam);
        motionSpeedHash = Animator.StringToHash(motionSpeedParam);
        addictiveLayerIndex = animator.GetLayerIndex(addictiveLayer);
        addictiveLayerTriggerHash = Animator.StringToHash(addictiveLayer+"Restart");
        
        // 진입 시 초기화
        isPaused = false;
        isPausing = false;
        t = 0f;
        
        loopPoseOffset %= 1f;
        animator.SetFloat(motionSpeedHash, 1f);
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        bool shoudPause = animator.GetInteger(motionTypeHash) == motionInt;
        
        if (!isPausing && shoudPause && loopPoseOffset <= stateInfo.normalizedTime)
        {
           // Debug.Log($"Stop Pose Looping Motion{stateInfo.fullPathHash} [{animator.GetInteger(motionTypeHash)} {motionInt} , curr = {stateInfo.normalizedTime}]");
            isPaused = true;
            isPausing = true;
            
            //2026-01-15 KHJ : 정지
            animator.SetFloat(motionSpeedHash, 0f);
            
            //2026-01-14 KHJ : addictiveLayer를 블랜딩해서 멈춰있지 않은것처럼 보이게 연출
            //2026-01-15 KHJ : 자연스럽게 시작되기 위해서 addictiveLayer 아래에서 Lerp 계산할 것
            //animator.SetLayerWeight(addictiveLayerIndex, maxWeight);
            animator.SetTrigger(addictiveLayerTriggerHash);
            t = 0f;

            //2026-01-14 KHJ : 정확히 맞추기 위해서 Play 시켜버리면 OnEnter가 다시 들어와서 무한 반복당함 (참고)
            //animator.Play(stateInfo.fullPathHash, layerIndex, loopPoseOffset);
        }
        else if (isPausing && !shoudPause)
        {
            //Debug.Log($"Play Pose Looping Motion{stateInfo.fullPathHash} [{animator.GetInteger(motionTypeHash)} {motionInt} , curr = {stateInfo.normalizedTime}]");
            isPausing = false;
            
            //2026-01-15 KHJ : 재시작
            animator.SetFloat(motionSpeedHash, 1f);
            animator.SetLayerWeight(addictiveLayerIndex, 0f);
        }

        if (isPausing)
        {
            t += Time.deltaTime * 5f;
            t = Mathf.Clamp01(t);
            float nextWeight = MathfEx.Lerp(0f, maxWeight, t, MathfEx.EaseType.OutExpo);
            animator.SetLayerWeight(addictiveLayerIndex, nextWeight);
        }
    }

    public float GetLoopPoseOffset() 
    {
        return loopPoseOffset;
    }
}