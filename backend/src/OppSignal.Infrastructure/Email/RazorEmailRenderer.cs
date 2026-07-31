using RazorLight;

namespace OppSignal.Infrastructure.Email;

public interface IEmailRenderer
{
    Task<string> RenderAsync<TModel>(string templateKey, TModel model);
}

/// <summary>
/// Renders embedded <c>.cshtml</c> email templates with RazorLight. The engine is
/// built once and cached; templates live under
/// <c>OppSignal.Infrastructure/Email/Templates/*.cshtml</c> (embedded resources).
/// </summary>
public sealed class RazorEmailRenderer : IEmailRenderer
{
    private readonly IRazorLightEngine _engine;

    public RazorEmailRenderer()
    {
        _engine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(RazorEmailRenderer).Assembly, "OppSignal.Infrastructure.Email.Templates")
            .UseMemoryCachingProvider()
            .Build();
    }

    public Task<string> RenderAsync<TModel>(string templateKey, TModel model)
        => _engine.CompileRenderAsync(templateKey, model);
}
