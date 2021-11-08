# Make a best team from a list of players

## How to use

``` > dotnet run -- <path to csv file that contains all of the players> <algorithm name> <algorithm flags...> ```

### List of algorithms
* Greedy
    * ``` > dotnet run -- <path to csv file that contains all of the players> greedy (price|value|pv) ```
    * optimize *price* or *value* or expression *price/value* (**pv**)