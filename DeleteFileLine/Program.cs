using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DeleteFileLine.Properties;

namespace DeleteFileLine
{
  /// <summary>
  /// Class of the main program.
  /// </summary>
  public static class Program
  {
    /// <summary>
    /// Entry point of the program.
    /// </summary>
    /// <param name="arguments">
    /// All the arguments separated by a white space.
    /// </param>
    private static void Main(string[] arguments)
    {
      Action<string> display = Console.WriteLine;
      var argumentDictionary = new Dictionary<string, string>
      {
        // Initialization of the dictionary with default values
        {"filename", string.Empty},
        {"separator", ";" },
        {"hasheader", "false" },
        {"hasfooter", "false"},
        {"deleteheader", "false"},
        {"deletefooter", "false"},
        {"deletefirstcolumn", "false"},
        {"samename", "true"},
        {"newname", string.Empty},
        {"log", "false" },
        {"removeemptylines", "true" },
        {"countlines", "false" },
        {"verifyheaderandfooter", "false" }
      };
      // the variable numberOfInitialDictionaryItems is used for the log to list all non-standard arguments passed in.
      int numberOfInitialDictionaryItems = 13;
      var fileContent = new List<string>();
      var fileTransformed = new List<string>();
      int numberOfLineInfile = 0;
      bool hasExtraArguments = false;
      bool fileHasHeader = false;
      bool fileHasFooter = false;
      string datedLogFileName = string.Empty;
      byte returnCode = 1;
      Stopwatch chrono = new Stopwatch();
      if (arguments.Length == 0 || arguments[0].ToLower().Contains("help") || arguments[0].Contains("?"))
      {
        Usage();
        return;
      }

      chrono.Start();
      // we split arguments into the dictionary
      foreach (string argument in arguments)
      {
        string argumentKey = string.Empty;
        string argumentValue = string.Empty;
        if (argument.IndexOf(':') != -1)
        {
          argumentKey = argument.Substring(1, argument.IndexOf(':') - 1).ToLower();
          argumentValue = argument.Substring(argument.IndexOf(':') + 1,
            argument.Length - (argument.IndexOf(':') + 1));
        }
        else
        {
          // If we have an argument without the colon sign (:) then we add it to the dictionary
          argumentKey = argument;
          argumentValue = "The argument passed in does not have any value. The colon sign (:) is missing.";
        }

        if (argumentDictionary.ContainsKey(argumentKey))
        {
          // set the value of the argument
          argumentDictionary[argumentKey] = argumentValue;
        }
        else
        {
          // we add any other or new argument into the dictionary to look at them in the log
          argumentDictionary.Add(argumentKey, argumentValue);
          hasExtraArguments = true;
        }
      }

      // We check if countlines is true then removeemptyline = true
      if (argumentDictionary["countlines"] == "true")
      {
        argumentDictionary["removeemptylines"] = "true";
      }

      // check that filename doesn't any Windows forbidden characters and trim all space characters at the start of the name.
      argumentDictionary["filename"] = RemoveWindowsForbiddenCharacters(argumentDictionary["filename"]).TrimStart();

      // if log file name is empty in XML file then we define it with a default value like "Log"
      if (Settings.Default.LogFileName.Trim() == string.Empty)
      {
        Settings.Default.LogFileName = "Log.txt";
        Settings.Default.Save();
      }

      // if Company name is empty in XML file then we define it with a default value like "Company name"
      if (Settings.Default.CompanyName.Trim() == string.Empty)
      {
        Settings.Default.LogFileName = "Company name";
        Settings.Default.Save();
      }

      if (argumentDictionary["filename"].Trim() != string.Empty)
      {
        datedLogFileName = AddDateToFileName(Settings.Default.LogFileName);
      }
      else
      {
        Usage();
        return;
      }

      // Add version of the program at the beginning of the log
      Log(datedLogFileName, argumentDictionary["log"], $"DeleteFileLine.exe is in version {GetAssemblyVersion()}");

      // We log all arguments passed in.
      foreach (KeyValuePair<string, string> keyValuePair in argumentDictionary)
      {
        if (argumentDictionary["log"] == "true")
        {
          Log(datedLogFileName, argumentDictionary["log"], $"Argument requested: {keyValuePair.Key}");
          Log(datedLogFileName, argumentDictionary["log"], $"Value of the argument: {keyValuePair.Value}");
        }
      }

      //we log extra arguments
      if (hasExtraArguments && argumentDictionary["log"] == "true")
      {
        Log(datedLogFileName, argumentDictionary["log"], $"Here are a list of argument passed in but not understood and thus not used.");
        for (int i = numberOfInitialDictionaryItems; i <= argumentDictionary.Count - 1; i++)
        {
          Log(datedLogFileName, argumentDictionary["log"], $"Extra argument requested: {argumentDictionary.Keys.ElementAt(i)}");
          Log(datedLogFileName, argumentDictionary["log"], $"Value of the extra argument: {argumentDictionary.Values.ElementAt(i)}");
        }
      }

      // reading of the CSV file
      try
      {
        if (argumentDictionary["filename"].Trim() != string.Empty)
        {
          if (File.Exists(argumentDictionary["filename"]))
          {
            using (StreamReader sr = new StreamReader(argumentDictionary["filename"]))
            {
              while (!sr.EndOfStream)
              {
                string tmpLine = sr.ReadLine();
                if (tmpLine != null && tmpLine.StartsWith("0;"))
                {
                  fileHasHeader = true;
                }

                if (tmpLine != null && tmpLine.StartsWith("9;"))
                {
                  fileHasFooter = true;
                  bool parseLastLineTointOk = int.TryParse(tmpLine.Substring(2, tmpLine.Length - 2).TrimStart('0'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out numberOfLineInfile);
                  if (!parseLastLineTointOk)
                  {
                    const string tmpErrorMessage = "There was an error while parsing the last line of the file to an integer to know the number of lines in the file.";
                    Log(datedLogFileName, argumentDictionary["log"], $"{tmpErrorMessage}");
                    Console.WriteLine($"{tmpErrorMessage}");
                  }
                }

                if (tmpLine != null)
                {
                  if (argumentDictionary["removeemptylines"] == "false")
                  {
                    fileContent.Add(tmpLine);
                  }
                  else if (argumentDictionary["removeemptylines"] == "true" && tmpLine != string.Empty)
                  {
                    fileContent.Add(tmpLine);
                  }
                }
              }
            }

            Log(datedLogFileName, argumentDictionary["log"], "The file has been read correctly");
            if (argumentDictionary["countlines"] == "true")
            {
              Log(datedLogFileName, argumentDictionary["log"], $"The footer of the file states {numberOfLineInfile} lines.");
            }
          }
          else
          {
            Log(datedLogFileName, argumentDictionary["log"], $"the filename: {argumentDictionary["filename"]} could be read because it doesn't exist");
          }
        }
        else
        {
          Log(datedLogFileName, argumentDictionary["log"], $"the filename: {argumentDictionary["filename"]} is empty, it cannot be read");
        }
      }
      catch (Exception exception)
      {
        Log(datedLogFileName, argumentDictionary["log"], $"There was an error while processing the file {exception}");
        Console.WriteLine($"There was an error while processing the file {exception}");
      }

      if (fileContent.Count != 0)
      {
        if (argumentDictionary["deleteheader"] == "true" && argumentDictionary["hasheader"] == "true" && fileHasHeader)
        {
          Log(datedLogFileName, argumentDictionary["log"], $"Header (which is the first line) has been removed, it was: {fileContent[0]}");
          fileContent.RemoveAt(0);
        }

        if (argumentDictionary["deletefooter"] == "true" && argumentDictionary["hasfooter"] == "true" && fileContent.Count != 0 && fileHasFooter)
        {
          if (argumentDictionary["countlines"] == "true")
          {
            Log(datedLogFileName, argumentDictionary["log"], $"{numberOfLineInfile} lines stated in footer");
            Log(datedLogFileName, argumentDictionary["log"], $"Footer (which is the last line) has been removed, it was: {fileContent[fileContent.Count - 1]}");
          }

          Log(datedLogFileName, argumentDictionary["log"], $"The file has {fileContent.Count - 1} lines");
          fileContent.RemoveAt(fileContent.Count - 1);
        }

        if (argumentDictionary["deletefirstcolumn"] == "true" && fileContent.Count != 0)
        {
          Log(datedLogFileName, argumentDictionary["log"], "The first column has been deleted");
          fileTransformed = new List<string>();
          foreach (string line in fileContent)
          {
            fileTransformed.Add(line.Substring(line.IndexOf(argumentDictionary["separator"], StringComparison.InvariantCulture) + 1, line.Length - line.IndexOf(argumentDictionary["separator"], StringComparison.InvariantCulture) - 1));
          }

          fileContent = fileTransformed;
        }

        // we free up memory
        fileTransformed = null;

        //We check integrity of the file i.e. number of line stated equals to the number of line written
        if (fileContent.Count == numberOfLineInfile && argumentDictionary["countlines"] == "true")
        {
          Log(datedLogFileName, argumentDictionary["log"], $"The file has the same number of lines as stated in the last line which is {numberOfLineInfile} lines.");
          returnCode = Settings.Default.ReturnCodeOK;
        }
        else if (fileContent.Count != numberOfLineInfile && argumentDictionary["countlines"] == "true")
        {
          Log(datedLogFileName, argumentDictionary["log"], $"The file has not the same number of lines {fileContent.Count} as stated in the last line which is {numberOfLineInfile} lines.");
          returnCode = Settings.Default.ReturnCodeKO;
        }

        if (argumentDictionary["countlines"] == "false")
        {
          returnCode = Settings.Default.ReturnCodeOK;
        }

        // If the user wants a different name for the transformed file
        if (argumentDictionary["samename"] == "true" && argumentDictionary["filename"] != string.Empty)
        {
          try
          {
            File.Delete(argumentDictionary["filename"]);
            using (StreamWriter sw = new StreamWriter(argumentDictionary["filename"], true))
            {
              foreach (string line in fileContent)
              {
                if (argumentDictionary["removeemptylines"] == "true" && line.Trim() != string.Empty)
                {
                  sw.WriteLine(line);
                }
              }
            }

            Log(datedLogFileName, argumentDictionary["log"], $"The transformed file has been written correctly:{argumentDictionary["filename"]}");
          }
          catch (Exception exception)
          {
            Log(datedLogFileName, argumentDictionary["log"], $"The filename {argumentDictionary["filename"]} cannot be written");
            Log(datedLogFileName, argumentDictionary["log"], $"The exception is: {exception}");
          }
        }

        if (argumentDictionary["samename"] == "false" && argumentDictionary["newname"] != string.Empty)
        {
          try
          {
            using (StreamWriter sw = new StreamWriter(argumentDictionary["newname"]))
            {
              foreach (string line in fileContent)
              {
                if (argumentDictionary["removeemptylines"] == "true" && line.Trim() != string.Empty)
                {
                  sw.WriteLine(line);
                }
              }
            }

            Log(datedLogFileName, argumentDictionary["log"], $"The transformed file has been written correctly with the new name {argumentDictionary["newname"]}.");
          }
          catch (Exception exception)
          {
            Log(datedLogFileName, argumentDictionary["log"], $"The filename: {argumentDictionary["newname"]} cannot be written.");
            Log(Settings.Default.LogFileName, argumentDictionary["log"], $"The exception is: {exception}");
          }
        }

        Log(Settings.Default.LogFileName, argumentDictionary["log"], $"The header was {Negative(fileHasHeader)}found in the file.");
        Log(Settings.Default.LogFileName, argumentDictionary["log"], $"The footer was {Negative(fileHasFooter)}found in the file.");
      }
      else
      {
        // filecontent is empty
        Log(Settings.Default.LogFileName, argumentDictionary["log"], "The file cannot be processed because it is empty.");
      }

      // Managing return code if header or footer were not found
      if (!fileHasHeader && argumentDictionary["countlines"] == "true" && returnCode != 0)
      {
        returnCode = Settings.Default.ReturnCodeHeaderMissing;
      }

      if (!fileHasFooter && argumentDictionary["countlines"] == "true" && returnCode != 0)
      {
        returnCode = Settings.Default.ReturnCodeFooterMissing;
      }


      if (argumentDictionary["countlines"] == "true")
      {
        // Managing return code : we write a file with the return code which will be read by the DOS script to import SQL tables into a database.
        const string returnCodeFileName = "ReturnCode.txt";
        try
        {
          File.Delete(returnCodeFileName);
          StreamWriter sw = new StreamWriter(returnCodeFileName, false);
          sw.WriteLine(returnCode);
          sw.Close();
          Log(datedLogFileName, argumentDictionary["log"], $"The return code has been written into the file {returnCodeFileName}, the return code is {returnCode}.");
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
          Log(Settings.Default.LogFileName, argumentDictionary["log"], $"There was an error while writing the return code file: {returnCodeFileName}. The exception is: {unauthorizedAccessException}");
          Console.WriteLine($"There was an error while writing the return code file: {returnCodeFileName}. The exception is:{unauthorizedAccessException}");
        }
        catch (IOException ioException)
        {
          Log(Settings.Default.LogFileName, argumentDictionary["log"], $"There was an error while writing the return code file: {returnCodeFileName}. The exception is: {ioException}");
          Console.WriteLine($"There was an error while writing the return code file: {returnCodeFileName}. The exception is:{ioException}");
        }
        catch (Exception exception)
        {
          Log(Settings.Default.LogFileName, argumentDictionary["log"], $"There was an error while writing the return code file: {returnCodeFileName}. The exception is: {exception}");
          Console.WriteLine($"There was an error while writing the return code file: {returnCodeFileName}. The exception is:{exception}");
        }
      }

      chrono.Stop();
      TimeSpan tickTimeSpan = chrono.Elapsed;
      Log(datedLogFileName, argumentDictionary["log"], $"This program took {chrono.ElapsedMilliseconds} milliseconds which is {ConvertToTimeString(tickTimeSpan)}.");
      Log(datedLogFileName, argumentDictionary["log"], $"END OF LOG.");
      Log(datedLogFileName, argumentDictionary["log"], "-----------");
    }

    /// <summary>
    /// Convert a Time span to days hours minutes seconds milliseconds.
    /// </summary>
    /// <param name="ts">The time span.</param>
    /// <param name="removeZeroArgument">Do you want zero argument not send back, true by default.</param>
    /// <returns>Returns a string with the number of days, hours, minutes, seconds and milliseconds.</returns>
    public static string ConvertToTimeString(TimeSpan ts, bool removeZeroArgument = true)
    {
      string result = string.Empty;
      if (!removeZeroArgument || ts.Days != 0 )
      {
        result = $"{ts.Days} jour{Plural(ts.Days)} ";
      }

      if (!removeZeroArgument || ts.Hours != 0)
      {
        result += $"{ts.Hours} heure{Plural(ts.Hours)} ";
      }

      if (!removeZeroArgument || ts.Minutes != 0)
      {
        result += $"{ts.Minutes} minute{Plural(ts.Minutes)} ";
      }

      if (!removeZeroArgument || ts.Seconds != 0)
      {
        result += $"{ts.Seconds} seconde{Plural(ts.Seconds)} ";
      }

      if (!removeZeroArgument || ts.Milliseconds != 0)
      {
        result += $"{ts.Milliseconds} milliseconde{Plural(ts.Milliseconds)}";
      }

      return result.TrimEnd();
    }

    /// <summary>
    /// Add an 's' if the number is greater than 1.
    /// </summary>
    /// <param name="number"></param>
    /// <returns>Returns an 's' if number if greater than one ortherwise returns an empty string.</returns>
    public static string Plural(int number)
    {
      return number > 1 ? "s" : string.Empty;
    }

    /// <summary>
    /// The method returns the string Not according to the boolean value passed in.
    /// </summary>
    /// <param name="booleanValue"></param>
    /// <returns>Returns the string "Not" or nothing according to the boolean value passed in.</returns>
    public static string Negative(bool booleanValue)
    {
      return booleanValue ? "" : "not ";
    }

    /// <summary>
    /// Remove all Windows forbidden characters for a Windows path.
    /// </summary>
    /// <param name="path">The initial string to be processed.</param>
    /// <returns>A string without Windows forbidden characters.</returns>
    private static string RemoveWindowsForbiddenCharacters(string path)
    {
      string result = path;
      // We remove all characters which are forbidden for a Windows path
      string[] forbiddenWindowsFilenameCharacters = { "\\", "/", ":", "*", "?", "\"", "<", ">", "|" };
      foreach (var item in forbiddenWindowsFilenameCharacters)
      {
        result = result.Replace(item, string.Empty);
      }

      return result;
    }

    /// <summary>
    /// Add date to the file name.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>A string with the date at the end of the file name.</returns>
    private static string AddDateToFileName(string fileName)
    {
      string result = string.Empty;
      // We strip the fileName and add a datetime before the extension of the filename.
      string tmpFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
      string tmpFileNameExtension = Path.GetExtension(fileName);
      string tmpDateTime = DateTime.Now.ToShortDateString();
      tmpDateTime = tmpDateTime.Replace('/', '-');
      result = $"{tmpFileNameWithoutExtension}_{tmpDateTime}{tmpFileNameExtension}";

      return result;
    }

    /// <summary>
    /// Get assembly version.
    /// </summary>
    /// <returns>A string with all assembly versions like major, minor, build.</returns>
    private static string GetAssemblyVersion()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      return $"{fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}.{fvi.FilePrivatePart}";
    }

