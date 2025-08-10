using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    private Camera camera;

    const string BUFFER_NAME = "CameraRenderer"; //缓冲区名称

    private readonly CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME
    };

    private CullingResults cullingResults;

    private static readonly ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        //设置命令缓冲区的名字
        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull())
        {
            return;
        }

        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    /// <summary>
    /// 设置相机属性和清除缓冲区
    /// </summary>
    private void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags <= CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    /// <summary>
    /// 绘制可见物体
    /// </summary>
    private void DrawVisibleGeometry()
    {
        //设置绘制顺序和指定渲染相机
        var soringSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        //设置渲染的ShaderPass和排序模式
        var drawingSettings = new DrawingSettings(unlitShaderTagId, soringSettings);
        //只绘制不透明物体
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        //绘制不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        context.DrawSkybox(camera);
        soringSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = soringSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        //绘制透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    /// <summary>
    /// 剔除
    /// </summary>
    bool Cull()
    {
        if (camera.TryGetCullingParameters(out var p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
}