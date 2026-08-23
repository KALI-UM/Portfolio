using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CharacterVisualEffectController : MonoBehaviour
{
    public List<SkinnedMeshRenderer> bodyParts = new();

    [Tooltip("투명 용")] [Range(0, 1f)] public float alpha = 1f;

    [Tooltip("오버레이 용")] 
    [SerializeField] private Color overlayColor = new Color(0.6698113f, 0.6698113f, 0.6698113f, 1f);
    [SerializeField] private Color overlayOutlineColor = new Color(0.1960784f,0.1960784f, 0.1960784f, 1f);
    

    [SerializeField] private float changeSpeed = 1.2f;
    private ECharacter targetCharacterIndex;
    private static readonly int StencilRefId = Shader.PropertyToID("_StencilRef");
    private static readonly int OutlineStencilRefId = Shader.PropertyToID("_OutlineStencilRef");
    private static readonly int[] CharacterStencilIds = { 99, 1, 2, 3, 4, 5, 6 };


    public bool IsTransparent
    {
        get => alpha < 1f;
    }

    public bool IsOverlay { get; private set; }
    private bool isTransparent = false;
    
    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (isTransparent != IsTransparent)
        {
            foreach (var smr in bodyParts)
            {
                SetTransparentMode(smr, IsTransparent);
            }

            isTransparent = IsTransparent;
        }

        if (IsTransparent)
        {
            foreach (var smr in bodyParts)
            {
                SetTransparentValue(smr, alpha);
            }
        }
    }

    private void Awake()
    {
        SetTargetCharacter();
    }

    public void SetTargetCharacter()
    {
        targetCharacterIndex = ECharacter.U;

        string charName = gameObject.name;
        foreach (ECharacter character in System.Enum.GetValues(typeof(ECharacter)))
        {
            if (charName.Contains(character.ToString()))
            {
                targetCharacterIndex = character;
                break;
            }
        }

        ApplyStencilReference();
    }

    private void ApplyStencilReference()
    {
        int characterIndex = (int)targetCharacterIndex;
        if (characterIndex < 0 || characterIndex >= CharacterStencilIds.Length)
            return;

        int stencilReference = CharacterStencilIds[characterIndex];
        foreach (var smr in bodyParts)
        {
            if (smr == null)
                continue;

            foreach (var material in smr.materials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty(StencilRefId))
                    material.SetInt(StencilRefId, stencilReference);

                if (material.HasProperty(OutlineStencilRefId))
                    material.SetInt(OutlineStencilRefId, stencilReference);
            }
        }
    }

    public void SetTransparencyValueInstant(float _value)
    {
        _value = Mathf.Clamp01(_value);
        foreach (var mat in bodyParts)
        {
            SetTransparentMode(mat, _value<0.999f);
        }
        
        foreach (var mat in bodyParts)
        {
            SetTransparentValue(mat, _value);
        }
    }

    [ContextMenu("Fade To Transparent")]
    public void FadeToTransparent()
    {
        StartCoroutine(FadeAlpha(1f, 0f));
    }

    [ContextMenu("Fade To Opaque")]
    public void FadeToOpaque()
    {
        StartCoroutine(FadeAlpha(0f, 1f));
    }

    private IEnumerator FadeAlpha(float _startAlpha, float _targetAlpha)
    {
        foreach (var smr in bodyParts)
        {
            SetTransparentMode(smr, true);
        }

        float startTime = Time.time;
        float duration = 1f / changeSpeed;
        float normalizedTime = 0f;

        while (normalizedTime < 1f)
        {
            normalizedTime = Mathf.Clamp01((Time.time - startTime) / duration);
            alpha = Mathf.Lerp(_startAlpha, _targetAlpha, normalizedTime);

            foreach (var smr in bodyParts)
            {
                SetTransparentValue(smr, alpha);
            }

            yield return null;
        }

        alpha = _targetAlpha;
        SetTransparencyValueInstant(alpha);
    }

    [ContextMenu("Set Overlay Color")]
    public void SetOverlayColorInstant()
    {
        IsOverlay = true;
        foreach (var smr in bodyParts)
        {
            LilToonShaderHelper.SetOverlayColor(smr, (int)targetCharacterIndex,overlayColor);
        }
    }
    
    [ContextMenu("Set Default Color")]
    public void SetDefaultColorInstant()
    {
        IsOverlay = false;
        Color defaultColor = overlayColor;
        defaultColor.a = 0f;
        foreach (var smr in bodyParts)
        {
            LilToonShaderHelper.SetOverlayColor(smr, (int)targetCharacterIndex, defaultColor);
        }
    }

    [ContextMenu("Fade To Overlay Color")]
    public void FadeToOverlayColor()
    {
        StartCoroutine(SetOverlayColor());
    }

    [ContextMenu("Fade To Default Color")]
    public void FadeToDefaultColor()
    {
        StartCoroutine(SetDefaultColor());
    }
    
    public IEnumerator SetOverlayColor()
    {
        IsOverlay = true;

        float startTime = Time.time;
        float duration = 1f / changeSpeed; // changeSpeed가 빠를수록 duration이 짧아집니다.
        float normalizedTime = 0f;

        while (normalizedTime < 1f)
        {
            normalizedTime = (Time.time - startTime) / duration;
            if (normalizedTime > 1f) normalizedTime = 1f;
            Color prevColor = overlayColor;
            prevColor.a = 0f;
            // 설정된 오버레이 색상으로 보간
            foreach (var smr in bodyParts)
            {
                Color currentColor = Color.Lerp(prevColor, overlayColor, normalizedTime);
                LilToonShaderHelper.SetOverlayColor(smr, (int)targetCharacterIndex,currentColor);
            }

            yield return null;
        }

        //2025-12-11 KHJ : 정확한 값으로 보정
        SetOverlayColorInstant();
        yield break;
    }

    public IEnumerator SetDefaultColor()
    {
        IsOverlay = false;
        
        float startTime = Time.time;
        float duration = 1f / changeSpeed; // changeSpeed가 빠를수록 duration이 짧아집니다.
        float normalizedTime = 0f;

        while (normalizedTime < 1f)
        {
            normalizedTime = (Time.time - startTime) / duration;
            if (normalizedTime > 1f) normalizedTime = 1f;

            // 설정된 오버레이 색상으로 보간
            Color prevColor = overlayColor;
            prevColor.a = 0f;
            foreach (var smr in bodyParts)
            {
                Color currentColor = Color.Lerp(overlayColor, prevColor, normalizedTime);
                LilToonShaderHelper.SetOverlayColor(smr, (int)targetCharacterIndex,currentColor);
            }

            yield return null;
        }

        //2025-12-11 KHJ : 정확한 값으로 보정
        SetDefaultColorInstant();

        yield break;
    }

    private void SetTransparentMode(SkinnedMeshRenderer _smr, bool _on)
    {
        var materials = _smr.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            LilToonShaderHelper.SetTransparentMode(mat, _on);
        }
    }

    private void SetTransparentValue(SkinnedMeshRenderer _smr, float _value)
    {
        var materials = _smr.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            LilToonShaderHelper.SetTranparentValue(mat, _value);
        }

        //2025-12-18 KHJ : 오버레이 올린채로 투명해지면 오버레이 부분이 남아버려 같이 투명화 해줌 
        LilToonShaderHelper.SetOverlayAlpha(_smr, (int)targetCharacterIndex,_value);
    }

    private void OnDisable()
    {
        SetDefaultColorInstant();
    }
}
