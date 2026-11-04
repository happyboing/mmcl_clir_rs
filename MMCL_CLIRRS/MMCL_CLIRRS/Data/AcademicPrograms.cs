namespace MMCL_CLIRRS;

public static class AcademicPrograms
{
    public static readonly string[] Departments =
    {
        "CAS", "CCIS", "CHS", "CMET", "ETYCB", "MIA", "MITL", "SHS"
    };

    private static readonly Dictionary<string, string[]> Programs =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["CAS"] = new[] { "BA COMM", "BMMA" },
            ["CCIS"] = new[] { "IT", "CS" },
            ["CHS"] = new[] { "BIO", "BSN", "PSY", "MEDTECH", "PT", "PHARMA" },
            ["CMET"] = new[] { "MARE", "MT" },
            ["ETYCB"] = new[] { "ACT", "AIS", "BA FM", "BA OM", "BA MM", "HM", "TM", "MKT" },
            ["MIA"] = new[] { "AEE", "AMT", "AM" },
            ["MITL"] = new[] { "AR", "CE", "CHE", "COE", "ECE", "EE", "IE", "ME" },
            ["SHS"] = new[] { "BAE", "ASSH", "ICT", "STEM" }
        };

    public static IReadOnlyList<string> GetPrograms(string department)
        => Programs.TryGetValue(department, out string[]? programs)
            ? programs
            : Array.Empty<string>();
}
