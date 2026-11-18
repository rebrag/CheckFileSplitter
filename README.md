# CheckFileSplitter

**LSK-839: split trust disbursements into chunks of about 500.**

A C# console app that reads a pipe-delimited trust disbursement file and writes it out as
`InputFilename_001.txt`, `InputFilename_002.txt`, … with about 500 records each. **A check is
never split across two files.**

## Requirements

| # | Requirement | Where it's handled |
|---|---|---|
| 1 | Split the input into chunks of ~500 records named `InputFilename_001.txt`, `_002`, … | `Program.WriteChunks` |
| 2 | Every row with the same `CkNum` must be in the same file. The input is already sorted by `CkNum`. | `DisbursementSplitter.Split` |
| 3 | If record 500 starts a check, the rest of that check goes in the same file, **even if it makes the file > 500 records** | `DisbursementSplitter.Split` |
| 4 | Read with `StreamReader`, write with `StreamWriter` | `DisbursementFile.Read`, `ChunkFileWriter.Write` |
| 5 | Parse each row (24 `\|`-delimited columns) into a `TrustDisbursement` | `DisbursementFile.Read` |
| 6 | The input has a header record on top, and **every output file starts with that header** | `DisbursementFile.Read`, `ChunkFileWriter.Write` |
| 7 | The first records may have no `CkNum`. **They all go into the same file, however many there are.** | `TrustDisbursement.CheckKey` |
| 8 | Write everything the program prints to `Splitter_Log_YYYYMMDD.log` | `SplitterLog`, `TeeTextWriter` |
| 9 | Every line in each output file, **including the very last one**, ends with CRLF (`\r\n`) | `ChunkFileWriter` |

### How the split works

Records are added to the current file. A new file is started only when the current file already
holds **at least 500 records** *and* the next record has a **different `CkNum`**. Rows with no
`CkNum` all count as one group, so they always stay together. The 500 counts data records only,
not the header.

### Safety checks

The program stops with a clear message, and writes nothing, if:

- a row doesn't have exactly 24 columns (the message gives the line number)
- the first line isn't the header (column 18 must be `CkNum`)
- the file isn't sorted by `CkNum`. Rows for one check appearing in two separate places, or
  rows without a `CkNum` that aren't all at the top, would make requirements 2 and 7 impossible.

Each row is written back exactly as it was read (`TrustDisbursement.ToPipeLine()`). Every line of every output file ends with CRLF (`\r\n`), the last line included, whatever the
input uses. `StreamWriter.WriteLine` adds a line ending after each line, but by default that's
`Environment.NewLine`, which is `\r\n` only on Windows. So `ChunkFileWriter` sets
`writer.NewLine = "\r\n"` explicitly, and the output is the same on any machine.

## Running it

The program targets **.NET Framework 4.6** (`net46`), so it runs on any Windows machine with
.NET Framework **4.6 or later**: 4.6, 4.6.1, 4.6.2, 4.7.x and 4.8 all work, and Windows 10 and 11
include 4.8. It doesn't need .NET Core or .NET 8. The code sticks to C# 7.3, the default
language version for .NET Framework projects.

The test project targets `net462`, the oldest version the test tools support. It only runs on
the development machine, never on the machine that runs the splitter.

**Visual Studio (2019 or 2022, with the ".NET desktop development" workload):**

1. Open `CheckFileSplitter.sln`.
2. Right-click the **CheckFileSplitter** project → *Set as Startup Project*.
3. *Project Properties → Debug*: set **Command line arguments** to the input file path in quotes,
   for example `"C:\Data\TrustDisb_20261116020008.txt"`. With no argument the program only prints
   its usage line and exits.
4. Press **F5**.

