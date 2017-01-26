using System;
using System.Collections.Generic;
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
        {"language", "english" }
       };
      // TODO implement language
      var fileContent = new List<string>();
      var fileTransformed = new List<string>();
      if (arguments.Length == 0 || arguments[0].Contains("help") || arguments[0].Contains("?"))
      {
        Usage();
        return;
      }

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
          // we add any other or new argument into the dictionary
          argumentDictionary.Add(argumentKey, argumentValue);
        }
      }

      // reading of the file
      try
      {
        if (argumentDictionary["filename"] != string.Empty)
        {
          if (File.Exists(argumentDictionary["filename"]))
          {
            using (StreamReader sr = new StreamReader(argumentDictionary["filename"]))
            {
              while (!sr.EndOfStream)
              {
                fileContent.Add(sr.ReadLine());
              }
            }
          }
          else
          {
            if (argumentDictionary["log"] == "true")
            {
              Log(Settings.Default.LogFileName, $"the filename: {argumentDictionary["filename"]}");
            }
          }
        }
      }
      catch (Exception exception)
      {
        Console.WriteLine($"There was an error while processing the file {exception}");
      }

      if (fileContent.Count == 0) return;

      if (argumentDictionary["deleteheader"] == "true" && argumentDictionary["hasheader"] == "true")
      {
        Log(Settings.Default.LogFileName, $"Header has been removed: {fileContent[0]}");
        fileContent.RemoveAt(0);
      }

      if (argumentDictionary["deletefooter"] == "true" && argumentDictionary["hasfooter"] == "true")
      {
        Log(Settings.Default.LogFileName, $"Footer has been removed: {fileContent[fileContent.Count - 1]}");
        fileContent.RemoveAt(fileContent.Count - 1);
      }

      if (argumentDictionary["deletefirstcolumn"] == "true")
      {
        Log(argumentDictionary["log"], "The first column has been deleted.");
        foreach (string line in fileContent)
        {
          fileTransformed.Add(line.Substring(line.IndexOf(argumentDictionary["separator"], StringComparison.InvariantCulture) + 1, line.Length - line.IndexOf(argumentDictionary["separator"], StringComparison.InvariantCulture) - 1));
        }

        fileContent = fileTransformed;
      }

      if (argumentDictionary["samename"] == "true")
      {
        try
        {
          using (StreamWriter sw = new StreamWriter(argumentDictionary["filename"]))
          {
            foreach (string line in fileContent)
            {
              sw.WriteLine(line);
            }
          }
        }
        catch (Exception exception)
        {
          if (argumentDictionary["log"] == "true")
          {
            Log(Settings.Default.LogFileName, $"The filename is: {argumentDictionary["filename"]} cannot be written");
            Log(Settings.Default.LogFileName, $"The exception is: {exception}");
          }
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
              sw.WriteLine(line);
            }
          }
        }
        catch (Exception exception)
        {
          if (argumentDictionary["log"] == "true")
          {
            Log(Settings.Default.LogFileName, $"The filename is: {argumentDictionary["newname"]} cannot be written");
            Log(Settings.Default.LogFileName, $"The exception is: {exception}");
          }
        }
      }
    }

    private static string RemoveForbiddenCharaters(string originalString, IEnumerable<string> listOfForbiddenCharacters)
    {
      string result = originalString;
      foreach (string character in listOfForbiddenCharacters)
      {
        result = result.Replace(character, "_");
      }

      return result;
    }

    private static string RemoveWindowsForbiddenCharaters(string originalString, string characterToReplace = "_")
    {
      string result = originalString;
      foreach (string character in new List<string>
      {"/", "\\", ":", "*", "?", "\"", "<", ">", "|"})
      {
        result = result.Replace(character, characterToReplace);
      }

      return result;
    }

    private static void Log(string filename, string message)
    {
      try
      {
        // remove Windows forbidden characters
        // TODO
        StreamWriter sw = new StreamWriter(filename);
        sw.WriteLine($"{DateTime.Now} - {message}");
        sw.Close();
      }
      catch (Exception exception)
      {
        Console.WriteLine($"There was an error wile writing the file: {filename}. The exception is:{exception}");
      }
    }

    private static void Usage()
    {
      Action<string> display = Console.WriteLine;
      display($"DeleteFileLine is a console application written by Sogeti for {Settings.Default.CompanyName}.");
      display($"Copyrighted (c) 2017 by {Settings.Default.CompanyName}, all rights reserved.");
      display(string.Empty);
      display("Usage of this program:");
      display(string.Empty);
      display("List of arguments:");
      display(string.Empty);
      display("/help (this help)");
      display("/? (this help)");
      display(
        "You can write argument name (not its value) in uppercase or lowercase or a mixed of them (case insensitive)");
      display("/filename is the same as FileName or fileName");
      display("/fileName:<name of the file to be processed> add quotes if file name has space");
      display("/separator:<the CSV separator> semicolon (;) is the default separator");
      display("/hasHeader:<true or false> false by default");
      display("/hasFooter:<true or false> false by default");
      display("/deleteHeader:<true or false> false by default");
      display("/deleteFooter:<true or false> false by default");
      display("/deleteFirstColumn:<true or false> true by default");
      display("/sameName:<true or false> true by default");
      display("/newName:<new name of the file which has been processed> add quotes if file name has space");
      display("/log:<true or false> false by default");
      display(string.Empty);
      display("Example:");
      display(string.Empty);
      display("DeleteFileLine /filename:MyCSVFile.txt /separator:, /hasheader:true /hasfooter:true /deleteheader:true /deletefooter:true /deletefirstcolumn:true /log:true");
      display(string.Empty);
      display("DeleteFileLine /help (this help)");
      display(string.Empty);
    }
  }
}