using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DeleteFileLine.Properties;

namespace DeleteFileLine
{
  /// <summary>
  /// Class of the main program.
  /// </summary>
  internal static class Program
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
        {"removeemptyline", "true" }
      };
      var fileContent = new List<string>();
      var fileTransformed = new List<string>();
      bool numberOfLineReceivedOk = false;
      int numberOfLineInfile = 0;
      string datedLogFileName = string.Empty;
      if (arguments.Length == 0 || arguments[0].ToLower().Contains("help") || arguments[0].Contains("?"))
      {
        Usage();
        return;
      }

      // we split arguments into the dictionary
      foreach (string argument in arguments)
      {
        if (argument.IndexOf(':') == -1) continue;
        string argumentKey = argument.Substring(1, argument.IndexOf(':') - 1).ToLower();
        string argumentValue = argument.Substring(argument.IndexOf(':') + 1, argument.Length - (argument.IndexOf(':') + 1));
        if (argumentDictionary.ContainsKey(argumentKey))
        {
          // set the value of the argument
          argumentDictionary[argumentKey] = argumentValue;
        }
        else
        {
          // we add any other or new argument into the dictionary to look at them in the log
          argumentDictionary.Add(argumentKey, argumentValue);
        }
      }

      // check that filename doesn't any Windows forbidden characters and trim all space characters at the start of the name.
      argumentDictionary["filename"] = RemoveWindowsForbiddenCharacters(argumentDictionary["filename"]).TrimStart();

      if (argumentDictionary["filename"].Trim() != string.Empty)
      {
        datedLogFileName = AddDateToFileName(Settings.Default.LogFileName);
      }
      else
      {
        Usage();
        return;
      }
      
      // We log all arguments passed in.
      foreach (KeyValuePair<string, string> keyValuePair in argumentDictionary)
      {
        if (argumentDictionary["log"] == "true" && argumentDictionary["filename"] != string.Empty)
        {
          Log(datedLogFileName, argumentDictionary["log"], $"Argument requested: {keyValuePair.Key}");
          Log(datedLogFileName, argumentDictionary["log"], $"Value of the argument: {keyValuePair.Value}");
        }
      }

      // reading of the CSV file
      try
      {
        if (argumentDictionary["filename"].Trim() != string.Empty)
        {
          if (File.Exists(argumentDictionary["filename"].Trim()))
          {
            using (StreamReader sr = new StreamReader(argumentDictionary["filename"]))
            {
              while (!sr.EndOfStream)
              {
                string tmpLine = sr.ReadLine();
                if (tmpLine != null && tmpLine.StartsWith("9;"))
                {
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
                  if (argumentDictionary["removeemptyline"] == "false")
                  {
                    fileContent.Add(tmpLine);
                  }
                  else if (argumentDictionary["removeemptyline"] == "true" && tmpLine != string.Empty)
                  {
                    fileContent.Add(tmpLine);
                  }
                }
              }
            }

            Log(datedLogFileName, argumentDictionary["log"], "The file has been read correctly");
            Log(datedLogFileName, argumentDictionary["log"], $"The footer of the file states {numberOfLineInfile} lines.");
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
        if (argumentDictionary["deleteheader"] == "true" && argumentDictionary["hasheader"] == "true")
        {
          Log(datedLogFileName, argumentDictionary["log"], $"Header (which is the first line) has been removed: {fileContent[0]}");
          fileContent.RemoveAt(0);
        }

        if (argumentDictionary["deletefooter"] == "true" && argumentDictionary["hasfooter"] == "true" && fileContent.Count != 0)
        {
          Log(datedLogFileName, argumentDictionary["log"], $"{numberOfLineInfile} lines stated in footer");
          Log(datedLogFileName, argumentDictionary["log"], $"The file has {fileContent.Count - 1} lines");
          Log(datedLogFileName, argumentDictionary["log"], $"Footer (which is the last line) has been removed: {fileContent[fileContent.Count - 1]}");
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

        //We check integrity of the file i.e. number of line stated equals to the number of line written
        if (fileContent.Count == numberOfLineInfile)
        {
          numberOfLineReceivedOk = true;
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
                if (argumentDictionary["removeemptyline"] == "true" && line.Trim() != string.Empty)
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
                if (argumentDictionary["removeemptyline"] == "true" && line.Trim() != string.Empty)
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
      }
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
      // TODO
      string[] forbiddenWindowsFilenameCharacters = { "\\", "/", ":", "*", "?", "\"", "<", ">", "|" };
      // Linq version return forbiddenWindowsFilenameCharacters.Aggregate(result, (current, item) => current.Replace(item, string.Empty));
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
    /// The log file to record all activities.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    /// <param name="logging">Do we log or not?</param>
    /// <param name="message">The message to be logged.</param>
    private static void Log(string filename, string logging, string message)
    {
      if (logging.ToLower() != "true") return;
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
      display("/filename is the same as /FileName or /fileName");
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
      display("/removeEmptyLine:<true or false> true by default");
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
