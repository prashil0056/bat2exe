using System;
using System.IO;
using System.Diagnostics;

class Bat2Exe
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: bat2exe <input.bat> <output.exe> [--hidden]");
            return 1;
        }

        string input  = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[1]);
        bool hidden   = args.Length > 2 && args[2] == "--hidden";

        if (!File.Exists(input))
        {
            Console.Error.WriteLine("input not found: " + input);
            return 1;
        }

        byte[] scriptBytes = File.ReadAllBytes(input);
        string stub        = hidden ? StubSourceHidden : StubSourceVisible;

        string tempDir = Path.Combine(Path.GetTempPath(), "bat2exe_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string stubPath   = Path.Combine(tempDir, "stub.cs");
            string scriptPath = Path.Combine(tempDir, "payload.bin");
            string stubExe    = Path.Combine(tempDir, "stub.exe");

            File.WriteAllText(stubPath, stub);
            File.WriteAllBytes(scriptPath, scriptBytes);

            string compiler = FindCompiler();
            string compilerArgs =
                "-nologo -target:exe -out:\"" + stubExe + "\" " +
                "-resource:\"" + scriptPath + "\",payload.bin " +
                "-r:System.dll -r:System.Core.dll " +
                "\"" + stubPath + "\"";

            var psi = new ProcessStartInfo
            {
                FileName               = compiler,
                Arguments              = compilerArgs,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };

            using (var p = Process.Start(psi))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    Console.Error.WriteLine("compile failed:");
                    Console.Error.WriteLine(stdout);
                    Console.Error.WriteLine(stderr);
                    return 1;
                }
            }

            File.Copy(stubExe, output, true);
            Console.WriteLine("built: " + output);
            return 0;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    static string FindCompiler()
    {
        string[] monoCandidates = new[]
        {
            "/Library/Frameworks/Mono.framework/Versions/Current/bin/mcs",
            "/usr/local/bin/mcs",
            "/opt/homebrew/bin/mcs",
            "/usr/bin/mcs"
        };
        foreach (var c in monoCandidates)
            if (File.Exists(c)) return c;

        string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrEmpty(win))
        {
            string[] winCandidates = new[]
            {
                Path.Combine(win, @"Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
                Path.Combine(win, @"Microsoft.NET\Framework\v4.0.30319\csc.exe")
            };
            foreach (var c in winCandidates)
                if (File.Exists(c)) return c;
        }

        return "mcs";
    }

    const string StubSourceVisible = @"
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

class Program
{
    static int Main()
    {
        var asm = Assembly.GetExecutingAssembly();
        using (var s = asm.GetManifestResourceStream(""payload.bin""))
        {
            if (s == null) return 1;

            string bat = Path.Combine(Path.GetTempPath(), ""b2e_"" + Guid.NewGuid().ToString(""N"") + "".bat"");
            using (var fs = File.Create(bat)) s.CopyTo(fs);

            var psi = new ProcessStartInfo
            {
                FileName        = ""cmd.exe"",
                Arguments       = ""/c \"""" + bat + ""\"""",
                UseShellExecute = false,
                CreateNoWindow  = false
            };

            var p = Process.Start(psi);
            p.WaitForExit();
            int code = p.ExitCode;

            try { File.Delete(bat); } catch { }
            return code;
        }
    }
}
";

    const string StubSourceHidden = @"
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        var asm = Assembly.GetExecutingAssembly();
        using (var s = asm.GetManifestResourceStream(""payload.bin""))
        {
            if (s == null) return;

            string bat = Path.Combine(Path.GetTempPath(), ""b2e_"" + Guid.NewGuid().ToString(""N"") + "".bat"");
            using (var fs = File.Create(bat)) s.CopyTo(fs);

            var psi = new ProcessStartInfo
            {
                FileName        = ""cmd.exe"",
                Arguments       = ""/c \"""" + bat + ""\"""",
                UseShellExecute = false,
                CreateNoWindow  = true,
                WindowStyle     = ProcessWindowStyle.Hidden
            };

            var p = Process.Start(psi);
            p.WaitForExit();

            try { File.Delete(bat); } catch { }
        }
    }
}
";
}
