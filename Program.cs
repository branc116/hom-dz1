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


var constraints = new List<(string constraintName, Func<IEnumerable<Player>, bool> predicat)> {
    ("Player count must be 15",   (ps) => ps.Count() == 15),
    ("There must be 2 goalkeepers", (ps) => ps.Count(i => i.Positions == Positions.GK) == 2),
    ("There must be 5 defs", (ps) => ps.Count(i => i.Positions == Positions.DEF) == 5),
    ("There must be 5 mids", (ps) => ps.Count(i => i.Positions == Positions.MID) == 5),
    ("There must be 3 fwd", (ps) => ps.Count(i => i.Positions == Positions.FW) == 3),
    ("There must be at most 3 players from the same club.", (ps) => ps.GroupBy(i => i.Club).All(i => i.Count() <= 3)),
    ("Can spend max 100 M€.", (ps) => ps.Sum(i => i.Price) <= 1000) // initially multiplied by 10
};

Func<IEnumerable<Player>, IEnumerable<Player>> algorith = args[1] switch {
    "greedy" => args[2] switch {
        "price" => (p) => Greedy(p, (pl) => pl.Price),
        "value" => (p) => Greedy(p, (pl) => -pl.Value),
        "pv" => (p) => Greedy(p, (pl) => (double)pl.Price/(pl.Value + 1)),
        string s => throw new Exception($"Greedy {s} not Implementd"),
        _ => throw new Exception("Fatal Error")
    },
    "dp" => args[2] switch {
        "price" => (p) => Repeate(p1 => DpDistinct(p.ToList(), p1, int.Parse(args[4]), constraints), Greedy(p, (pl) => pl.Price), int.Parse(args[3])),
        "value" => (p) => Repeate(p1 => DpDistinct(p.ToList(), p1, int.Parse(args[4]), constraints), Greedy(p, (pl) => -pl.Value), int.Parse(args[3])),
        "pv" => (p) => Repeate(p1 => DpDistinct(p.ToList(), p1, int.Parse(args[4]), constraints), Greedy(p, (pl) => (double)pl.Price/(pl.Value + 1)), int.Parse(args[3])),
        string s => throw new Exception($"Dp {s} not Implementd"),
        _ => throw new Exception("Fatal Error")
    },
    _ => throw new Exception("Not implemented")
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
    var main = ps.Where(p => p.Positions == Positions.GK).Take(1).Union(
        ps.Where(p => p.Positions == Positions.FW).Take(2)
    ).Union(
        ps.Where(p => p.Positions == Positions.MID).Take(4)
    ).Union(
        ps.Where(p => p.Positions == Positions.DEF).Take(4)
    );
    var back = ps.Except(main);
    var ms=main.Select(i => i.Index.ToString()).Aggregate((i,j)=>$"{i},{j}");
    var bs=back.Select(i => i.Index.ToString()).Aggregate((i,j)=>$"{i},{j}");
    System.Console.WriteLine(ms);
    System.Console.WriteLine(bs);
    System.Console.WriteLine($"Total value: {ps.Select(i => i.Value).Sum()}, Total cost: {ps.Select(p => p.Price).Sum()}");
    System.Console.WriteLine("===============================================\n");
}


IEnumerable<Player> Greedy<T>(IEnumerable<Player> ps, Func<Player, T> minimize) {
    var clubs = ps.GroupBy(p => p.Club).ToDictionary(p => p.Key, p => 3);
    var taken = new HashSet<Player>();
    return new []{(2, Positions.GK), (5, Positions.DEF), (5, Positions.MID), (3, Positions.FW)}.SelectMany(selMany).ToList();

    IEnumerable<Player> selMany((int n, Positions pos) en) {
        while(en.n > 0) {
            System.Console.WriteLine($"Taking {en.pos}");
            var pl = ps.Where(p => p.Positions == en.pos).Where(p => clubs![p.Club] > 0)
                .Where(p => !taken.Contains(p))
                .OrderBy(minimize).First();
            clubs![pl.Club]--;
            en.n--;
            taken.Add(pl);
            yield return pl;
        }
    }
}
IEnumerable<Player> DpDistinct(IEnumerable<Player> players, IEnumerable<Player> selected, int toLeave, List<(string constraintName, Func<IEnumerable<Player>, bool> predicat)> cs) {
    var r = new Random((int)DateTime.Now.Ticks);
    var thinedOut = selected.OrderBy(i => r.NextInt64()).Take(toLeave).ToHashSet(). ToList();
    var toGo = 1000-thinedOut.Select(p => p.Price).Sum();
    var dp = new (int, HashSet<Player>, Dictionary<string, int>, Dictionary<Positions, int>)[toGo];
    dp[0] = (0, thinedOut.ToHashSet(),
        players.GroupBy(p => p.Club).ToDictionary(p => p.Key, p => thinedOut.Count(j => j.Club == p.Key)),
        players.GroupBy(p => p.Positions).ToDictionary(p => p.Key, p => thinedOut.Count(j => j.Positions == p.Key)));
    var (bestv, besti) = (0, -1);
    for (int i = 1; i < toGo;++i) {
        dp[i] = dp[i - 1];
        for (int j = 1; j <= i; ++j) {
            var (value, rooster, clubs, positions) = dp[i - j];
            var newP = players.Where(p => p.Price == j)
                .Where(p => clubs[p.Club] < 3)
                .Where(p => positions[p.Positions] < Help.MAX_POS[p.Positions])
                .Where(p => !rooster.Contains(p))
                .OrderBy(p => r.NextInt64()).FirstOrDefault();
            if (newP is not null && dp[i].Item1 < value + newP.Value){
                var nClubs = new Dictionary<string, int>(clubs);
                nClubs[newP.Club]++;
                var nPositions = new Dictionary<Positions, int>(positions);
                nPositions[newP.Positions]++;
                dp[i] = (value + newP.Value, rooster.Append(newP).ToHashSet(), nClubs, nPositions);
            }
        }
        if (dp[i].Item1 > bestv && cs.Select(c => c.predicat(dp[i].Item2)).All(val => val)){
            bestv = dp[i].Item1;
            besti = i;
        }
    }
    if (besti == -1)
        return selected;
    return dp[besti].Item2.ToList();
}

IEnumerable<Player> Repeate(Func<IEnumerable<Player>, IEnumerable<Player>> gether, IEnumerable<Player> init, int times) {
    var best = 0; //init.Select(i => i.Value).Sum();
    Print(init, constraints);
    while(--times >= 0) {
        init = gether(init);
        var score = init.Select(i => i.Value).Sum();
        if (score > best) {
            Print(init, constraints);
            best = score;
        }
    }
    return init;
}
public record Player(int Index, Positions Positions, string Name, string Club, int Value, int Price);
public enum Positions {
    GK,
    DEF,
    MID,
    FW
}
public static class Help {
    public static Dictionary<Positions, int> MAX_POS = new Dictionary<Positions, int>{
        {Positions.GK, 2},
        {Positions.DEF, 5},
        {Positions.MID, 5},
        {Positions.FW, 3}
    };
    public static void Print<T, K>(this IDictionary<T, K> d) {
        foreach(var kv in d) {
            System.Console.WriteLine($"{kv.Key} => {kv.Value}");
        }
    }
}