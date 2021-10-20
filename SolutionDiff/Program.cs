using net.r_eg.MvsSln;
using LibGit2Sharp;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

//var parsedSolution = parser.Parse(@"C:\Users\t_mil\source\repos\Work\UITools\Designer\Xamarin.Designer.Forms\FormsPreviewer.sln");

using var sln = new Sln(@"C:\Users\t_mil\source\repos\Work\UITools\Designer\Xamarin.Designer.Forms\FormsPreviewer.sln", SlnItems.EnvWithMinimalProjects);

var proj = sln.Result.ProjectItems.First(n => n.name == "Xamarin.Designer.Forms");

var test = sln.Result.Env.Projects.FirstOrDefault(n => n.ProjectName == "Xamarin.Designer.Forms");

var items = test.GetItems().FirstOrDefault(n => n.evaluatedInclude.Contains("FormsXamlDesignerSession"));

var poop = items.evaluatedInclude;

using var repo = new Repository(@"C:\Users\t_mil\source\repos\Work\UITools\");

var repoTree1 = repo.Commits.First(n => n.Id == new ObjectId("27a744df9ba91a0a4f036bf1a6569a177a0588a8"));

var repoTree2 = repo.Head.Tip;

var diffTree = repo.Diff.Compare<TreeChanges>(repoTree1.Tree, repoTree2.Tree);

foreach(var diff in diffTree)
{

    var realPath = test.GetItems().Where(n => Path.GetFullPath(diff.Path, @"C:\Users\t_mil\source\repos\Work\UITools\") == Path.Combine(test.ProjectPath, n.evaluatedInclude));
    if (realPath.Any())
    {
        Console.WriteLine($"Status: {diff.Status}");
        Console.WriteLine($"Old Path: {diff.OldPath}");
        Console.WriteLine($"New Path: {diff.Path}");
        Console.WriteLine($"New Exists: {diff.OldExists}");
        Console.WriteLine($"New Exists: {diff.Exists}");
    }
}
