



var lines = await System.IO.File.ReadAllLinesAsync(args[0]);
var players = lines
    .Select(i => i.Split(','))
    .Select(el => new Player (
        int.Parse(el[0]),
        Enum.Parse<Positions>(el[1]),
        el[2],
        el[3],
        int.Parse(el[4]),
        (int)(double.Parse(el[5]) * 10))
    ).ToList();

Func<IEnumerable<Player>, IEnumerable<Player>> algorith = args[1] switch {
    "greedy" => args[2] switch {
        "price" => (p) => Greedy(p, (pl) => pl.Price),
        "value" => (p) => Greedy(p, (pl) => -pl.Value),
        "pv" => (p) => Greedy(p, (pl) => (double)pl.Price/(pl.Value + 1)),
        string s => throw new Exception($"Greedy {s} not Implementd"),
        _ => throw new Exception("Fatal Error")
    },
    _ => throw new Exception("Not implemented")
};



var constraints = new List<(string constraintName, Func<IEnumerable<Player>, bool> predicat)> {
    ("Player count must be 15",   (ps) => ps.Count() == 15),
    ("There must be 2 goalkeepers", (ps) => ps.Count(i => i.Positions == Positions.GK) == 2),
    ("There must be 5 defs", (ps) => ps.Count(i => i.Positions == Positions.DEF) == 5),
    ("There must be 5 mids", (ps) => ps.Count(i => i.Positions == Positions.MID) == 5),
    ("There must be 3 fwd", (ps) => ps.Count(i => i.Positions == Positions.FW) == 3),
    ("There must be at most 3 players from the same club.", (ps) => ps.GroupBy(i => i.Club).All(i => i.Count() <= 3)),
    ("Can spend max 100 M€.", (ps) => ps.Sum(i => i.Price) <= 1000) // initially multiplied by 10
};

Print(algorith(players), constraints);

void Print(IEnumerable<Player> ps, List<(string constraintName, Func<IEnumerable<Player>, bool> predicat)> cs) {
    var cs_ns = cs.Where(c => !c.predicat(ps)).ToList();
    if (!cs_ns.Any())
        System.Console.WriteLine($"All constraints sattisfied!");
    foreach(var c in cs_ns) {
        System.Console.WriteLine($"Constraint `{c.constraintName}` not sattisfied");
    }
    System.Console.WriteLine("\n===============================================\n");
    ps.GroupBy(i => i.Positions)
        .ToList()
        .ForEach(el =>  {
            System.Console.WriteLine(el.Key);
            el.ToList().ForEach(System.Console.WriteLine);
            System.Console.WriteLine("-----------------------------------");
        });
    System.Console.WriteLine("===============================================\n");
}


IEnumerable<Player> Greedy<T>(IEnumerable<Player> ps, Func<Player, T> minimize) {
    var clubs = ps.GroupBy(p => p.Club).ToDictionary(p => p.Key, p => 3);
    return new []{(2, Positions.GK), (5, Positions.DEF), (5, Positions.MID), (3, Positions.FW)}.SelectMany(selMany).ToList();

    IEnumerable<Player> selMany((int n, Positions pos) en) {
        while(en.n > 0) {
            System.Console.WriteLine($"Taking {en.pos}");
            var pl = ps.Where(p => p.Positions == en.pos).Where(p => clubs![p.Club] > 0).OrderBy(minimize).First();
            clubs![pl.Club]--;
            en.n--;
            yield return pl;
        }
    }

}
public record Player(int Index, Positions Positions, string Name, string Club, int Value, int Price);
public enum Positions {
    GK,
    DEF,
    MID,
    FW
}