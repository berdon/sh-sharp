# SH-Sharp
PHP+C# for the command line. Yes, that's right.

## Example
```c#
using System;
using System.IO;

<#zsh
    echo "How could this be useful?"
#>

for (var i = 0; i < 5; i++) {
<#zsh
    echo "I'll give you {i} reasons"
#>
}

<#zsh
files=$(ls)
#>

foreach (var file in (Var["files"] as string).Split('\n')) {
    Console.WriteLine(Path(file));
}
```

## Output
```shell
    echo "I'll give you 6 reasons"
I'll give you 6 reasons
    echo "I'll give you 7 reasons"
I'll give you 7 reasons
    echo "I'll give you 8 reasons"
I'll give you 8 reasons
    echo "I'll give you 9 reasons"
I'll give you 9 reasons
files=$(ls)
/github/sh-sharp/LICENSE
/github/sh-sharp/README.md
/github/sh-sharp/SH-Sharp.sln
/github/sh-sharp/res
/github/sh-sharp/src
/github/sh-sharp/tests
```