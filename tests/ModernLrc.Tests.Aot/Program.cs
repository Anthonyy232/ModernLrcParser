using System;
using ModernLrc;
using ModernLrc.Model;

const string sample = """
[ti:Demo]
[ar:Tester]
[offset:-150]

[00:01.00]hello world
[00:02.50]F: she sings
[00:03.00]<00:03.00>word <00:03.50>by <00:04.00>word
""";

var parsed = LrcParser.Parse(sample);
if (parsed.HasErrors)
{
    Console.Error.WriteLine("Parse failed:");
    foreach (var d in parsed.Diagnostics)
        Console.Error.WriteLine($"  {d.Code} {d.Severity} L{d.Line}:C{d.Column} — {d.Message}");
    return 1;
}

if (parsed.Document.Lines.Count != 3)
{
    Console.Error.WriteLine($"Expected 3 lines, got {parsed.Document.Lines.Count}");
    return 1;
}

if (parsed.Document.Metadata.Title != "Demo")
{
    Console.Error.WriteLine($"Expected Title=Demo, got '{parsed.Document.Metadata.Title}'");
    return 1;
}

var rendered = LrcWriter.Write(parsed.Document);
var reparsed = LrcParser.Parse(rendered);
if (reparsed.HasErrors)
{
    Console.Error.WriteLine("Round-trip parse failed.");
    return 1;
}

if (reparsed.Document.Lines.Count != 3)
{
    Console.Error.WriteLine($"Round-trip lost lines: expected 3, got {reparsed.Document.Lines.Count}");
    return 1;
}

Console.WriteLine("AOT smoke OK");
return 0;
