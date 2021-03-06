# SH-Sharp
PHP+C# for the command line. Yes, that's right.

## Example
[41 line script file doing the following:](https://github.com/berdon/sh-sharp/blob/master/res/scripts/test3.shs)
![SH-Sharp Example GIF](https://github.com/berdon/sh-sharp/blob/master/res/shsharp.gif?raw=true)

### A simpler example
```c#
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

foreach (var file in (Var.files).Split('\n')) {
    Console.WriteLine(new FileInfo(Path(file)));
}
```

## Output
```shell
echo "How could this be useful?"
How could this be useful?
    echo "I'll give you 0 reasons"
    
I'll give you 0 reasons
    echo "I'll give you 1 reasons"
    
I'll give you 1 reasons
    echo "I'll give you 2 reasons"
    
I'll give you 2 reasons
    echo "I'll give you 3 reasons"
    
I'll give you 3 reasons
    echo "I'll give you 4 reasons"
    
I'll give you 4 reasons
files=$(ls)
/github/sh-sharp/LICENSE
/github/sh-sharp/README.md
/github/sh-sharp/SH-Sharp.sln
/github/sh-sharp/res
/github/sh-sharp/src
/github/sh-sharp/tests
```