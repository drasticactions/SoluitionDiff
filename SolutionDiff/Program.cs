using net.r_eg.MvsSln;
using LibGit2Sharp;
using System.Collections.Concurrent;
using net.r_eg.MvsSln.Core;
using HandlebarsDotNet;

Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

var directory = args[0];

using var repo = new Repository(directory);

// Get two commits to diff.
var commit1 = repo.Commits.First(n => n.Id == new ObjectId(args[1]));
var commit2 = repo.Commits.First(n => n.Id == new ObjectId(args[2]));

var diffTree1 = repo.Diff.Compare<Patch>(commit1.Tree, commit2.Tree);
var slnDiffer = new SolutionDiffItem()
{
    Repo = repo,
    OldCommit = commit1,
    NewCommit = commit2,
    Patch = diffTree1,
};

// Get all of the solutions in the directory.
var slns = DiscoverSolutions(directory);
var slnDiffs = new ConcurrentBag<SolutionDifferences>();

Parallel.ForEach(slns, (sln) => {
    var slnDiff = GetSolutionDifferences(sln, diffTree1, directory);
    if (slnDiff.ProjectDifferences.Any())
        slnDiffs.Add(slnDiff);
});

slnDiffer.Solutions = slnDiffs.ToList();

GenerateHtmlTemplate(slnDiffer);

SolutionDifferences GetSolutionDifferences(Sln sln, Patch diffs, string repoPath = "")
{
    ConcurrentBag<ProjectDifferences> projDifferences = new ConcurrentBag<ProjectDifferences>();
    Parallel.ForEach(sln.Result.Env.Projects, (proj) => {
        var item = GetProjectDifferences(proj, diffs, repoPath);
        if (item.Items.Any())
            projDifferences.Add(item);
    });

    return new SolutionDifferences() { ProjectDifferences = projDifferences.ToList(), Solution = sln };
}

ProjectDifferences GetProjectDifferences(IXProject proj, Patch diffs, string repoPath = "")
{
    ConcurrentBag<ProjectDifference> differences = new ConcurrentBag<ProjectDifference>();

    Parallel.ForEach(diffs, (diff) => {
        var item = proj.GetItems("Compile").Where(n => Path.GetFullPath(diff.Path, repoPath) == Path.Combine(proj.ProjectPath, n.evaluatedInclude));
        if (item.Any())
        {
            differences.Add(new ProjectDifference() { Diff = diff, Item = item.First() });
        }
    });
    return new ProjectDifferences() { Project = proj, Items = differences.ToList() };
}

List<Sln> DiscoverSolutions(string directory)
{
    var slnList = new ConcurrentBag<Sln>();

    var slnFiles = Directory.GetFiles(directory, "*.sln", SearchOption.AllDirectories);
    Parallel.ForEach(slnFiles, (slnFile) => {
        slnList.Add(new Sln(slnFile, SlnItems.EnvWithMinimalProjects));
    });

    return slnList.ToList();
}

void GenerateHtmlTemplate(SolutionDiffItem item, string filename = "temp.html")
{
    var templateHtml = File.ReadAllText("Index.html.hbs");
    var handlebars = Handlebars.Create();
    var template = handlebars.Compile(templateHtml);
    var result = template.Invoke(slnDiffer);
    File.WriteAllText(filename, result);
}

public class SolutionDiffItem
{
    public Repository Repo { get; set; }

    public Commit OldCommit { get; set; }

    public Commit NewCommit { get; set; }

    public Patch Patch { get; set; }

    public Remote Origin => Repo?.Network.Remotes.FirstOrDefault(n => n.Name == "origin");

    public List<SolutionDifferences> Solutions { get; set; }

    public string LocalJson { get; set; }
}

public class SolutionDifferences
{
    public Sln Solution { get; set; }

    public string SolutionFileName => Path.GetFileNameWithoutExtension(Solution.Result.SolutionFile);

    public string SolutionFileNameHeader => SolutionFileName.Replace(".", "-");

    public List<ProjectDifferences> ProjectDifferences { get; set; } = new List<ProjectDifferences>();

    public void Print()
    {
        Console.WriteLine($"Affected Solution: {Solution.Result.SolutionFile}");
        foreach (var difference in ProjectDifferences)
            difference.PrintItems();
    }
}

public class ProjectDifferences
{
    public IXProject Project { get;  set; }

    public string ProjectFileNameHeader => Project.ProjectName.Replace(".", "-");

    public List<ProjectDifference> Items { get; set; } = new List<ProjectDifference>();

    public void PrintItems()
    {
        Console.WriteLine($"Affected Project: {Project.ProjectName}");
        foreach (var item in Items)
            item.Print();
    }
}

public class ProjectDifference 
{
    public net.r_eg.MvsSln.Projects.Item Item { get; set; }

    public PatchEntryChanges Diff { get; set; }

    public void Print()
    {
        Console.WriteLine($"Affected Item: {Item.evaluatedInclude}");
        Console.WriteLine($"Patch: {Diff?.Patch}");
    }
}