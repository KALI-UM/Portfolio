using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class VisibleChanger : MonoBehaviour
{
    public List<SkinnedMeshRenderer> bodyParts = new();

    [Tooltip("투명 용")] [Range(0, 1f)] public float alpha = 1f;

    [Tooltip("오버레이 용")] 
    [SerializeField] private Color overlayColor = new Color(0.6698113f, 0.6698113f, 0.6698113f, 1f);
    [SerializeField] private Color overlayOutlineColor = new Color(0.1960784f,0.1960784f, 0.1960784f, 1f);
    

    [SerializeField] private float changeSpeed = 1.2f;

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
    
    public IEnumerator SetTransparencyValueMax()
    {
        alpha = 0;
        while (alpha < 1f)
        {
            alpha = Mathf.Clamp01(alpha += 0.05f);
            foreach (var mat in bodyParts)
            {
                SetTransparentValue(mat, alpha);
            }
            yield return null;
        }
        
        foreach (var mat in bodyParts)
        {
            SetTransparentValue(mat, 1f);
        }
        yield break;
    }

    public IEnumerator SetTransparencyValueMin(GameObject _character)
    {
        foreach (var mat in bodyParts)
        {
            SetTransparentMode(mat, true);
        }
        
        alpha = 1;
        while (alpha > 0f)
        {
            alpha -= 0.1f;
            foreach (var mat in bodyParts)
            {
                SetTransparentValue(mat, alpha);
            }
            yield return null;
        }
        _character.SetActive(false);
        yield break;
    }

    public void SetZeroTransparencyValue()
    {
        foreach (var mat in bodyParts)
        {
            SetTransparentMode(mat, true);
        }
        
        foreach (var mat in bodyParts)
        {
            SetTransparentValue(mat, 0);
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
        foreach (var smr in bodyParts)
        {
            SetTransparentValue(smr, alpha);
        }
    }
    

    

    [ContextMenu("Set Overlay Color")]
    public void SetOverlayColorInstant()
    {
        foreach (var smr in bodyParts)
        {
            LilToonShaderHelper.SetOverlayColor(smr, overlayColor);
        }
    }
    
    [ContextMenu("Set Default Color")]
    public void SetDefaultColorInstant()
    {
        Color defaultColor = overlayColor;
        defaultColor.a = 0f;
        foreach (var smr in bodyParts)
        {
            LilToonShaderHelper.SetOverlayColor(smr, defaultColor);
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
                LilToonShaderHelper.SetOverlayColor(smr, currentColor);
            }

            yield return null;
        }

        //2025-12-11 KHJ : 정확한 값으로 보정
        foreach (var smr in bodyParts)
        {
            LilToonShaderHelper.SetOverlayColor(smr, overlayColor);
        }
        //Debug.Log($"걸린 시간 {Time.time - startTime}");
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
                LilToonShaderHelper.SetOverlayColor(smr, currentColor);
            }

            yield return null;
        }

        //2025-12-11 KHJ : 정확한 값으로 보정
        Color defaultColor = overlayColor;
        defaultColor.a = 0f;
        foreach (var smr in bodyParts)
        {
            LilToonShaderHelper.SetOverlayColor(smr, defaultColor);
        }

        //Debug.Log($"걸린 시간 {Time.time - startTime}");
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

        _smr.materials = materials;
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
        LilToonShaderHelper.SetOverlayAlpha(_smr, _value);
        _smr.materials = materials;
    }

}
