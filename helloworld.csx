#! "netcoreapp2.0"
#r "nuget:NetStandard.Library,2.0.0"

using System.Xml;

/// <summary>
/// Sequence point ID.
/// </summary>
struct SeqId
{
    public int Value { get; }

    public SeqId(int value) => Value = value;

    public override int GetHashCode() => Value;

    public override bool Equals(object obj) => obj != null && obj is SeqId && ((SeqId)obj).Value == this.Value;
}

struct TestId
{
    public int Value { get; }

    public TestId(int value) => Value = value;

    public override int GetHashCode() => Value;

    public override bool Equals(object obj) => obj != null && obj is TestId && ((TestId)obj).Value == this.Value;
}

class ModuleCoverage
{
    public string ModuleName { get; set; }

    public int? NumSequencePoints { get; set; }

    public int? VisitedSequencePoints { get; set; }

    public Dictionary<TestId, HashSet<SeqId>> Visits { get; set; }
        = new Dictionary<TestId, HashSet<SeqId>>();
}

// Extracting relevant data from the OpenCover output

var modules = new List<ModuleCoverage>();

using (var xml = XmlReader.Create("C:/Source.git/roslyn/opencoveroutput3.xml"))
{
    SeqId? currentSeqId = null;

    while (xml.Read())
    {
        if (xml.NodeType != XmlNodeType.Element)
        {
            continue;
        }

        if (xml.Name == "Module")
        {
            if (xml.GetAttribute("skippedDueTo") == null)
            {
                modules.Add(new ModuleCoverage());
            }
            else
            {
                xml.Skip(); // if skipped, then skip
            }
            
            continue;
        }

        if (xml.Name == "ModuleName")
        {
            modules.Last().ModuleName = xml.ReadElementContentAsString();
            continue;
        }

        if (   xml.Name == "Summary"
            && modules.Any()
            && !modules.Last().NumSequencePoints.HasValue)
        {
            xml.MoveToAttribute("numSequencePoints");
            modules.Last().NumSequencePoints = xml.ReadContentAsInt();

            xml.MoveToAttribute("visitedSequencePoints");
            modules.Last().VisitedSequencePoints = xml.ReadContentAsInt();

            continue;
        }

        if (xml.Name == "SequencePoint")
        {
            xml.MoveToAttribute("uspid");
            currentSeqId = new SeqId(xml.ReadContentAsInt());

            continue;
        }

        if (   xml.Name == "TrackedMethodRef"
            && currentSeqId.HasValue)
        {
            xml.MoveToAttribute("uid");
            var currentTestId = new TestId(xml.ReadContentAsInt());

            var currentModule = modules.Last();
            if (currentModule.Visits.ContainsKey(currentTestId))
            {
                currentModule.Visits[currentTestId].Add(currentSeqId.Value);
            }
            else
            {
                currentModule.Visits.Add(currentTestId, new HashSet<SeqId>{ currentSeqId.Value });
            }

            continue;
        }
    }
}

// Merging modules observed from multiple test assemblies

modules = modules.GroupBy(m => m.ModuleName).Select(mg => {
    var visitsGrouping = mg.SelectMany(m => m.Visits.AsEnumerable()).GroupBy(kv => kv.Key);
    var visits = visitsGrouping.ToDictionary(
        kvg => kvg.Key,
        kvg => new HashSet<SeqId>(kvg.Select(kv => kv.Value.AsEnumerable()).SelectMany(kv => kv)));

    return new ModuleCoverage(){
        ModuleName = mg.Key,
        NumSequencePoints = mg.First().NumSequencePoints,
        VisitedSequencePoints = 0,
        Visits = visits
    };
}).ToList();

// Computing partial coverages for tests limited by increasingly growing maximum size

foreach (var module in modules)
{
    if (module.ModuleName.EndsWith(".UnitTests"))
    {
        continue;
    }

    var totalSeqPoints = module.Visits.Values.SelectMany(v => v).Distinct().Count();

    if (totalSeqPoints < 5000)
    {
        continue;
    }

    Console.WriteLine(Environment.NewLine + module.ModuleName + Environment.NewLine);

    var thresholdExponentMaximum = 10M;
    var thresholdMaximum = Math.Pow(2.0, (double)thresholdExponentMaximum);
    for (var thresholdExponent = 0.1M; thresholdExponent <= thresholdExponentMaximum; thresholdExponent += 0.1M)
    {
        var threshold = Math.Pow(2.0, (double)thresholdExponent);

        // What is maximum number of points for test for this threshold?
        var cutPoint = (int)(threshold / thresholdMaximum * module.NumSequencePoints);

        // What points are covered by tests with the coverage under the current cutPoint?
        var visited = new HashSet<SeqId>();

        foreach (var v in module.Visits.Values.Where(v => v.Count <= cutPoint))
        {
            visited.UnionWith(v);
        }

        Console.WriteLine($"{cutPoint}, {visited.Count * 10000L / totalSeqPoints}");
    }
}