//Christian Ling

//File to mimic 'du' behavior

//imports
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace du
{	
	
    class Program
    {
		//lock to brevent print statement colision
		private static Object threadLock=new Object();
		
		//hold the output
		static String numFile="";
		static String numByte="";
		static String numFolder="";
		
		//count the files and bytes
		static long numBytes= 0;
		static int numFiles= 0;

		//update via a setter
		public static void updatenumBytes(long bytestoadd){
			numBytes+=bytestoadd;
		}
		
		//parallel read all directories helper function
		//parameters: stopwatch, filepath
		public static void readAllDirectoriesParallel(Stopwatch stopwatch, string path){
			numFiles=0;
			numBytes=0;
			numFile="";
			numByte="";
			numFolder="";
			
			runParallelThread(stopwatch, path);
			//if we get here, we've finished. format and output the results
			
			stopwatch.Stop();
			TimeSpan ts = stopwatch.Elapsed;
			Console.WriteLine("Parallel Calculated in: "+ts.Seconds+"."+ts.Milliseconds+"s");
			Console.Write(numFolder.ToString()+" folders, ");
			Console.Write(numFile.ToString()+" files, ");
			Console.Write(numByte.ToString()+" bytes\n\n");
			
		}
		
		//this is a recursive method to iterate over files in parallel
		public static string[] runParallelThread(Stopwatch stopwatch, string path){
			string[] subdirectories=Directory.GetDirectories(path);//get the working directory
			
			//if there are no children, were done
			if(subdirectories.Length==0){
				return subdirectories;
			}
			
			//otherwise get the children of each folder
			else{
				foreach(string sub in subdirectories){
					try{
						//get the child folder
						string[] childsubdirectories=runParallelThread(stopwatch, sub);
						create a temporary location to append the folders to
						string[] z = new string[subdirectories.Length + childsubdirectories.Length];

						//append the child folders to the subdirectory
						subdirectories.CopyTo(z, 0);
						childsubdirectories.CopyTo(z, subdirectories.Length);
						//copy over the changes to the array
						subdirectories=z;
					}catch(Exception ex){
						
						System.Error.WriteLine("Error occurred in runParallelThread");
					}
				}
			}
			foreach(string s in subdirectories){
				
				try{
					//try to get information from the directories
					DirectoryInfo d = new DirectoryInfo(s);
					FileInfo[] Files = d.GetFiles();
					//update the number of bytes
					Parallel.ForEach(Files,currentbyte=>{
						updatenumBytes(currentbyte.Length);
					});
					numFiles+=Files.Length;
				}
				catch(Exception ex){
					var exception=ex.ToString();
				}
			}
			numFolder=subdirectories.Length.ToString("#,##0");
			numByte=numBytes.ToString("#,##0");
			numFile=numFiles.ToString("#,##0");
			
			return subdirectories;
		}
		
		//Serial helper method for file reading
		public static void readAllDirectoriesSerial(Stopwatch stopwatch, string path){
			numFile="";

			numByte="";
			numFolder="";
			
			runSerialThread(stopwatch, path);
			//when we've gotten to this point we can print
			stopwatch.Stop();
			TimeSpan ts = stopwatch.Elapsed;
			Console.WriteLine("Sequential Calculated in: "+ts.Seconds+"."+ts.Milliseconds+"s");
			Console.Write(numFolder.ToString()+" folders, ");
			Console.Write(numFile.ToString()+" files, ");
			Console.Write(numByte.ToString()+" bytes\n\n");
			
			
		}
		
		//main method
		public static string[] runSerialThread(Stopwatch stopwatch, string path){
			string[] subdirectories=Directory.GetDirectories(path);
			if(subdirectories.Length==0){
				return subdirectories;
			}
			
			long numBytes= 0;
			
			foreach(string s in subdirectories){
				try{
					DirectoryInfo d = new DirectoryInfo(s);
					FileInfo[] Files = d.GetFiles();
					foreach(FileInfo file in Files )
					{
						numBytes+=file.Length;
					}
					numFiles+=Files.Length;
					string[] childsubdirectories=runSerialThread(stopwatch, s);
					string[] z = new string[subdirectories.Length + childsubdirectories.Length];
					subdirectories.CopyTo(z, 0);
					childsubdirectories.CopyTo(z, subdirectories.Length);
					subdirectories=z;
				}
				catch(Exception ex){
					var exception=ex.ToString();
				}
			}
			

			numFolder=subdirectories.Length.ToString("#,##0");
			numByte=numBytes.ToString("#,##0");
			numFile=numFiles.ToString("#,##0");
			
			return subdirectories;
		}
        static void Main(string[] args)
        {
			//make sure parameters are correct
			if(args.Length!=2){
				Console.Error.WriteLine("Usage: dotnet run [ -s | -p | -b ] <path>");
				Environment.Exit(0);
			}
			if(args[0] != "-s" && args[0] != "-p" && args[0] != "-b"){
				Console.Error.WriteLine("Usage: dotnet run [ -s | -p | -b ] <path>");
				Environment.Exit(0);
			}
			
			//stopwatches
			Stopwatch stopwatch=new Stopwatch();
			Stopwatch stopwatch2=new Stopwatch();
			
			//printing header
			Console.WriteLine("Directory "+args[1]+":\n");

			//child threads for reading
			Thread childThread2=new Thread(() => readAllDirectoriesParallel(stopwatch, args[1]));
			Thread childThread=new Thread(() => readAllDirectoriesSerial(stopwatch2, args[1]));
			if (Directory.Exists(args[1])){
				
				//run in parallel
				if(args[0]=="-p" || args[0]=="-b"){
					try{
						stopwatch.Start();
						stopwatch2.Start();
						childThread2.Start();
						
					}catch(UnauthorizedAccessException ex){
						Console.Error.WriteLine("error starting thread 1");
					}
				}
				
				//while the first thread has not completed, do not proceed
				while(childThread2.IsAlive){
					try{
						Thread.Sleep(1);
					catch(Exception e){
						Console.Error.WriteLine("error starting thread 2");
					}
				}
				
				//thread safety so we dont have darkwing duck threading
				lock(threadLock){
					
					//serial or both
					if((args[0]=="-s" || args[0]=="-b") && Directory.Exists(args[1])){
						try{
							//start the thread
							childThread.Start();
							childThread.Join();
						}catch(UnauthorizedAccessException ex){
							Console.Error.WriteLine("error starting thread 2");
						}
					}
				}

			}
			else{
				Console.Error.WriteLine("Path not found");
			}
        }
    }
}
