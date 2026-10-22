namespace KandyTourism.Api.Tests.Infrastructure;

public static class RepositoryPaths
{
    public static string SolutionDirectory { get; } = FindSolutionDirectory();

    public static string ApiProjectDirectory => Path.Combine(SolutionDirectory, "kandy-tourism-backend");

    private static string FindSolutionDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "kandy-tourism-backend.slnx")))
            {
                return dir.FullName;
            }
        }
        throw new InvalidOperationException("Could not locate kandy-tourism-backend.slnx above the test output directory.");
    }
}
