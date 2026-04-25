using System;
using System.IO;
using ModernLrc;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: lrc <command> [args]");
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  parse <file.lrc>          Parse a file and print summary");
    Console.Error.WriteLine("  shift <file.lrc> <ms>     Shift all timestamps and print result");
    return 1;
}

switch (args[0])
{
    case "parse":
        if (args.Length < 2) { Console.Error.WriteLine("parse needs <file.lrc>"); return 1; }
        return RunParse(args[1]);

    case "shift":
        if (args.Length < 3) { Console.Error.WriteLine("shift needs <file.lrc> <ms>"); return 1; }
        if (!int.TryParse(args[2], out int ms)) { Console.Error.WriteLine("ms must be an integer"); return 1; }
        return RunShift(args[1], ms);

    default:
        Console.Error.WriteLine($"Unknown command: {args[0]}");
        return 1;
}

static int RunParse(string path)
{
    if (!File.Exists(path)) { Console.Error.WriteLine($"File not found: {path}"); return 1; }
    var result = LrcParser.ParseFile(path);
    Console.WriteLine($"Title:    {result.Document.Metadata.Title ?? "(none)"}");
    Console.WriteLine($"Artist:   {result.Document.Metadata.Artist ?? "(none)"}");
    Console.WriteLine($"Lines:    {result.Document.Lines.Count}");
    Console.WriteLine($"Offset:   {result.Document.Metadata.Offset.TotalMilliseconds}ms");
    Console.WriteLine($"HasErrors:{result.HasErrors}");
    if (result.Diagnostics.Length > 0)
    {
        Console.WriteLine("Diagnostics:");
        foreach (var d in result.Diagnostics)
            Console.WriteLine($"  {d.Code} {d.Severity} L{d.Line}:C{d.Column} — {d.Message}");
    }
    return result.HasErrors ? 1 : 0;
}

static int RunShift(string path, int ms)
{
    if (!File.Exists(path)) { Console.Error.WriteLine($"File not found: {path}"); return 1; }
    var result = LrcParser.ParseFile(path);
    if (result.HasErrors) { Console.Error.WriteLine("Parse failed; not shifting."); return 1; }
    var shifted = new LrcDocumentBuilder(result.Document)
        .ShiftAll(TimeSpan.FromMilliseconds(ms))
        .Build();
    Console.Write(LrcWriter.Write(shifted));
    return 0;
}
