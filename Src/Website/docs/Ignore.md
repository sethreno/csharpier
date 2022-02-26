---
title: Ignoring Code
hide_table_of_contents: true
---

Csharpier will ignore the following files
- Any file that begins with ```TemporaryGeneratedFile_```
- Any file that ends with ```.designer.cs```
- Any file that ends with ```.generated.cs```
- Any file that ends with ```.g.cs```
- Any file that ends with ```.g.i.cs```
- Any file that begins with a comment that contains ```<autogenerated``` or ```<auto-generated```

Add a ```.csharpierignore``` file to ignore additional files and folders. The file uses [gitignore syntax](https://git-scm.com/docs/gitignore#_pattern_format)

Example
```
Uploads/
**/App_Data/*.cs
```

Add a `// csharpier-ignore` comment to exclude the next node from formatting. This is valid on statements and members.

```c#

// csharpier-ignore
public class Unformatted     { 
        private string     unformatted;
}

public class ClassName
{
    // csharpier-ignore
    private string    unformatted;

    // csharpier-ignore
    public void MethodName(      ) {
        var unformatted =     "";
}

    public void MethodName()
    {
        // csharpier-ignore
        var unformatted    = true;

        if (true)
        {
            // csharpier-ignore
            var unformatted    = true;
        }
    }
}

```