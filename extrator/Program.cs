using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.IO;
using System.IO.Compression;
using NLog;
using NLog.Layouts;

namespace extrator
{
   public class Program
   {
      private static Logger logger;
      private static string ExePath;
      private static string RootPath;
      private static string LogPath;
      static void Main(string[] args)
      {
         ExePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         RootPath = Path.Combine(ExePath, "");
         LogPath = Path.Combine(ExePath, $"Logs\\UploadFileExtractor_{DateTime.Now.ToString("yyyyMMdd")}");
         logger = new Logger("log");
         try
         {
            LoggerConfigure(LogPath);

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
         }
         catch (Exception ex)
         {
            logger.Error($":: ERROR :: {ex.Message}");
         }
      }

      static void RunOptions(Options opt)
      {
         if (!File.Exists(opt.PathFile))
            throw new Exception($"{opt.PathFile} is not exist");
         var f = Path.GetFileNameWithoutExtension(opt.PathFile);
         var d = $"{opt.Output}\\{f}";
         if (!Directory.Exists(d))
            Directory.CreateDirectory(d);
         if (string.IsNullOrEmpty(opt.Server))
            throw new Exception($"Server must be set");

         if (string.IsNullOrEmpty(opt.Database))
            throw new Exception($"Database must be set");
         var connStr = $"Data Source={opt.Server};Initial Catalog={opt.Database};Integrated Security=True;Connection Timeout=30;";
         SqlManager res = new SqlManager(connStr);
         res.CreateTable();

         using (ZipArchive source = ZipFile.Open(opt.PathFile, ZipArchiveMode.Read, null))
         {
            foreach (ZipArchiveEntry e in source.Entries)
            {
               string fullPath = Path.GetFullPath(Path.Combine(d, e.FullName));
               if (Path.GetFileName(fullPath).Length == 0)
                  continue;
               Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
               // The boolean parameter determines whether an existing file that has the same name as the destination file should be overwritten
               e.ExtractToFile(fullPath, true);
            }
         }

         foreach (var file in Directory.GetFiles($"{d}\\Image\\"))
            res.InsertToTableUploadFileExtractor(Path.GetFullPath(file));
      }

      static void HandleParseError(IEnumerable<Error> errs)
      {
         foreach (var err in errs)
         {
            logger.Error($"Error : {err.Tag}");
         }
      }

      static void LoggerConfigure(string path)
      {
         var config = new NLog.Config.LoggingConfiguration();

         // Targets where to log to: File and Console
         var logfile = new NLog.Targets.FileTarget("logfile");
         if (path != null)
         {
            if (Path.GetFileName(path) == path)
               logfile.FileName = $"{Path.Combine(Path.Combine(RootPath, "Logs"), path)}";
            else
               logfile.FileName = $"{path}";
         }
         else
            logfile.FileName = $"{Path.Combine(Path.Combine(RootPath, "Logs"), $"{DateTime.Now.ToString("yyyyMMdd")}.log")}";

         logfile.MaxArchiveFiles = 60;
         logfile.ArchiveAboveSize = 10240000;

         var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

         config.AddRule(LogLevel.Error, LogLevel.Fatal, logconsole);

         config.AddRule(LogLevel.Error, LogLevel.Fatal, logfile);

         // design layout for file log rotation
         CsvLayout layout = new CsvLayout();
         layout.Delimiter = CsvColumnDelimiterMode.Comma;
         layout.Quoting = CsvQuotingMode.Auto;
         layout.Columns.Add(new CsvColumn("Start Time", "${longdate}"));
         layout.Columns.Add(new CsvColumn("Elapsed Time", "${elapsed-time}"));
         layout.Columns.Add(new CsvColumn("Machine Name", "${machinename}"));
         layout.Columns.Add(new CsvColumn("Login", "${windows-identity}"));
         layout.Columns.Add(new CsvColumn("Level", "${uppercase:${level}}"));
         layout.Columns.Add(new CsvColumn("Message", "${message}"));
         layout.Columns.Add(new CsvColumn("Exception", "${exception:format=toString}"));
         logfile.Layout = layout;

         // design layout for console log rotation
         SimpleLayout ConsoleLayout = new SimpleLayout("${longdate}:${message}\n${exception}");
         logconsole.Layout = ConsoleLayout;

         // Apply config           
         NLog.LogManager.Configuration = config;
      }
   }

   public class Options
   {
      [Option('p', "path", Required = true, HelpText = "Path for source file")]
      public string PathFile { get; set; }
      [Option('o', "output", Required = true, HelpText = "Path output for destination dir")]
      public string Output { get; set; }
      [Option('s', "server", Required = true, HelpText = "Server Name")]
      public string Server { get; set; }
      [Option('d', "database", Required = true, HelpText = "Database Name")]
      public string Database { get; set; }
   }
}
