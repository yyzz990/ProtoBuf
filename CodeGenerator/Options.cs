using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace SilentOrbit.ProtocolBuffers
{
    /// <summary>
    /// Options set using Command Line arguments
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Show the help
        /// </summary>
        [Option('h', "help", HelpText = "Show this help")]
        public bool ShowHelp { get; set; }

        /// <summary>
        /// Convert message/class and field/propery names to CamelCase
        /// </summary>
        [Option("preserve-names",
            HelpText =
                "Keep names as written in .proto, otherwise class and field names by default are converted to CamelCase"
            )]
        public bool PreserveNames { get; set; }

        /// <summary>
        /// If false, an error will occur.
        /// </summary>
        [Option("fix-nameclash",
            HelpText =
                "If a property name is the same as its class name or any subclass the property will be renamed. if the name clash occurs and this flag is not set, an error will occur and the code generation is aborted."
            )]
        public bool FixNameclash { get; set; }

        /// <summary>
        /// Generated code indent using tabs
        /// </summary>
        [Option('t', "use-tabs", HelpText = "If set generated code will use tabs rather than 4 spaces.")]
        public bool UseTabs { get; set; }

        /// <summary>
        /// Path to the source dir where the protofiles are located (and where imports will be searched)
        /// </summary>
        [Option("src-dir", Required = false,
            HelpText =
                "Directory where the proto files reside, where the dependencies among protos will be searched. the current directory is used if you don't provide a value"
            )]
        public string SourceDir { get; set; }

        /// <summary>
        /// List of the protos that will be generated  (accepts wildcard and search recursively in the directory provided)
        /// </summary>
        [Value(0, Required = true)]
        public IEnumerable<string> InputProto { get; set; }

        /// <summary>
        /// Path to the generated cs files
        /// </summary>
        [Option('o', "output", Required = false,
            HelpText =
                "Folder where the generated .cs files will be placed.  default output folder in the current directory is used if you don't provide a value"
            )]
        public string OutputPath { get; set; }


        /// <summary>
        /// Use experimental stack per message type
        /// </summary>
        [Option("experimental-message-stack",
            HelpText =
                "Assign the name of the stack implementatino to use for each message type, included options are ThreadSafeStack, ThreadUnsafeStack, ConcurrentBagStack or the full namespace to your own implementation."
            )]
        public string ExperimentalStack { get; set; }

        /// <summary>
        /// If set default constructors will be generated for each message
        /// </summary>
        [Option("ctor", HelpText = "Generate constructors with default values.")]
        public bool GenerateDefaultConstructors { get; set; }

        /// <summary>
        /// Use Nullable&lt;&gt; for optional fields
        /// </summary>
        [Option("nullable", Required = false, HelpText = "Generate nullable primitives for optional fields")]
        public bool Nullable { get; set; }

        /// <summary>
        /// Exclude .NET 4 code
        /// </summary>
        [Option("net2", Required = false, HelpText = "Exclude code that require .NET 4")]
        public bool Net2 { get; set; }

        /// <summary>
        /// De/serialize DateTime as UTC only
        /// </summary>
        [Option("utc", Required = false, HelpText = "De/serialize DateTime as DateTimeKind.Utc")]
        public bool Utc { get; set; }

        /// <summary>
        /// Add the [Serializable] attribute to generated classes
        /// </summary>
        [Option("serializable", Required = false, HelpText = "Add the [Serializable] attribute to generated classes")]
        public bool SerializableAttributes { get; set; }

        /// <summary>
        /// Skip serializing properties having the default value.
        /// </summary>
        [Option("skip-default", Required = false, HelpText = "Skip serializing properties having the default value.")]
        public bool SkipSerializeDefault { get; set; }

        /// <summary>
        /// Do not output ProtocolParser.cs
        /// </summary>
        [Option("no-protocolparser", Required = false, HelpText = "Don't output ProtocolParser.cs")]
        public bool NoProtocolParser { get; set; }

        /// <summary>
        /// Don't generate code from imported .proto files.
        /// </summary>
        [Option("no-generate-imported", Required = false, HelpText = "Don't generate code from imported .proto files.")]
        public bool NoGenerateImported { get; set; }

        [Option("use-interface", Required = false, HelpText = "Add interface to generated code")]
        public bool UseInterface { get; set; }

        [Option("split-output", Required = false, HelpText = "Proto messages are splitted in single files")]
        public bool SplitOutput { get; set; }

        public static Options Parse(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            var options = result.Value;
            if (result.Errors.Any())
                return null;

            if (args == null || args.Length == 0 || options.ShowHelp)
            {
                Console.Error.WriteLine(options.GetUsage());
                return null;
            }

            bool error = false;

            //Do any extra option checking/cleanup here
            if (options.InputProto == null)
            {
                Console.Error.WriteLine("Missing input .proto arguments.");
                return null;
            }

            Console.WriteLine("--input =  " + options.InputProto);
            var inputs = ExpandFileWildCard(options.InputProto);
            options.InputProto = inputs;
            foreach (var input in inputs)
            {
                if (File.Exists(input) == false)
                {
                    Console.Error.WriteLine("File not found: " + input);
                    error = true;
                }
            }
            Console.WriteLine("ProtoFiles =  " + options.InputProto);

            if (options.SourceDir == null)
            {
                options.SourceDir = Directory.GetCurrentDirectory();
            }
            Console.WriteLine("--srcdir =  " + options.SourceDir);

            if (options.OutputPath == null)
            {
                options.OutputPath = DEFAULT_OUTPUT_FOLDER;
                Console.Error.WriteLine(
                    "Warning: output FOLDER: (--output ) was not defined - default will be used " +
                    options.OutputPath);
            }


            if (!options.SplitOutput)
            {
                //If output is a directory then the first input filename will be used.
                if (!options.OutputPath.EndsWith(".cs"))
                {
                    string firstPathCs = inputs[0];
                    firstPathCs = Path.GetFileNameWithoutExtension(firstPathCs) + ".cs";
                    options.OutputPath = Path.Combine(options.OutputPath, firstPathCs);
                }
            }
            var fullpath = Path.GetFullPath(options.OutputPath);
            Console.WriteLine("--output =  " + options.OutputPath + "  full path to output folder= " + fullpath);
            options.OutputPath = fullpath;

            if (options.ExperimentalStack != null && !options.ExperimentalStack.Contains("."))
                options.ExperimentalStack = "global::SilentOrbit.ProtocolBuffers." + options.ExperimentalStack;

            if (error)
                return null;
            else
                return options;
        }

        private static readonly string DEFAULT_OUTPUT_FOLDER = "output";

        /// <summary>
        /// Expand wildcards in the filename part of the input file argument.
        /// </summary>
        /// <returns>List of full paths to the files.</returns>
        /// <param name="paths">List of relative paths with possible wildcards in the filename.</param>
        private static List<string> ExpandFileWildCard(IEnumerable<string> paths)
        {
            //Thanks to https://stackoverflow.com/a/2819150
            var list = new List<string>();

            foreach (var path in paths)
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(path);

                var dir = Path.GetDirectoryName(expandedPath);
                if (dir.Length == 0)
                    dir = ".";

                var file = Path.GetFileName(expandedPath);

                foreach (var filepath in Directory.GetFiles(dir, file, SearchOption.AllDirectories))
                    list.Add(Path.GetFullPath(filepath));
            }

            return list;
        }

        public string GetUsage()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var help = new HelpText
            {
                Heading = new HeadingInfo("ProtoBuf Code Generator", version.ToString()),
                Copyright = new CopyrightInfo("Peter Hultqvist", version.Major),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine(
                "Usage: CodeGenerator.exe --src-dir protoSrcDir --output generatedOutputDir *.proto --use-interface --net2");
            help.AddOptions(this);
            return help;
        }
    }
}