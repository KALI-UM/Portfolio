using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

[ExecuteAlways]
public static class LilToonShaderHelper
{
    // 2025.09.19 : KHJ - lilToon 런타임 투명/불투명 전환(키워드/AlphaToMask 정합성 포함)
    static readonly int ID_TMode = Shader.PropertyToID("_TransparentMode");
    static readonly int ID_BlendMode = Shader.PropertyToID("_BlendMode");
    static readonly int ID_SrcBlend = Shader.PropertyToID("_SrcBlend");
    static readonly int ID_DstBlend = Shader.PropertyToID("_DstBlend");
    static readonly int ID_AlphaToMask = Shader.PropertyToID("_AlphaToMask");
    static readonly int ID_ZWrite = Shader.PropertyToID("_ZWrite");
    static readonly int ID_ZTest = Shader.PropertyToID("_ZTest");
    static readonly int ID_Cutoff = Shader.PropertyToID("_Cutoff");
    static readonly int ID_OutLSrcBlend = Shader.PropertyToID("_OutlineSrcBlend");
    static readonly int ID_OutLDstBlend = Shader.PropertyToID("_OutlineDstBlend");
    static readonly int ID_OutLAlphaToMask = Shader.PropertyToID("_OutlineAlphaToMask");

    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_OutlineColor = Shader.PropertyToID("_OutlineColor");
    static readonly int ID_OutlineLitColor = Shader.PropertyToID("_OutlineLitColor");

    static readonly int ID_RimLight = Shader.PropertyToID("_UseRim");
    static readonly int ID_UseMatCap = Shader.PropertyToID("_UseMatCap");
    static readonly int ID_UseEmission = Shader.PropertyToID("_UseEmission");
    static readonly int ID_UseEmission2nd = Shader.PropertyToID("_UseEmission2nd");
    static readonly int ID_EmissionMap = Shader.PropertyToID("_EmissionMap");
    static readonly int ID_Emission2ndMap = Shader.PropertyToID("_Emission2ndMap");

    //2025-12-11 KHJ :커스텀 오버레이 쉐이더용 변수 LiltoonShader에는 없는! 이름을 써야함
    static readonly int ID_OverlayColor = Shader.PropertyToID("_OverlayColor");
    
    #region keyword

    const string KW_ALPHA_TEST = "_ALPHATEST_ON";
    const string KW_ALPHA_BLEND = "_ALPHABLEND_ON";
    const string KW_UI_CLIP_RECT = "UNITY_UI_CLIP_RECT";
    private const string KW_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A";
    private const string KW_METALLICGLOSSMAP = "_METALLICGLOSSMAP";
    private const string KW_EMISSION = "_EMISSION";
    private const string KW_GEOM_TYPE_BRANCH = "GEOM_TYPE_BRANCH";

    #endregion

