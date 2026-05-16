using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Other/Quality", 13)]
    public class Other_Quality_Info : InfoBase
    {
        [GmCommand("system.quality",
            "设置画质等级 level: 画质等级 0~6  applyExpensiveChanges:true= 彻底切换（设置页用），false= 轻量切换（运行时用）")]
        public static void SetQualityLevel(int level, bool applyExpensiveChanges = true)
        {
            if (level >= 0 && level < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(level, applyExpensiveChanges);
                Debug.Log($"Quality Level set to {QualitySettings.names[level]}");
            }
            else
            {
                Debug.LogError($"Invalid quality level: {level}, valid range: 0-{QualitySettings.names.Length - 1}");
            }
        }

        [InfoItem("Current Quality Level")]
        public string CurrentQualityLevel => QualitySettings.names[QualitySettings.GetQualityLevel()];
        [InfoItem("Active Color Space")] public string ActiveColorSpace => QualitySettings.activeColorSpace.ToString();
        [InfoItem("Desired Color Space")]
        public string DesiredColorSpace => QualitySettings.desiredColorSpace.ToString();
        [InfoItem("Max Queued Frames")] public string MaxQueuedFrames => QualitySettings.maxQueuedFrames.ToString();
        [InfoItem("Pixel Light Count")] public string PixelLightCount => QualitySettings.pixelLightCount.ToString();
        [InfoItem("Master Texture Limit")]
        public string MasterTextureLimit => QualitySettings.globalTextureMipmapLimit.ToString();
        [InfoItem("Anisotropic Filtering")]
        public string AnisotropicFiltering => QualitySettings.anisotropicFiltering.ToString();
        [InfoItem("Anti Aliasing")] public string AntiAliasing => QualitySettings.antiAliasing.ToString();
        [InfoItem("Soft Particles")] public string SoftParticles => QualitySettings.softParticles.ToString();
        [InfoItem("Soft Vegetation")] public string SoftVegetation => QualitySettings.softVegetation.ToString();
        [InfoItem("Realtime Reflection Probes")]
        public string RealtimeReflectionProbes => QualitySettings.realtimeReflectionProbes.ToString();
        [InfoItem("Billboards Face Camera Position")]
        public string BillboardsFaceCameraPosition => QualitySettings.billboardsFaceCameraPosition.ToString();
        [InfoItem("Resolution Scaling Fixed DPI Factor")]
        public string ResolutionScalingFixedDPIFactor => QualitySettings.resolutionScalingFixedDPIFactor.ToString();
        [InfoItem("Texture Streaming Enabled")]
        public string TextureStreamingEnabled => QualitySettings.streamingMipmapsActive.ToString();
        [InfoItem("Texture Streaming Add All Cameras")]
        public string TextureStreamingAddAllCameras => QualitySettings.streamingMipmapsAddAllCameras.ToString();
        [InfoItem("Texture Streaming Memory Budget")]
        public string TextureStreamingMemoryBudget => QualitySettings.streamingMipmapsMemoryBudget.ToString();
        [InfoItem("Texture Streaming Renderers Per Frame")]
        public string TextureStreamingRenderersPerFrame => QualitySettings.streamingMipmapsRenderersPerFrame.ToString();
        [InfoItem("Texture Streaming Max Level Reduction")]
        public string TextureStreamingMaxLevelReduction => QualitySettings.streamingMipmapsMaxLevelReduction.ToString();
        [InfoItem("Texture Streaming Max File IO Requests")]
        public string TextureStreamingMaxFileIORequests => QualitySettings.streamingMipmapsMaxFileIORequests.ToString();
        [InfoItem("Shadowmask Mode")] public string ShadowmaskMode => QualitySettings.shadowmaskMode.ToString();
        [InfoItem("Shadow Quality")] public string ShadowQuality => QualitySettings.shadows.ToString();
        [InfoItem("Shadow Resolution")] public string ShadowResolution => QualitySettings.shadowResolution.ToString();
        [InfoItem("Shadow Projection")] public string ShadowProjection => QualitySettings.shadowProjection.ToString();
        [InfoItem("Shadow Distance")] public string ShadowDistance => QualitySettings.shadowDistance.ToString();
        [InfoItem("Shadow Near Plane Offset")]
        public string ShadowNearPlaneOffset => QualitySettings.shadowNearPlaneOffset.ToString();
        [InfoItem("Shadow Cascades")] public string ShadowCascades => QualitySettings.shadowCascades.ToString();
        [InfoItem("Shadow Cascade 2 Split")]
        public string ShadowCascade2Split => QualitySettings.shadowCascade2Split.ToString();
        [InfoItem("Shadow Cascade 4 Split")]
        public string ShadowCascade4Split => QualitySettings.shadowCascade4Split.ToString();
        [InfoItem("Skin Weights")] public string SkinWeights => QualitySettings.skinWeights.ToString();
        [InfoItem("VSync Count")] public string VSyncCount => QualitySettings.vSyncCount.ToString();
        [InfoItem("LOD Bias")] public string LODBias => QualitySettings.lodBias.ToString();
        [InfoItem("Maximum LOD Level")] public string MaximumLODLevel => QualitySettings.maximumLODLevel.ToString();
        [InfoItem("Particle Raycast Budget")]
        public string ParticleRaycastBudget => QualitySettings.particleRaycastBudget.ToString();
        [InfoItem("Async Upload Time Slice")]
        public string AsyncUploadTimeSlice => $"{QualitySettings.asyncUploadTimeSlice} ms";
        [InfoItem("Async Upload Buffer Size")]
        public string AsyncUploadBufferSize => $"{QualitySettings.asyncUploadBufferSize} MB";
        [InfoItem("Async Upload Persistent Buffer")]
        public string AsyncUploadPersistentBuffer => QualitySettings.asyncUploadPersistentBuffer.ToString();
    }
}