using GitForest.Core.Services;
using MediatR;

namespace GitForest.Application.Features.Forest;

public sealed record InitForestCommand(string? DirOptionValue, bool Force) : IRequest<InitForestResult>;

public sealed record InitForestResult(string DirectoryOptionValue, string ForestDirPath);

internal sealed class InitForestHandler : IRequestHandler<InitForestCommand, InitForestResult>
{
    private readonly IForestInitializer _initializer;

    public InitForestHandler(IForestInitializer initializer)
    {
        _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
    }

    public Task<InitForestResult> Handle(InitForestCommand request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (request is null) throw new ArgumentNullException(nameof(request));

        // Init is idempotent today; Force is reserved for future behavior.
        _ = request.Force;

        var dir = string.IsNullOrWhiteSpace(request.DirOptionValue) ? ".git-forest" : request.DirOptionValue!;
        var forestDir = Path.IsPathRooted(dir)
            ? Path.GetFullPath(dir)
            : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, dir));

        _initializer.Initialize(forestDir);

        return Task.FromResult(new InitForestResult(DirectoryOptionValue: dir, ForestDirPath: forestDir));
    }
}

