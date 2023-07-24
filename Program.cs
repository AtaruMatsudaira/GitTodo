using System.Text;
using Cysharp.Diagnostics;
using Zx;
using static Zx.Env;

var br = Environment.NewLine;
var sb = new StringBuilder();

await ConsoleApp.RunAsync(args, MainAsync);

async Task<int> MainAsync(
    [Option("path", "走査するディレクトリ")] string rootPath,
    [Option("ex", "拡張子")] string extensions = "cs")
{
    if (File.Exists(rootPath))
    {
        log("pathがファイルでした。ディレクトリを指定してください", ConsoleColor.Red);
        return 1;
    }

    await $"cd {rootPath}";

    var csPaths = await $"find {rootPath} -type f -name '*.{extensions}'";
    foreach (var csPath in csPaths.Split(br))
    {
        string grepResults;
        try
        {
            grepResults = await $"grep -in '[^a-zA-Z]TODO[^a-zA-Z]' '{csPath}'";
        }
        catch (ProcessErrorException)
        {
            continue;
        }

        foreach (var grepResult in grepResults.Split(br))
        {
            var index = grepResult.IndexOf(':');
            var lineNum = grepResult.Substring(0, index);
            var content = grepResult.Substring(index + 1);
            string blameInfos;

            try
            {
                blameInfos = await $"git blame -L {lineNum},+1 {csPath} --porcelain";
            }
            catch (ProcessErrorException)
            {
                continue;
            }

            var author = blameInfos.Split(br).Where(info => info.StartsWith("author")).FirstOrDefault("");
            sb.AppendLine($"{csPath}:{lineNum}:{author}");
            sb.AppendLine(content);
            sb.AppendLine();
        }
    }

    log(sb.ToString(), ConsoleColor.Yellow);
    return 0;
}