If the build fails with **MSB3644** ("reference assemblies for .NETFramework,Version=v4.6 were not
found"), Visual Studio is missing the 4.6 targeting pack (Visual Studio 2022 no longer installs it).
Normally the build downloads the reference assemblies from NuGet on its own. If it doesn't, change
`<TargetFramework>net46</TargetFramework>` to `net48` in `src/CheckFileSplitter/CheckFileSplitter.csproj`,
as long as the machine that runs it has 4.8.

**Command line / scheduled task:** after a build, the program is a plain `.exe`:

```
src\CheckFileSplitter\bin\Release\net46\CheckFileSplitter.exe "C:\Data\TrustDisb_20261116020008.txt" ["C:\Data\Split"]
```

The output goes next to the input file unless you give an output folder. Example console output
for the sample file:

```
===== 2026-11-18 15:04:15 CheckFileSplitter started =====
Input:  C:\Data\SampleTrustDisb.txt
Output: C:\Data
Read 1451 records from SampleTrustDisb.txt.
  SampleTrustDisb_001.txt: 502 records (CkNum (none) to 500211)
  SampleTrustDisb_002.txt: 500 records (CkNum 500212 to 500408)
  SampleTrustDisb_003.txt: 449 records (CkNum 500409 to 500596)
Done: 3 file(s) written to C:\Data
===== 2026-11-18 15:04:15 Finished with exit code 0 =====
```

### Log file

Everything the program prints, errors included, is also written to
**`Splitter_Log_YYYYMMDD.log`** in the output folder, for example `Splitter_Log_20261118.log`.
It works through `Console.SetOut` / `Console.SetError` with a small `TeeTextWriter`, so every
`Console.WriteLine` goes to both the screen and the log. There is one log per day, and each run is
appended to it between the `started` / `Finished` lines, so earlier runs that day are kept.

Exit codes: `0` success, `1` no input file given, `2` bad input or a file problem (the message
says which), `3` unexpected error (the full stack trace goes to the log).

## Tests

```
dotnet test
```

There are 16 xUnit tests in `tests/CheckFileSplitter.Tests`. They cover each rule above: a check
straddling record 500, a split at exactly 500, more than 500 rows with no `CkNum`, nothing split
or dropped, an unsorted file, a missing header, a bad column count, blank lines, and an exact
round trip. They also cover the log: the file name, output and errors reaching the log, runs being
appended, and the console being restored afterwards. Two more check that every line, the last one
included, ends in CRLF with no bare `\n`, and that the header is the exact first line.

## Results on the de-identified test file

Gary's de-identified test file has 1 header row and 7,697 records, of which 353 have no
`CkNum`. It is **not** in this repo, and neither are its check numbers, so no work data is
published.

| File | Records |
|---|---:|
| _001 | 501 (includes all 353 rows with no `CkNum`) |
| _002 | 501 |
| _003 – _009 | 500–501 each |
| _010 | **1,401** (see question 1) |
| _011 | 541 |
| _012 | 576 |
| _013 | 556 |
| _014 | 120 |

A separate script checked the output. Every file starts with the header. The data rows,
joined back together, are **byte-for-byte identical** to the input (apart from line endings,
which are now CRLF), in the same order. Every file ends with `\r\n`, with no bare `\n` anywhere. No
`CkNum` appears in more than one file, all 353 rows with no `CkNum` are in `_001`, and every
file is closed at the first check boundary at or after 500.

## Questions to settle

1. **Very large checks.** One check in the test file has **1,217 rows** on its own. It started while `_010` held
   184 records, so `_010` ended up with 1,401. That follows rule 3 as written. If a big check
   should start its own file instead, it's a small change in `DisbursementSplitter.Split`.
2. **Rows with no `CkNum`.** All 353 are in `_001`, and that file is then topped up with checks
   to reach 500. Should those rows get a file of their own instead?

## Project layout

```
src/CheckFileSplitter/
  TrustDisbursement.cs      one row (24 fields) + ToPipeLine()
  DisbursementFile.cs       reads the header and records with StreamReader
  DisbursementSplitter.cs   the splitting rules and the "sorted by CkNum" check
  ChunkFileWriter.cs        writes one output file with StreamWriter: header + rows, all CRLF
  Program.cs                command line, names the output files, prints the summary
  SplitterLog.cs            copies console output to Splitter_Log_YYYYMMDD.log
  TeeTextWriter.cs          a TextWriter that writes to two places (screen + log)
tests/CheckFileSplitter.Tests/
samples/SampleTrustDisb.txt made-up data in the same format, for trying it out
```