    public static void SetTransparentMode(Material _mat, bool _on)
    {
        //2025-09-19 KHJ : 해당사항 없는 머테리얼 제외 
        if (!_mat.HasProperty(ID_TMode))
            return;
        //Dump(_mat);
        int prevMode = (int)_mat.GetFloat(ID_TMode);

        //2025-09-19 KHJ :이미 투명모드 
        if (_on && prevMode == 2)
            return;
        //2025-09-19 KHJ :이미 오파큐모드 
        if (!_on && prevMode == 0)
            return;

        GetShaderInfo(_mat, out bool isoutl, out bool islite, out bool istess, out bool ismulti, out bool isonepass,
            out bool istwopass);


        if (_on)
        {
            // 투명 모드 전환
            if (_mat.HasProperty(ID_TMode)) _mat.SetFloat(ID_TMode, 2f);
            if (_mat.HasProperty(ID_BlendMode)) _mat.SetFloat(ID_BlendMode, 3f);

            SetTransparentKeyword(_mat, _on);

            // lil 기본은 투명에서도 ZWrite=1을 사용합니다(프리멀티 경로). 필요 시 0으로 변경 가능.
            _mat.SetInt(ID_ZWrite, 1);
            _mat.SetInt(ID_ZTest, 4);

            // 프리멀티 경로(에디터 유틸 기본): One / OneMinusSrcAlpha
            _mat.SetInt(ID_SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
            _mat.SetInt(ID_DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt(ID_AlphaToMask, 0);

            // 아웃라인 셰이더면 아웃라인 블렌드도 정렬
            if (isoutl)
            {
                _mat.SetInt(ID_OutLSrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _mat.SetInt(ID_OutLDstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _mat.SetInt(ID_OutLAlphaToMask, 0);
            }

            if (ismulti)
            {
                _mat.SetOverrideTag("RenderType", "TransparentCutout");
                _mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // 컷오프 임계는 의미 없도록 낮춤
            if (_mat.HasProperty(ID_Cutoff)) _mat.SetFloat(ID_Cutoff, -0.001f);
        }
        else
        {
            // 불투명 복귀
            if (_mat.HasProperty(ID_TMode)) _mat.SetFloat(ID_TMode, 0f);
            if (_mat.HasProperty(ID_BlendMode)) _mat.SetFloat(ID_BlendMode, 0f);

            SetTransparentKeyword(_mat, _on);

            _mat.SetInt(ID_ZWrite, 1);
            _mat.SetInt(ID_ZTest, 4);

            _mat.SetInt(ID_SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
            _mat.SetInt(ID_DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
            _mat.SetInt(ID_AlphaToMask, 0);

            // 아웃라인 셰이더면 아웃라인 블렌드도 정렬
            if (isoutl)
            {
                _mat.SetInt(ID_OutLSrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                _mat.SetInt(ID_OutLDstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                _mat.SetInt(ID_OutLAlphaToMask, 0);
            }

            if (ismulti)
            {
                _mat.SetOverrideTag("RenderType", "");
                _mat.renderQueue = -1;
            }

            if (_mat.HasProperty(ID_Cutoff)) _mat.SetFloat(ID_Cutoff, 0.001f);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_mat);
#endif
    }

    public static void SetTranparentValue(Material _mat, float _value)
    {
        if (_mat == null) return;
        float alpha = Mathf.Clamp01(_value);

        // 메인 색상 알파
        if (_mat.HasProperty(ID_Color))
        {
            Color c = _mat.GetColor(ID_Color);
            c.a = alpha;
            _mat.SetColor(ID_Color, c);
        }

        // 아웃라인 색상 알파(있을 때만)
        if (_mat.HasProperty(ID_OutlineColor))
        {
            Color oc = _mat.GetColor(ID_OutlineColor);
            oc.a = alpha;
            _mat.SetColor(ID_OutlineColor, oc);
        }

        // if (_mat.HasProperty(idOutlineLitColor))
        // {
        //     var olc = _mat.GetColor(idOutlineLitColor);
        //     olc.a = a;
        //     _mat.SetColor(idOutlineLitColor, olc);
        // }
        //
        // // 페이드 중 하드 컷 방지: Invisible은 항상 0
        // if (_mat.HasProperty(idInvisible))
        //     _mat.SetInt(idInvisible, 0);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_mat);
#endif
    }

    private static void SetTransparentKeyword(Material _mat, bool _on)
    {
        if (_on)
        {
            _mat.EnableKeyword(KW_UI_CLIP_RECT);
            _mat.EnableKeyword(KW_ALPHA_BLEND);
        }
        else
        {
            _mat.DisableKeyword(KW_UI_CLIP_RECT);
            _mat.DisableKeyword(KW_ALPHA_BLEND);
        }
    }

    //2025-09-19 KHJ : shader명으로 라이트/멀티/테스/아웃라인 추정
    static private void GetShaderInfo(Material _mat, out bool isoutl, out bool islite, out bool istess,
        out bool ismulti, out bool isonepass, out bool istwopass)
    {
        string shaderName = _mat.shader != null ? _mat.shader.name : string.Empty;
        isoutl = shaderName.Contains("Outline");
        islite = shaderName.Contains("Lite") || shaderName.Contains("ltsl");
        istess = shaderName.Contains("tess");
        ismulti = shaderName.Contains("Multi") || shaderName.Contains("ltsm");

        isonepass = shaderName.Contains("OnePass");
        istwopass = shaderName.Contains("TwoPass");
    }

    public static void SetColor(Material _mat, Color _color, Color _outlineColor)
    {
        if (_mat == null) return;

        // 메인 색상 알파
        if (_mat.HasProperty(ID_Color))
        {
            _mat.SetColor(ID_Color, _color);
        }

        if (_mat.HasProperty(ID_OutlineColor))
        {
            _mat.SetColor(ID_OutlineColor, _outlineColor);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_mat);
#endif
    }

    public static void GetColor(Material _mat, out Color _color, out Color _outlineColor)
    {
        _color = new Color();
        _outlineColor = new Color();
        if (_mat == null)
        {
            return;
        }

        if (_mat.HasProperty(ID_Color))
        {
            _color = _mat.GetColor(ID_Color);
        }

        if (_mat.HasProperty(ID_OutlineColor))
        {
            _outlineColor = _mat.GetColor(ID_OutlineColor);
        }
    }

    //2025-12-11 KHJ : 구버젼 오버레이 
//     public static void SetOverlayColorMode(Material _mat, bool _on, bool _isEye = false)
//     {
//         if (_mat == null) return;
//
//         if (_on)
//         {
//             //2025-12-09 KHJ : 눈은 발광이 들어가 있어서 발광을 꺼줍니다 
//             if (_isEye)
//             {
//                 if (_mat.HasProperty(ID_UseEmission))
//                 {
//                     _mat.SetFloat(ID_UseEmission, 0f);
//                     _mat.DisableKeyword(KW_EMISSION);
//                 }
//
//                 if (_mat.HasProperty(ID_UseEmission2nd))
//                 {
//                     _mat.SetFloat(ID_UseEmission2nd, 0f);
//                     _mat.DisableKeyword(KW_GEOM_TYPE_BRANCH);
//                 }
//             }
//             else
//             {
//                 if (_mat.HasProperty(ID_RimLight))
//                 {
//                     _mat.SetFloat(ID_RimLight, 0f);
//                     _mat.DisableKeyword(KW_METALLICGLOSSMAP);
//                 }
//
//                 if (_mat.HasProperty(ID_UseMatCap))
//                 {
//                     _mat.SetFloat(ID_UseMatCap, 0f);
//                     _mat.DisableKeyword(KW_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A);
//                 }
//
//                 //2025-12-05 KHJ : 아웃라인의 하이라이트 부분을 투명도 0으로 만들어서 단순하게 아웃라인 컬러로 그리도록 함 
//                 if (_mat.HasProperty(ID_OutlineLitColor))
//                 {
//                     Color c = _mat.GetColor(ID_OutlineLitColor);
//                     c.a = 0f;
//                     _mat.SetColor(ID_OutlineLitColor, c);
//                 }
//             }
//         }
//         else
//         {
//             if (_isEye)
//             {
//                 if (_mat.HasProperty(ID_UseEmission) && _mat.GetTexture(ID_EmissionMap) != null)
//                 {
//                     _mat.SetFloat(ID_UseEmission, 1f);
//                     _mat.EnableKeyword(KW_EMISSION);
//                 }
//
//                 if (_mat.HasProperty(ID_UseEmission2nd) && _mat.GetTexture(ID_Emission2ndMap) != null)
//                 {
//                     _mat.SetFloat(ID_UseEmission2nd, 1f);
//                     _mat.EnableKeyword(KW_GEOM_TYPE_BRANCH);
//                 }
//             }
//             else
//             {
//                 if (_mat.HasProperty(ID_RimLight))
//                 {
//                     _mat.SetFloat(ID_RimLight, 1f);
//                     _mat.EnableKeyword(KW_METALLICGLOSSMAP);
//                 }
//
//                 if (_mat.HasProperty(ID_UseMatCap))
//                 {
//                     _mat.SetFloat(ID_UseMatCap, 1f);
//                     _mat.EnableKeyword(KW_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A);
//                 }
//
//                 if (_mat.HasProperty(ID_OutlineLitColor))
//                 {
//                     Color c = _mat.GetColor(ID_OutlineLitColor);
//                     _mat.SetColor(ID_OutlineLitColor, c);
//                 }
//             }
//         }
//
//
// #if UNITY_EDITOR
//         UnityEditor.EditorUtility.SetDirty(_mat);
// #endif
//     }

    public static void SetOverlayColor(SkinnedMeshRenderer _smr, Color _color)
    {
        MaterialPropertyBlock newPropertyBlock = new MaterialPropertyBlock();
        _smr.GetPropertyBlock(newPropertyBlock);
        
        //2025-12-11 KHJ : 오버레이 쉐이더만 이 프로퍼티를 가지고 있도록 해야 해당 기능 정상 작동  
        newPropertyBlock.SetColor(ID_OverlayColor, _color);

        _smr.SetPropertyBlock(newPropertyBlock);
    }

    public static void SetOverlayAlpha(SkinnedMeshRenderer _smr, float alpha)
    {
        MaterialPropertyBlock newPropertyBlock = new MaterialPropertyBlock();
        _smr.GetPropertyBlock(newPropertyBlock);

        //2025-12-11 KHJ : 오버레이 쉐이더만 이 프로퍼티를 가지고 있도록 해야 해당 기능 정상 작동  
        Color originColor = newPropertyBlock.GetColor(ID_OverlayColor);
        originColor.a = Mathf.Min(originColor.a, alpha);
        newPropertyBlock.SetColor(ID_OverlayColor, originColor);

        _smr.SetPropertyBlock(newPropertyBlock);
    }

}