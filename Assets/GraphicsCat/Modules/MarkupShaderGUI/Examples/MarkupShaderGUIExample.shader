Shader "GraphicsCat/MarkupShaderGUI/Example" 
{
    Properties
    {
        [BeginFoldout(Foldout)]
            [Toggle] _Prop1("Prop1", Float) = 1
            _Prop2("Prop2", Color) = (1, 1, 1, 1)
        [EndFoldout] 

        [BeginFoldout(EnableIf)]
            [Toggle(_NORMALMAP)] _NORMALMAP("Normal", Float) = 0
            [BeginEnableIf(_NORMALMAP, Equal, 1)]
                [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
                _BumpScale("Normal Scale", Float) = 1.0
            [EndEnableIf]
        [EndFoldout] 

        [BeginFoldout(ShowIf)]
            [Toggle(_EMISSION)] _EMISSION("Emission", Float) = 0
            [BeginShowIf(_EMISSION, Equal, 1)]
                _EmissionMap("Emission Map", 2D) = "white" {}
                [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
            [EndShowIf]
        [EndFoldout] 

        [BeginFoldout(Separator)]
            [Enum(Off, 0, On, 1)] _PropAbove("Prop Above", Float) = 0
            [Separator]
            _PropBelow("Prop Below", Color) = (1, 1, 1, 1)
        [EndFoldout] 

        [BeginFoldout(MiniTexture)]
            [MiniTexture] _MiniTexture("MiniTexture", 2D) = "white" {}
        [EndFoldout]

        [BeginFoldout(MiniTextureWithColor)]
            [BeginMiniTextureWithColor]
                [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
                [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
            [EndMiniTextureWithColor]
        [EndFoldout]

        [BeginFoldout(FloatRange)]
            [FloatRange(0, 1)] _FloatRange("FloatRange", Float) = 0.5
            _Range("UnityRange", Range(0, 1)) = 0.5
        [EndFoldout]

        [BeginFoldout(MultiLineVector)]
            [MultiLineVector(2, X, 0f, 1f, Y, 0f, 1f)] 
            _MultiLineVector2("Multi Line Vector2", Vector) = (1, 1, 1, 1)
            [MultiLineVector(3, Top, n1f, 1f, Bottom, n1f, 1f, Left, n1f, 1f, Right, n1f, 1f)] 
            _MultiLineVector3("Multi Line Vector3", Vector) = (1, 1, 1, 1)
        [EndFoldout]

        [BeginFoldout(Label)]
            [Label(Label Default)]
            [Label(Label Size15, 15)]
            [Label(Label Size15 Normal, 15, Normal)]
            [Label(Label Size15 Bold, 15, Bold)]
            [Label(Label Size15 Italic, 15, Italic)]
            [Label(Label Size15 BoldAndItalic, 15, BoldAndItalic)]
        [EndFoldout] 

        [BeginFoldout(Tooltip)]
            _Tooltip("Tooltip [This is Tooltip Text]", Color) = (1, 1, 1, 1)
        [EndFoldout]

        [BeginFoldout(HelpBox)]
            [HelpBox] _NeutralHelpBox("This is a neutral message.", Float) = 0
            [HelpBox(Info)] _InfoHelpBox("This is an info message.", Float) = 0
            [HelpBox(Warning)] _WarningHelpBox("This is a warning message.", Float) = 0
            [HelpBox(Error)] _ErrorHelpBox("This is an error message.", Float) = 0
        [EndFoldout]
         
        [Separator]
        [HideInInspector] _("", Float) = 0
    }

    SubShader
    {
        Pass
        {
 
        }
    }

    CustomEditor "GraphicsCat.MarkupShaderGUI"
    FallBack "Universal Render Pipeline/Unlit"
}




