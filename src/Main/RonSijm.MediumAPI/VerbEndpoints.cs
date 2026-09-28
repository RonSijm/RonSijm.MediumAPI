namespace RonSijm.MediumAPI;

// Each of these interfaces is nothing more than `IEndpoint` with `Verb` pre-filled via a default
// interface implementation - implementing e.g. `IGetEndpoint` instead of `IEndpoint` just saves you
// from writing `public HttpVerb Verb => HttpVerb.Get;` yourself. You can still override `Verb`
// explicitly in your class if for some reason you want to lie about it.
//
// The `<TOut>` and `<TIn, TOut>` variants additionally pin down the `HandleAsync` signature itself
// (no-input-parameter and single-input-parameter shapes, respectively) so the compiler - not just
// the analyzer - can catch a mismatched handler. Endpoints that need more than one input parameter
// (e.g. a route id *and* a request body) can't fit that shape and should keep implementing the
// non-generic verb interface with a free-form `HandleAsync`, same as before.

#region GET

public interface IGetEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Get;
}

public interface IGetEndpoint<TOut> : IGetEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IGetEndpoint<TIn, TOut> : IGetEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region POST

public interface IPostEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Post;
}

public interface IPostEndpoint<TOut> : IPostEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IPostEndpoint<TIn, TOut> : IPostEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region PUT

public interface IPutEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Put;
}

public interface IPutEndpoint<TOut> : IPutEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IPutEndpoint<TIn, TOut> : IPutEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region PATCH

public interface IPatchEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Patch;
}

public interface IPatchEndpoint<TOut> : IPatchEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IPatchEndpoint<TIn, TOut> : IPatchEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region DELETE

public interface IDeleteEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Delete;
}

public interface IDeleteEndpoint<TOut> : IDeleteEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IDeleteEndpoint<TIn, TOut> : IDeleteEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region HEAD

public interface IHeadEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Head;
}

public interface IHeadEndpoint<TOut> : IHeadEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IHeadEndpoint<TIn, TOut> : IHeadEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region OPTIONS

public interface IOptionsEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Options;
}

public interface IOptionsEndpoint<TOut> : IOptionsEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface IOptionsEndpoint<TIn, TOut> : IOptionsEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion

#region TRACE

public interface ITraceEndpoint : IEndpoint
{
	HttpVerb IEndpoint.Verb => HttpVerb.Trace;
}

public interface ITraceEndpoint<TOut> : ITraceEndpoint
{
	Task<TOut> HandleAsync(CancellationToken cancellationToken);
}

public interface ITraceEndpoint<TIn, TOut> : ITraceEndpoint
{
	Task<TOut> HandleAsync(TIn request, CancellationToken cancellationToken);
}

#endregion
