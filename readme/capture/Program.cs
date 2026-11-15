// Runs each README snippet against the library and writes ../inputs/<name>.cs.txt (the region's
// code, as shown) and ../inputs/<name>.out.txt (what it printed). build.cjs renders the panels from them.
using System.Runtime.CompilerServices;
using ModernLrc;
using ModernLrc.Model;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

Capture("hero", () =>
{
    #region hero
    var doc = LrcParser.ParseFile("song.lrc").Document;

    var line = doc.FindLineAt(TimeSpan.FromSeconds(40));
    Console.WriteLine($"{line?.EffectiveVoice}: {line?.GetText()}");
    #endregion
});

Capture("parse", () =>
{
    #region parse
    var result = LrcParser.Parse("""
        [ti:Paper Lanterns]
        [ar:The Tide Pools]
        [00:12.00]Paper lanterns on the water
        [00:17.40]Drifting where the river bends
        [00:23.10][01:04.60]Carry me home
        """);

    var doc = result.Document;
    Console.WriteLine($"{doc.Metadata.Title} by {doc.Metadata.Artist}");
    foreach (var line in doc.Lines)
        Console.WriteLine($"{line.Timestamp}  {line.GetText()}");
    #endregion
});

Capture("karaoke", () =>
{
    #region karaoke
    var doc = LrcParser.Parse("""
        [00:23.10]<00:23.10>Carry <00:23.80>me <00:24.30>home
        """).Document;

    var now = TimeSpan.FromSeconds(24);
    if (doc.FindLineAt(now) is LrcEnhancedLine line)
        foreach (var word in line.Words)
        {
            var at = doc.GetEffectiveTime(word.Timestamp);
            string state = at <= now ? "sung" : "";
            Console.WriteLine($"{word.Timestamp}  {word.Text,-6} {state}");
        }
    #endregion
});

Capture("duet", () =>
{
    #region duet
    var doc = LrcParser.Parse("""
        [00:30.00]M: I kept the porch light on
        [00:34.20]All through the winter
        [00:38.50]F: I saw it from the road
        [00:42.90]D: So carry me home
        """).Document;

    foreach (var line in doc.Lines)
        Console.WriteLine($"{line.EffectiveVoice,-7} {line.GetText()}");
    #endregion
});

Capture("roundtrip", () =>
{
    #region roundtrip
    var doc = LrcParser.Parse("""
        [ti:Paper Lanterns]
        [offset:250]
        [00:12.00]Paper lanterns on the water
        [00:23.10][01:04.60]F: Carry me home
        [01:10.00]<01:10.00>Carry <01:10.70>me <01:11.20>home
        """).Document;

    var shifted = new LrcDocumentBuilder(doc)
        .ShiftAll(TimeSpan.FromSeconds(1.5))
        .Build();

    string lrc = LrcWriter.Write(shifted);
    Console.Write(lrc);
    Console.WriteLine(LrcParser.Parse(lrc).Document == shifted);
    #endregion
});

File.Copy("song.lrc", Path.Combine(Inputs(), "song.lrc.txt"), overwrite: true);

static string Inputs([CallerFilePath] string source = "") => Path.Combine(Path.GetDirectoryName(source)!, "..", "inputs");

static void Capture(string name, Action snippet, [CallerFilePath] string source = "")
{
    var lines = File.ReadAllLines(source);
    var start = Array.FindIndex(lines, l => l.Trim() == $"#region {name}") + 1;
    var body = lines[start..Array.FindIndex(lines, start, l => l.Trim() == "#endregion")];
    var indent = body.Where(l => l.Length > 0).Min(l => l.Length - l.TrimStart().Length);
    File.WriteAllText(Path.Combine(Inputs(), $"{name}.cs.txt"), string.Join('\n', body.Select(l => l.Length > 0 ? l[indent..] : l)) + "\n");

    var stdout = Console.Out;
    var output = new StringWriter();
    Console.SetOut(output);
    snippet();
    Console.SetOut(stdout);
    File.WriteAllText(Path.Combine(Inputs(), $"{name}.out.txt"), output.ToString().ReplaceLineEndings("\n"));
}
