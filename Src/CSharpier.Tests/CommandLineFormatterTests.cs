using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace CSharpier.Tests
{
    [TestFixture]
    public class CommandLineFormatterTests
    {
        private MockFileSystem fileSystem;
        private const string UnformattedClass = "public class ClassName { public int Field; }";
        private const string FormattedClass =
            "public class ClassName\n{\n    public int Field;\n}\n";

        [SetUp]
        public void Setup()
        {
            this.fileSystem = new MockFileSystem();
        }

        [Test]
        public void Format_Writes_Failed_To_Compile()
        {
            WhenAFileExists("Invalid.cs", "asdfasfasdf");

            var result = this.Format();

            result.lines.First().Should().Contain(@"Invalid.cs - failed to compile");
        }

        [Test]
        public void Format_Writes_File()
        {
            const string unformattedFilePath = "Unformatted.cs";
            WhenAFileExists(unformattedFilePath, UnformattedClass);

            this.Format();

            this.GetFileContent(unformattedFilePath).Should().Be(FormattedClass);
        }

        [Test]
        public void Format_Supports_Skip_Write()
        {
            const string unformattedFilePath = "Unformatted.cs";
            WhenAFileExists(unformattedFilePath, UnformattedClass);

            this.Format(skipWrite: true);

            this.GetFileContent(unformattedFilePath).Should().Be(UnformattedClass);
        }

        [Test]
        public void Format_Checks_Unformatted_File()
        {
            const string unformattedFilePath = "Unformatted.cs";
            WhenAFileExists(unformattedFilePath, UnformattedClass);

            var (exitCode, lines) = this.Format(check: true);

            exitCode.Should().Be(1);
            this.GetFileContent(unformattedFilePath).Should().Be(UnformattedClass);
            lines.First().Should().Contain(@"Unformatted.cs - was not formatted");
        }

        [Test]
        public void Format_Checks_Formatted_File()
        {
            const string formattedFilePath = "Formatted.cs";
            WhenAFileExists(formattedFilePath, FormattedClass);

            var (exitCode, lines) = this.Format(check: true);

            exitCode.Should().Be(0);
        }

        [TestCase("TemporaryGeneratedFile_Tester.cs")]
        [TestCase("TestFile.designer.cs")]
        [TestCase("TestFile.generated.cs")]
        [TestCase("TestFile.g.cs")]
        [TestCase("TestFile.g.i.cs")]
        public void Format_Skips_Generated_Files(string fileName)
        {
            var unformattedFilePath = fileName;
            WhenAFileExists(unformattedFilePath, UnformattedClass);

            var (_, lines) = this.Format();

            lines.Should().Contain("Total files: 0 ");
        }

        [TestCase("File.cs", "File.cs")]
        [TestCase("File.cs", "*.cs")]
        [TestCase("SubFolder/File.cs", "*.cs")]
        [TestCase("Debug/Logs/File.cs", "**/Logs")]
        [TestCase("Debug/Logs/File.cs", "Logs/")]
        [TestCase("Debug/Logs/File.cs", "Debug/Logs/File.cs")]
        [TestCase(
            @"\Src\CSharpier.Playground\App_Data\Uploads\f45e11a81b926de2af29459af6974bb8.cs",
            "Uploads/")]
        public void File_In_Ignore_Skips_Formatting(string fileName, string ignoreContents)
        {
            var unformattedFilePath = fileName;
            WhenAFileExists(unformattedFilePath, UnformattedClass);
            WhenAFileExists(".csharpierignore", ignoreContents);

            var (_, lines) = this.Format();

            lines.FirstOrDefault(o => o.StartsWith("Total files")).Should().Be("Total files: 0 ");
        }

        [TestCase("SubFolder/File.cs", "*.cs", "SubFolder")]
        [TestCase("SubFolder/File.cs", "SubFolder/File.cs", "SubFolder")]
        public void File_In_Ignore_Skips_Formatting_With_BaseDirectory(
            string fileName,
            string ignoreContents,
            string baseDirectory
        ) {
            var unformattedFilePath = fileName;
            WhenAFileExists(unformattedFilePath, UnformattedClass);
            WhenAFileExists(".csharpierignore", ignoreContents);

            var (_, lines) = this.Format(
                directoryOrFilePaths: Path.Combine(GetRootPath(), baseDirectory)
            );

            lines.FirstOrDefault(o => o.StartsWith("Total files")).Should().Be("Total files: 0 ");
        }

        [Test]
        public void Multiple_Files_Should_Use_Root_Ignore()
        {
            var unformattedFilePath1 = "SubFolder/1/File1.cs";
            var unformattedFilePath2 = "SubFolder/2/File2.cs";
            WhenAFileExists(unformattedFilePath1, UnformattedClass);
            WhenAFileExists(unformattedFilePath2, UnformattedClass);
            WhenAFileExists(".csharpierignore", "Subfolder/**/*.cs");

            var (_, lines) = this.Format(
                directoryOrFilePaths: new[] { unformattedFilePath1, unformattedFilePath2 }
            );

            lines.FirstOrDefault(o => o.StartsWith("Total files")).Should().Be("Total files: 0 ");
        }

        [Test]
        public void Multiple_Files_Should_Use_Multiple_Ignores()
        {
            var unformattedFilePath1 = "SubFolder/1/File1.cs";
            var unformattedFilePath2 = "SubFolder/2/File2.cs";
            WhenAFileExists(unformattedFilePath1, UnformattedClass);
            WhenAFileExists(unformattedFilePath2, UnformattedClass);
            WhenAFileExists("SubFolder/1/.csharpierignore", "File1.cs");
            WhenAFileExists("SubFolder/2/.csharpierignore", "File2.cs");

            var (_, lines) = this.Format(
                directoryOrFilePaths: new[] { unformattedFilePath1, unformattedFilePath2 }
            );

            lines.FirstOrDefault(o => o.StartsWith("Total files")).Should().Be("Total files: 0 ");
        }

        [Test]
        public void Ignore_Should_Deal_With_Inconsistent_Slashes()
        {
            var unformattedFilePath1 = @"SubFolder\1\File1.cs";
            WhenAFileExists(unformattedFilePath1, UnformattedClass);
            WhenAFileExists("SubFolder/1/.csharpierignore", "File1.cs");

            var (_, lines) = this.Format(directoryOrFilePaths: unformattedFilePath1);

            lines.FirstOrDefault(o => o.StartsWith("Total files")).Should().Be("Total files: 0 ");
        }

        [Test]
        public void Ignore_Reports_Errors()
        {
            WhenAFileExists(".csharpierignore", @"\Src\Uploads\*.cs");

            var (exitCode, lines) = this.Format();

            var path = this.fileSystem.Path.Combine(GetRootPath(), ".csharpierignore");

            exitCode.Should().Be(1);
            lines.Should()
                .Contain(
                    $"The .csharpierignore file at {path} could not be parsed due to the following line:"
                );
            lines.Should().Contain(@"\Src\Uploads\*.cs");
        }

        private (int exitCode, IList<string> lines) Format(
            bool skipWrite = false,
            bool check = false,
            params string[] directoryOrFilePaths
        ) {
            if (directoryOrFilePaths.Length == 0)
            {
                directoryOrFilePaths = new[] { GetRootPath() };
            }
            else
            {
                directoryOrFilePaths = directoryOrFilePaths.Select(
                        o => this.fileSystem.Path.Combine(GetRootPath(), o)
                    )
                    .ToArray();
            }

            var fakeConsole = new TestConsole();
            var result =
                CommandLineFormatter.Format(
                    new CommandLineOptions
                    {
                        DirectoryOrFilePaths = directoryOrFilePaths,
                        SkipWrite = skipWrite,
                        Check = check
                    },
                    this.fileSystem,
                    fakeConsole,
                    CancellationToken.None
                ).Result;

            return (result, fakeConsole.Lines);
        }

        private string GetRootPath()
        {
            return OperatingSystem.IsWindows() ? @"c:\test" : "/Test";
        }

        private string GetFileContent(string path)
        {
            path = this.fileSystem.Path.Combine(GetRootPath(), path);
            return this.fileSystem.File.ReadAllText(path);
        }

        private void WhenAFileExists(string path, string contents)
        {
            path = this.fileSystem.Path.Combine(GetRootPath(), path).Replace('\\', '/');
            this.fileSystem.AddFile(path, new MockFileData(contents));
        }

        private class TestConsole : IConsole
        {
            public readonly IList<string> Lines = new List<string>();

            public void WriteLine(string line = null)
            {
                while (line != null && line.Contains("  "))
                {
                    line = line.Replace("  ", " ");
                }

                if (line != null)
                {
                    this.Lines.Add(line);
                }
            }
        }
    }
}