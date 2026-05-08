using UnityEngine;

/// <summary>
/// 玻璃材质创建工具
/// 可以在运行时创建透明玻璃材质
/// </summary>
public static class GlassMaterialCreator
{
    /// <summary>
    /// 创建透明玻璃材质
    /// </summary>
    /// <param name="color">玻璃颜色（带透明度）</param>
    /// <param name="smoothness">光滑度 (0-1)</param>
    /// <param name="metallic">金属度 (0-1)</param>
    /// <returns>创建的玻璃材质</returns>
    public static Material CreateGlassMaterial(Color color, float smoothness = 0.9f, float metallic = 0.1f)
    {
        Material glassMaterial = new Material(Shader.Find("Standard"));

        // 设置渲染模式为透明
        glassMaterial.SetFloat("_Mode", 3); // Transparent mode
        glassMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        glassMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glassMaterial.SetInt("_ZWrite", 0);
        glassMaterial.DisableKeyword("_ALPHATEST_ON");
        glassMaterial.DisableKeyword("_ALPHABLEND_ON");
        glassMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        glassMaterial.renderQueue = 3000;

        // 设置颜色和透明度
        glassMaterial.SetColor("_Color", color);

        // 设置光滑度和金属度
        glassMaterial.SetFloat("_Glossiness", smoothness);
        glassMaterial.SetFloat("_Metallic", metallic);

        return glassMaterial;
    }

    /// <summary>
    /// 预设的玻璃材质
    /// </summary>
    public static class Presets
    {
        /// <summary>透明蓝色玻璃</summary>
        public static Material BlueGlass()
        {
            return CreateGlassMaterial(new Color(0.7f, 0.9f, 1f, 0.3f), 0.9f, 0.1f);
        }

        /// <summary>透明绿色玻璃</summary>
        public static Material GreenGlass()
        {
            return CreateGlassMaterial(new Color(0.7f, 1f, 0.8f, 0.3f), 0.9f, 0.1f);
        }

        /// <summary>透明红色玻璃</summary>
        public static Material RedGlass()
        {
            return CreateGlassMaterial(new Color(1f, 0.7f, 0.7f, 0.3f), 0.9f, 0.1f);
        }

        /// <summary>透明白色玻璃</summary>
        public static Material ClearGlass()
        {
            return CreateGlassMaterial(new Color(1f, 1f, 1f, 0.2f), 0.95f, 0.05f);
        }

        /// <summary>磨砂玻璃</summary>
        public static Material FrostedGlass()
        {
            return CreateGlassMaterial(new Color(0.9f, 0.9f, 0.9f, 0.5f), 0.5f, 0.0f);
        }
    }
}
