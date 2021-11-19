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

System.Console.WriteLine(players.Count);
players = players.GroupBy(p => (p.Positions, p.Club, p.Price))
    .Select(p => p.OrderByDescending(q => q.Value).First())
    .ToList();
System.Console.WriteLine(players.Count);


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
        "price" => (p) => Repeate(p1 => DpDistinct(p.ToList(), p1, int.Parse(args[4]), constraints), Greedy(p, (pl) => (pl.Price, -pl.Value)), int.Parse(args[3])),
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
    ps = ps.OrderByDescending(i => i.Price);
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
            var pl = ps
                .Where(p => p.Club != "Liverpool")
                .Where(p => p.Positions == en.pos)
                .Where(p => clubs![p.Club] > 0)
                .Where(p => !taken.Contains(p))
                .OrderBy(minimize).First();
            clubs![pl.Club]--;
            en.n--;
            taken.Add(pl);
            yield return pl;
        }
    }
}
IEnumerable<Player> DpDistinct2(IEnumerable<Player> players, IEnumerable<Player> selected, int toLeave, List<(string constraintName, Func<IEnumerable<Player>, bool> predicat)> cs) {
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
            var newPs = players.Where(p => p.Price == j)
                .Where(p => clubs[p.Club] < 3)
                .Where(p => positions[p.Positions] < Help.MAX_POS[p.Positions])
                .Where(p => !rooster.Contains(p))
                .OrderBy(p => r.NextInt64());
            var newP = newPs.FirstOrDefault();
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

IEnumerable<Player> DpDistinct( IEnumerable<Player> players, IEnumerable<Player> selected, int toLeave, List<(string constraintName, Func<IEnumerable<Player>, bool> predicat)> cs ) {
    var r = new Random((int)DateTime.Now.Ticks);
    var poss = new List<Positions> {Positions.GK, Positions.DEF, Positions.MID, Positions.FW};

    var thinedOut = poss.Select(pos => selected.OrderBy(i => i.Price)
        .First(p => p.Positions == pos));
    var seconds = thinedOut.ToList();

    thinedOut = seconds.Union( selected
        .Where(i => !seconds.Contains(i))
        .OrderBy(i => r.NextInt64())
        .Take(toLeave)
        .ToList());

    var toGo = 1001-thinedOut.Select(p => p.Price).Sum();
    var clubs = players.GroupBy(p => p.Club).Select(i => i.Key).ToList();
    var clubsIndex = Enumerable.Range(0, clubs.Count).ToDictionary(i => clubs[i], p => p);
    
    var dp = new Dictionary<(MyList teams, MyList positions), (int value, HashSet<int> roster)>?[toGo];
    // dp[0] = new Dictionary<(MyList teams, MyList positions), (int value, HashSet<int> roster)>{
    //     { (clubs.Select(p => 3 - thinedOut.Count(q => q.Club == p)).ToMyList(), poss.Select(p => Help.MAX_POS[p] - thinedOut.Count(q => q.Positions == p)).ToMyList()),
    //       (0, thinedOut.Select(p=> p.Index).ToHashSet()) }
    // };
    var dinit = dp[0] = new();
    var e = Enumerable.Range(0, toLeave).Select(i => 1);
    var not = Enumerable.Range(0, 11 - toLeave).Select(i => 0);
    var init = e.Concat(not).ToMyList();
    System.Console.WriteLine(init.Count());
    var cp = init.ToMyList();
    cp.NextPermutation();
    while(!cp.Equals(init)) {
        var toAdd = cp.Zip(selected.Except(seconds)).Where(i => i.First == 1).Select(i => i.Second).Union(seconds).ToHashSet();
        System.Console.WriteLine(toAdd.Select(i => i.Index).ToMyList());
        System.Console.WriteLine(toAdd.Count);
        var key = (clubs.Select(p => 3 - toAdd.Count(q => q.Club == p)).ToMyList(),
            poss.Select(p => Help.MAX_POS[p] - toAdd.Count(q => q.Positions == p)).ToMyList());
        var v_toAd = toAdd.Sum(p => p.Value);
        if (dinit.TryGetValue(key, out var v)) {
            if (v.value < v_toAd) {
                dinit[key] = (v_toAd, toAdd.Select(p => p.Index).ToHashSet());
            }
        }else {
            dinit.Add(key, (v_toAd, toAdd.Select(p => p.Index).ToHashSet()));
        }
        cp.NextPermutation();
        System.Console.WriteLine(cp);
    }
    System.Console.WriteLine(dp[0].Count);
    // System.Console.WriteLine(dp[0]!.First().Key.teams.Select(p => p.ToString()).Aggregate((i, j) => $"{i},{j}"));
    // System.Console.WriteLine(dp[0]!.First().Key.positions.Select(p => p.ToString()).Aggregate((i, j) => $"{i},{j}"));
    var maxv = 0;
    IEnumerable<Player> maxR = new HashSet<Player>();
    for(int i = 0; i<toGo; ++i) {
        var d = dp[i];
        if (d is null) continue;
        System.Console.WriteLine($"{i}/{toGo}: {d.Count}, {d.Where(i => i.Value.roster.Count < 15).Count()} ");
        var ps = players
                .Where(p => p.Price + i < dp.Length)
                .OrderBy(p => p.Price).ToList();
        int psc = ps.Count, psc_after_tot = 0, new_dicts = 0, dict_hits = 0, dict_reassign = 0, dict_add = 0;
        
        foreach(var kv in d.Where(i => i.Value.roster.Count < 15)) {
            var (tv, pv, v, roster) = (kv.Key.teams, kv.Key.positions, kv.Value.value, kv.Value.roster);
            psc_after_tot +=ps.Count;
            foreach (var newP in ps
                .Where(i => tv[clubsIndex[i.Club]] > 0)
                .Where(p => pv[(int)p.Positions] > 0)
                .Where(p => !roster.Contains(p.Index))
                .ToList())
            {
                var tvn = tv.CopyWithCange(clubsIndex[newP.Club], tv[clubsIndex[newP.Club]] - 1);
                var pvn = pv.CopyWithCange((int)newP.Positions, pv[(int)newP.Positions] - 1);
                if (dp[i+ newP.Price] is not {} dict) {
                    dp[i+ newP.Price] = new Dictionary<(MyList teams, MyList positions), (int value, HashSet<int> roster)> {
                        {(tvn, pvn), (v + newP.Value, roster.Append(newP.Index).ToHashSet())}
                    };
                    new_dicts++;
                }else {
                    
                    if (dict.TryGetValue((tvn, pvn), out var a)) {
                        dict_hits++;
                        if (a.value < newP.Value + v) {
                            dict[(tvn, pvn)] = (v + newP.Value, roster.Append(newP.Index).ToHashSet());
                            dict_reassign++;
                        }
                    }else {
                        dict.Add((tvn, pvn), (v + newP.Value, roster.Append(newP.Index).ToHashSet()));
                        dict_add++;
                    }
                }
            }
        }
        System.Console.WriteLine($"psc={psc}, psc_after_tot={psc_after_tot}, new_dicts={new_dicts}, dict_hits={dict_hits}, dict_reassign={dict_reassign}, dict_add={dict_add}");
        d.GroupBy(ddd => ddd.Value.roster.Count)
            .ToList()
            .ForEach(g => System.Console.WriteLine($"{g.Key} => {g.Count()}"));
        foreach(var kv in d.Where(ddd => ddd.Value.roster.Count == 15)) {
            if (maxv < kv.Value.value)
            {
                var rr = kv.Value.roster.Select(p => players.First(pp => pp.Index == p));
                maxv = kv.Value.value;
                maxR = rr;
            }
        }
        d.Clear();
        dp[i] = null;
    }
    return maxR;
}

IEnumerable<Player> Repeate(Func<IEnumerable<Player>, IEnumerable<Player>> gether, IEnumerable<Player> init, int times) {
    var best = 0; //init.Select(i => i.Value).Sum();
    Print(init, constraints);
    while(--times >= 0) {
        System.Console.WriteLine("================================");
        System.Console.WriteLine($"{times}");
        var ninit = gether(init);
        if (constraints.All(i => i.predicat(ninit))) {
            init = ninit;
            var score = init.Select(i => i.Value).Sum();
            if (score > best) {
                Print(init, constraints);
                best = score;
            }else if (score == best) {
                System.Console.WriteLine($"Same solution {score}");
            }else {
                System.Console.WriteLine($"Worse solution {score}");
            }
        }else {
            System.Console.WriteLine("Bad solution");
        }

        System.Console.WriteLine("================================");
    }
    return init;
}
public record Player(int Index, Positions Positions, string Name, string Club, int Value, int Price);
public enum Positions {
    GK = 0,
    DEF = 1,
    MID = 2,
    FW = 3
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
    public static MyList ToMyList(this IEnumerable<int> e) {
        var l = new MyList();
        l.AddRange(e);
        return l;
    }
}
public static class Solution {
    public static void NextPermutation(this MyList nums) {
        // Starting from the end, look for the first index that is not in descending order
        var startIndex = nums.Count - 2;
 
        while (startIndex >= 0) {
            // Find first decreasing element
            if (nums[startIndex] < nums[startIndex + 1]) {
                break;
            }
            --startIndex;
        }
 
        if (startIndex >= 0) {
            // Starting from the end, look for the first element greater than the start index
            int endIndex = nums.Count - 1;
 
            while (endIndex > startIndex) {
                // Find first greater element
                if (nums[endIndex] > nums[startIndex]) {
                    break;
                }
                --endIndex;
            }
 
            // Swap first and last elements
            Swap(nums, startIndex, endIndex);
        }
 
        // Reverse the array
        Reverse(nums, startIndex + 1);
    }
 
    private static void Reverse(MyList nums, int startIndex) {
        for (int start = startIndex, end = nums.Count - 1; start < end; ++start, --end) {
            Swap(nums, start, end);
        }
    }
 
    private static void Swap(MyList ar, int a, int b) {
        var tmp = ar[a];
        ar[a] = ar[b];
        ar[b] = tmp;
    }
}

public class MyList : List<int> {
    public override int GetHashCode( ) {
        var hc = Count.GetHashCode();
        for ( int i = 0 ; i < this.Count ; ++i )
            hc = HashCode.Combine( hc, i, this[i] );
        return hc;
    }
    public override bool Equals( object? obj ) {
        if (obj is not MyList l) return false;
        if (l.Count != this.Count) return false;
        for ( int i = 0 ;i < this.Count ; ++i ) {
            if (this[i] != l[i]) return false;
        }
        return true;
    }
    public MyList CopyWithCange(int index, int newValue) {
        var a = this.ToMyList();
        a[index] = newValue;
        return a;
    }
    public override string ToString()
    {
        return this.Select(i => i.ToString()).Aggregate((i,j) => $"{i}, {j}");
    }
}