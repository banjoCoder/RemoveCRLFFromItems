﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveCRLFFromItems
{
    class Program
    {
    	private const string NEW_LINE_DELIMITER = "\r\n";
        private static readonly char[] _newLineDelimiters = { '\r', '\n' };
        private static readonly char[] _itemsToClean = { '\r', '\n' };

        static void Main(string[] args)
        {
            try
            {
#if DEBUG                
                
                string path = @"PATH_TO_LOCAL_FILE";                
                string outputPath = @"OUTPUT_DIRECTORY";
                bool inParallel = false;                
                
#else

                if (args.Length == 0)
                {
                    Console.WriteLine("\r\n   [sourceFile]\t[outputDir]\tCreates 'CLEAN' version of\t\t\t\t\t\t\tspecified file");
                    Console.WriteLine("\r\n-p [sourceDir]\t[outputDir]\tIn parallel processes all files in\t\t\t\t\t\tdirectory and outputs 'CLEAN' versions");
                    return;
                }

                string path;
                string outputPath;
                bool inParallel = args[0].ToLower() == "-p";

#endif

                if (inParallel)
                {
                    path = args[1];

                    var tmpop = args[2].Replace("\"", string.Empty);
                    outputPath = tmpop.EndsWith("\\") ? tmpop : tmpop + "\\";

                    if (CanProcessFile(path, outputPath))
                    {
                        // get list of files
                        var files = System.IO.Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                        Parallel.ForEach(files, f =>
                        {
							int status = ProcessFile(f, outputPath);

	                        if(status == -1)
	                        	Console.WriteLine("Unexpected character during file processing");
	                        else
	                        	Console.WriteLine("File prcessing complete");
	                        });
                    }
                    else
                    {
                        Console.WriteLine("\r\nInvalid paths. \r\nInput File: \"{0}\"\r\nOutput Dir.: \"{1}\"", path, outputPath);
                    }
                }
                else
                {

#if !DEBUG

                    path = args[0].Replace("\"", string.Empty);
                    var tmpop = args[1].Replace("\"", string.Empty);
                    outputPath = tmpop.EndsWith("\\") ? tmpop : tmpop + "\\";

#endif

                    if (CanProcessFile(path, outputPath))
                    {
                        int status = ProcessFile(path, outputPath);

                        if(status == -1)
                        	Console.WriteLine("Unexpected character during file processing");
                        else
                        	Console.WriteLine("File prcessing complete");
                    }
                    else
                    {
                        Console.WriteLine("\r\nInvalid paths. \r\nInput File: \"{0}\"\r\nOutput Dir.: \"{1}\"", path, outputPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\n" + e.Message);
            }
        }

        private static int ProcessFile(string path, string outputPath)
        {
            var ext = System.IO.Path.GetExtension(path);
            string outputFileName = System.IO.Path.GetFileNameWithoutExtension(path) + "_CLEAN_.txt";

            using (StreamWriter sw = new StreamWriter(outputPath + outputFileName, true))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    return ProcessStream(fs, sw);
                }
            }
        }

        private static bool CanProcessFile(string inputFile, string outputPath)
        {
            return System.IO.File.Exists(inputFile) && System.IO.Directory.Exists(outputPath);
        }
        /// <summary>
        /// Reads input stream and removes unwanted items. 
        /// </summary>
        /// <param name="fs">Stream reader</param>
        /// <param name="sw">Stream writer</param>
        /// <returns>Final state. 1 is success, -1 is failure</returns>
        public static int ProcessStream(FileStream fs, StreamWriter sw)
        {
            int index = 1;
            bool isEOF = false;
            bool toClean = false;
            bool error = false;
            int finalStatus = 0;
            char c = '\0';

            // set stream to beginning
            fs.Seek(0, SeekOrigin.Begin);

            // set initial state
            int q = 0;
            
            while (isEOF == false)
            {// begin reading the stream
                c = (char)fs.ReadByte();
                
                if ((q == 0 || q == 4) && c == '"')
                {// opening quotes of item
                    sw.Write(c);
                    q = 1;
                }
                else if ((q == 1 || q == 3 ) && c == '"')
                {// closing quote of item
                    sw.Write(c);
                    q = 2;
                }
                else if((q == 1 || q == 3 || q == 4) && (toClean = Program._itemsToClean.Contains(c)))
                {// skip ignore values; replace new lines with space...prevents string concatination
                    if(toClean)
                        sw.Write(" ");

                    q = 3;
                }                
                else if(q == 2 && c == ',')
                {// transition back to initial state
                    sw.Write(c);
                    q = 0;
                }                     
                else if (q == 2 && Program._newLineDelimiters.Contains(c))
                {// reached end of the line

					sw.Write(Program.NEW_LINE_DELIMITER);

                    q = 4;
                    Console.WriteLine(++index);
                }                     
                else if(q == 1 || q == 2 || q == 3)
                {// valid character write to file
                    sw.Write(c);
                    q = 1;
                }  
                else if ((error = q == 0) || c == char.MaxValue)
                {// exit reader...
                    if (error)
					    finalStatus = -1;
                    else
                        finalStatus = 1;

                    isEOF = true;
                }              
            }// end while

            return finalStatus;
        }// end function     
    }
}
