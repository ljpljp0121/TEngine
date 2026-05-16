using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Graphics", 3)]
    public class GraphicsInformation : InfoBase
    {
        [InfoItem("Device ID")] public string DeviceId => SystemInfo.graphicsDeviceID.ToString();
        [InfoItem("Device Name")] public string DeviceName => SystemInfo.graphicsDeviceName;
        [InfoItem("Device Vendor ID")] public string DeviceVendorId => SystemInfo.graphicsDeviceVendorID.ToString();
        [InfoItem("Device Vendor")] public string DeviceVendor => SystemInfo.graphicsDeviceVendor;
        [InfoItem("Device Type")] public string DeviceType => SystemInfo.graphicsDeviceType.ToString();
        [InfoItem("Device Version")] public string DeviceVersion => SystemInfo.graphicsDeviceVersion;
        [InfoItem("Memory Size")] public string MemorySize => $"{SystemInfo.graphicsMemorySize} MB";
        [InfoItem("Multi Threaded")] public string MultiThreaded => SystemInfo.graphicsMultiThreaded.ToString();
        [InfoItem("Rendering Threading Mode")]
        public string RenderingThreadingMode => SystemInfo.renderingThreadingMode.ToString();
        [InfoItem("HDR Display Support Flags")]
        public string HRDDisplaySupportFlags => SystemInfo.hdrDisplaySupportFlags.ToString();
        [InfoItem("Shader Level")] public string ShaderLevel => GetShaderLevelString(SystemInfo.graphicsShaderLevel);
        [InfoItem("Global Maximum LOD")] public string GlobalMaximumLOD => Shader.globalMaximumLOD.ToString();
        [InfoItem("Global Render Pipeline")] public string GlobalRenderPipeline => Shader.globalRenderPipeline;
        [InfoItem("Min OpenGLES Version")] public string MinOpenGLESVersion => Graphics.minOpenGLESVersion.ToString();
        [InfoItem("Active Tier")] public string ActiveTier => Graphics.activeTier.ToString();
        [InfoItem("Active Color Gamut")] public string ActiveColorGamut => Graphics.activeColorGamut.ToString();
        [InfoItem("Preserve Frame Buffer Alpha")]
        public string PreserveFrameBufferAlpha => Graphics.preserveFramebufferAlpha.ToString();
        [InfoItem("NPOT Support")] public string NPOTSupport => SystemInfo.npotSupport.ToString();
        [InfoItem("Max Texture Size")] public string MaxTextureSize => SystemInfo.maxTextureSize.ToString();
        [InfoItem("Supported Render Target Count")]
        public string SupportedRenderTargetCount => SystemInfo.supportedRenderTargetCount.ToString();
        [InfoItem("Support Random Write Target Count")]
        public string SupportRandomWriteTargetCount => SystemInfo.supportedRandomWriteTargetCount.ToString();
        [InfoItem("Copy Texture Support")] public string CopyTextureSupport => SystemInfo.copyTextureSupport.ToString();
        [InfoItem("Uses Reversed ZBuffer")]
        public string UsesReversedZBuffer => SystemInfo.usesReversedZBuffer.ToString();
        [InfoItem("Max Cubemap Size")] public string MaxCubemapSize => SystemInfo.maxCubemapSize.ToString();
        [InfoItem("Graphics UV Starts At Top")]
        public string GraphicsUVStartsAtTop => SystemInfo.graphicsUVStartsAtTop.ToString();
        [InfoItem("Constant Buffer Offset Alignment")]
        public string ConstantBufferOffsetAlignment => SystemInfo.constantBufferOffsetAlignment.ToString();
        [InfoItem("Has Hidden Surface Removal On GPU")]
        public string HasHiddenSurfaceRemovalOnGPU => SystemInfo.hasHiddenSurfaceRemovalOnGPU.ToString();
        [InfoItem("Has Dynamic Uniform Array Indexing In Fragment Shaders")]
        public string HasDynamicUniformArrayIndexingInFragmentShaders =>
            SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders.ToString();
        [InfoItem("Uses Load Store Actions")]
        public string UsesLoadStoreActions => SystemInfo.usesLoadStoreActions.ToString();
        [InfoItem("Max Compute Buffer Inputs Compute")]
        public string MaxComputeBufferInputsCompute => SystemInfo.maxComputeBufferInputsCompute.ToString();
        [InfoItem("Max Compute Buffer Inputs Domain")]
        public string MaxComputeBufferInputsDomain => SystemInfo.maxComputeBufferInputsDomain.ToString();
        [InfoItem("Max Compute Buffer Inputs Fragment")]
        public string MaxComputeBufferInputsFragment => SystemInfo.maxComputeBufferInputsFragment.ToString();
        [InfoItem("Max Compute Buffer Inputs Geometry")]
        public string MaxComputeBufferInputsGeometry => SystemInfo.maxComputeBufferInputsGeometry.ToString();
        [InfoItem("Max Compute Buffer Inputs Hull")]
        public string MaxComputeBufferInputsHull => SystemInfo.maxComputeBufferInputsHull.ToString();
        [InfoItem("Max Compute Buffer Inputs Vertex")]
        public string MaxComputeBufferInputsVertex => SystemInfo.maxComputeBufferInputsVertex.ToString();
        [InfoItem("Max Compute Work Group Size")]
        public string MaxComputeWorkGroupSize => SystemInfo.maxComputeWorkGroupSize.ToString();
        [InfoItem("Max Compute Work Group Size X")]
        public string MaxComputeWorkGroupSizeX => SystemInfo.maxComputeWorkGroupSizeX.ToString();
        [InfoItem("Max Compute Work Group Size Y")]
        public string MaxComputeWorkGroupSizeY => SystemInfo.maxComputeWorkGroupSizeY.ToString();
        [InfoItem("Max Compute Work Group Size Z")]
        public string MaxComputeWorkGroupSizeZ => SystemInfo.maxComputeWorkGroupSizeZ.ToString();
        [InfoItem("Supports Sparse Textures")]
        public string SupportsSparseTextures => SystemInfo.supportsSparseTextures.ToString();
        [InfoItem("Supports 3D Textures")] public string Supports3DTextures => SystemInfo.supports3DTextures.ToString();
        [InfoItem("Supports Shadows")] public string SupportsShadows => SystemInfo.supportsShadows.ToString();
        [InfoItem("Supports Raw Shadow Depth Sampling")]
        public string SupportsRawShadowDepthSampling => SystemInfo.supportsRawShadowDepthSampling.ToString();
        [InfoItem("Supports Compute Shaders")]
        public string SupportsComputeShaders => SystemInfo.supportsComputeShaders.ToString();
        [InfoItem("Supports Instancing")] public string SupportsInstancing => SystemInfo.supportsInstancing.ToString();
        [InfoItem("Supports 2D Array Textures")]
        public string Supports2DArrayTextures => SystemInfo.supports2DArrayTextures.ToString();
        [InfoItem("Supports Motion Vectors")]
        public string SupportsMotionVectors => SystemInfo.supportsMotionVectors.ToString();
        [InfoItem("Supports Cubemap Array Textures")]
        public string SupportsCubemapArrayTextures => SystemInfo.supportsCubemapArrayTextures.ToString();
        [InfoItem("Supports 3D Render Textures")]
        public string Supports3DRenderTextures => SystemInfo.supports3DRenderTextures.ToString();
        [InfoItem("Supports Texture Wrap Mirror Once")]
        public string SupportsTextureWrapMirrorOnce => SystemInfo.supportsTextureWrapMirrorOnce.ToString();
        [InfoItem("Supports Graphics Fence")]
        public string SupportsGraphicsFence => SystemInfo.supportsGraphicsFence.ToString();
        [InfoItem("Supports Async Compute")]
        public string SupportsAsyncCompute => SystemInfo.supportsAsyncCompute.ToString();
        [InfoItem("Supports Multisampled Textures")]
        public string SupportsMultisampledTextures => SystemInfo.supportsMultisampledTextures.ToString();
        [InfoItem("Supports Async GPU Readback")]
        public string SupportsAsyncGPUReadback => SystemInfo.supportsAsyncGPUReadback.ToString();
        [InfoItem("Supports 32bits Index Buffer")]
        public string Supports32bitsIndexBuffer => SystemInfo.supports32bitsIndexBuffer.ToString();
        [InfoItem("Supports Hardware Quad Topology")]
        public string SupportsHardwareQuadTopology => SystemInfo.supportsHardwareQuadTopology.ToString();
        [InfoItem("Supports Mip Streaming")]
        public string SupportsMipStreaming => SystemInfo.supportsMipStreaming.ToString();
        [InfoItem("Supports Multisample Auto Resolve")]
        public string SupportsMultisampleAutoResolve => SystemInfo.supportsMultisampleAutoResolve.ToString();
        [InfoItem("Supports Separated Render Targets Blend")]
        public string SupportsSeparatedRenderTargetsBlend => SystemInfo.supportsSeparatedRenderTargetsBlend.ToString();
        [InfoItem("Supports Set Constant Buffer")]
        public string SupportsSetConstantBuffer => SystemInfo.supportsSetConstantBuffer.ToString();
        [InfoItem("Supports Geometry Shaders")]
        public string SupportsGeometryShaders => SystemInfo.supportsGeometryShaders.ToString();
        [InfoItem("Supports Ray Tracing")] public string SupportsRayTracing => SystemInfo.supportsRayTracing.ToString();
        [InfoItem("Supports Tessellation Shaders")]
        public string SupportsTessellationShaders => SystemInfo.supportsTessellationShaders.ToString();
        [InfoItem("Supports Compressed 3D Textures")]
        public string SupportsCompressed3DTextures => SystemInfo.supportsCompressed3DTextures.ToString();
        [InfoItem("Supports Conservative Raster")]
        public string SupportsConservativeRaster => SystemInfo.supportsConservativeRaster.ToString();
        [InfoItem("Supports Gpu Recorder")]
        public string SupportsGpuRecorder => SystemInfo.supportsGpuRecorder.ToString();
        [InfoItem("Supports Multisampled 2D Array Textures")]
        public string SupportsMultisampled2DArrayTextures => SystemInfo.supportsMultisampled2DArrayTextures.ToString();
        [InfoItem("Supports Multiview")] public string SupportsMultiview => SystemInfo.supportsMultiview.ToString();
        [InfoItem("Supports Render Target Array Index From Vertex Shader")]
        public string SupportsRenderTargetArrayIndexFromVertexShader =>
            SystemInfo.supportsRenderTargetArrayIndexFromVertexShader.ToString();

        private string GetShaderLevelString(int shaderLevel)
        {
            return $"Shader Model {shaderLevel / 10}.{shaderLevel % 10}";
        }
    }
}