    /// <summary>
    /// The log file to record all activities.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="logging">Do we log or not?</param>
    /// <param name="message">The message to be logged.</param>
    private static void Log(string filename, string logging, string message)
    {
      if (logging.ToLower() != "true") return;
      if (filename.Trim() == string.Empty) return;
      try
      {
        StreamWriter sw = new StreamWriter(filename, true);
        sw.WriteLine($"{DateTime.Now} - {message}");
        sw.Close();
      }
      catch (Exception exception)
      {
        Console.WriteLine($"There was an error while writing the file: {filename}. The exception is:{exception}");
      }
    }

    /// <summary>
    /// If the user requests help or gives no argument, then we display the help section.
    /// </summary>
    private static void Usage()
    {
      Action<string> display = Console.WriteLine;
      display(string.Empty);
      display($"DeleteFileLine is a console application written by Sogeti for {Settings.Default.CompanyName}.");
      display($"DeleteFileLine.exe is in version {GetAssemblyVersion()}");
      display("DeleteFileLine needs Microsoft .NET framework 4.0 to run, if you don't have it, download it from microsoft.com.");
      display($"Copyrighted (c) 2017 by {Settings.Default.CompanyName}, all rights reserved.");
      display(string.Empty);
      display("Usage of this program:");
      display(string.Empty);
      display("List of arguments:");
      display(string.Empty);
      display("/help (this help)");
      display("/? (this help)");
      display(string.Empty);
      display(
        "You can write argument name (not its value) in uppercase or lowercase or a mixed of them (case insensitive)");
      display("/filename is the same as /FileName or /fileName or /FILENAME");
      display(string.Empty);
      display("/fileName:<name of the file to be processed>");
      display("/separator:<the CSV separator> semicolon (;) is the default separator");
      display("/hasHeader:<true or false> false by default");
      display("/hasFooter:<true or false> false by default");
      display("/deleteHeader:<true or false> false by default");
      display("/deleteFooter:<true or false> false by default");
      display("/deleteFirstColumn:<true or false> true by default");
      display("/sameName:<true or false> true by default");
      display("/newName:<new name of the file which has been processed>");
      display("/log:<true or false> false by default");
      display("/removeemptylines:<true or false> true by default");
      display("countlines:<true or false> false by default");
      display("verifyheaderandfooter:<true or false> false by default");
      display(string.Empty);
      display("Examples:");
      display(string.Empty);
      display("DeleteFileLine /filename:MyCSVFile.txt /separator:, /hasheader:true /hasfooter:true /deleteheader:true /deletefooter:true /deletefirstcolumn:true /log:true");
      display(string.Empty);
      display("DeleteFileLine /help (this help)");
      display("DeleteFileLine /? (this help)");
      display(string.Empty);
    }
  }
}
