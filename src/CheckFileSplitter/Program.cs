using System;
using System.Collections.Generic;
using System.IO;

namespace CheckFileSplitter
{
    class Program
    {
        const int RecordsPerChunk = 500;

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: CheckFileSplitter <inputFile> [outputFolder]");
                Console.WriteLine("Writes <inputName>_001.txt, <inputName>_002.txt, ... (default: next to the input file),");
                Console.WriteLine("and appends a copy of this console output to Splitter_Log_YYYYMMDD.log in the same folder.");
                return 1;
            }

            string inputPath = args[0];
            string outputFolder = Path.GetFullPath(args.Length > 1
                ? args[1]
                : Path.GetDirectoryName(Path.GetFullPath(inputPath)));

            SplitterLog log;
            try
            {
                // From here on, every Console.WriteLine (and Console.Error.WriteLine) also goes to
                // Splitter_Log_YYYYMMDD.log in the output folder
                log = new SplitterLog(outputFolder, DateTime.Now);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Error: could not open the log file in {outputFolder}: {ex.Message}");
                return 2;
            }

            using (log)
            {
                Console.WriteLine($"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} CheckFileSplitter started =====");
                Console.WriteLine($"Input:  {Path.GetFullPath(inputPath)}");
                Console.WriteLine($"Output: {outputFolder}");

                int exitCode = Run(inputPath, outputFolder);

                Console.WriteLine($"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} Finished with exit code {exitCode} =====");
                Console.WriteLine();
                return exitCode;
            }
        }

        static int Run(string inputPath, string outputFolder)
        {
            try
            {
                // 1. Read the header and parse every row into a TrustDisbursement
                DisbursementFile file = DisbursementFile.Read(inputPath);
                Console.WriteLine($"Read {file.Records.Count} records from {Path.GetFileName(inputPath)}.");

                // 2. Split into ~500-record chunks without splitting any check
                List<List<TrustDisbursement>> chunks = DisbursementSplitter.Split(file.Records, RecordsPerChunk);

                // 3. Write each chunk, with the header as its first line
                WriteChunks(file, chunks, inputPath, outputFolder);
                return 0;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidDataException || ex is IOException
                                       || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 2;
            }
            catch (Exception ex)
            {
                // Anything unexpected still gets recorded in the log, with the full stack trace
                Console.Error.WriteLine($"Unexpected error: {ex}");
                return 3;
            }
        }

        static void WriteChunks(DisbursementFile file, List<List<TrustDisbursement>> chunks,
                                string inputPath, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath);
            if (ext == "") ext = ".txt";

            for (int i = 0; i < chunks.Count; i++)
            {
                string outPath = Path.Combine(outputFolder, $"{baseName}_{i + 1:D3}{ext}");

                using (var writer = new StreamWriter(outPath))
                {
                    writer.NewLine = file.NewLine;
                    writer.WriteLine(file.HeaderLine);

                    foreach (TrustDisbursement record in chunks[i])
                        writer.WriteLine(record.ToPipeLine());
                }

                List<TrustDisbursement> chunk = chunks[i];
                Console.WriteLine($"  {Path.GetFileName(outPath)}: {chunk.Count} records " +
                                  $"(CkNum {Describe(chunk[0])} to {Describe(chunk[chunk.Count - 1])})");
            }

            Console.WriteLine($"Done: {chunks.Count} file(s) written to {outputFolder}");
        }

        static string Describe(TrustDisbursement record)
        {
            return record.HasNoCheckNumber ? "(none)" : record.CkNum;
        }
    }
}
