/*********************************************************************************

    GarguS.FFMPEGLib: Just a FFMPEG Library for GArguS
    Copyright (C) 2022 Diklos Group Ltd.

    This file is part of GarguS.FFMPEGLib

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

***********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GArguS.FFMPEG
{
    public static class ListExtenstion
    {
        /// <summary>
        /// Return one line string from string List Array
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static string GetString(this List<string> arr)
        {
            string _str = String.Empty;
            foreach (var __str in arr)
                _str = _str + __str;
            return _str;
        }
    }

    /// <summary>
    /// SmartArray - Just a List<T> clone, but has limited size and older element autocleanup
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SmartArray<T> : List<T>
    {
        private readonly int _arrCapacity;

        public SmartArray(int num)
        {
            _arrCapacity = num;
        }

        public void InsertLine(T item)
        {
            if (this.Count < _arrCapacity) // if the current size does not exceed the _arrCapacity
            {
                this.Add(item);
            }
            else // else remove older elements from array and insert newer
            {
                this.RemoveRange(0, (this.Count - _arrCapacity));
                this.Add(item);
            }
        }
    }
    public class FFMpegLibEventArgs : EventArgs
    {
        public string _runArgs = String.Empty;
        public string _workingDirectory = String.Empty;
        public int _id = 0;
    }
    public class ffmpeglib
    {
        public event ErrorHandler onError;
        public event WarningHandler onWarn;
        public event OnNewSegmentHandler onNewSegment;

        public FFMpegLibEventArgs fea = new FFMpegLibEventArgs();

        public delegate void ErrorHandler(ffmpeglib fl, FFMpegLibEventArgs fe);
        public delegate void WarningHandler(ffmpeglib fl, FFMpegLibEventArgs fe);
        public delegate void OnNewSegmentHandler(ffmpeglib fl, FFMpegLibEventArgs fe);

        public SmartArray<string> FFMpegLogs = new SmartArray<string>(100); //Smart array for STDOUT logs from FFMPEG
        public string WorkingDir = "";

        public enum FFLags //fflags from: https://ffmpeg.org/ffmpeg-formats.html#toc-Format-Options
        {
            discardcorrupt,
            fastseek,
            genpts,
            igndts,
            ignidx,
            nobuffer,
            nofillin,
            noparse,
            sortdts
        };

        public enum MultimediaFilters //https://ffmpeg.org/ffmpeg-filters.html#Multimedia-Filters
        {
            concat,
            segment
        };

        public static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public List<string> _args = new List<string>(); // ready CLI Argument to call ffmpeg
        public List<string> _filters = new List<string>() { "-fflags" }; //multimedia filters args
        public List<string> _fflags = new List<string>() { "-f" }; //fflags args

        public static object durlocker = new();
        public static int getDur(string filename)
        {
            lock (durlocker)
            {
                string filePath = filename;
                string cmd =
                    string.Format(
                        "-v error  -select_streams v:0 -show_entries stream=duration -of  default=noprint_wrappers=1:nokey=1  {0}",
                        filePath);
                Process proc = new Process();
                string runpath = Directory.GetCurrentDirectory();
                proc.StartInfo.FileName = runpath + "/ffprobe.exe";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    proc.StartInfo.FileName = runpath + "/ffprobe";
                }
                proc.StartInfo.Arguments = cmd;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.UseShellExecute = false;
                if (!proc.Start())
                {
                    Console.WriteLine("Error starting");
                }

                string duration = proc.StandardOutput.ReadToEnd().Replace("\n", "");
                //Remove the milliseconds
                string[] dur = duration.Split(".");
                proc.WaitForExit();
                proc.Close();
                return int.Parse(dur[0]);
            }
        }

        public ffmpeglib(params string[] args)
        {
            _args = args.ToList();
        }

        public void SetWorkingDir(string dir)
        {
            WorkingDir = dir;
        }

        public void CompileArgs() //Join filters and fflags args to main args array
        {
            _args.AddRange(_filters);
            _args.AddRange(_fflags);
        }
        object locker = new();

        public void Run()
        {
            lock (locker)
            {
                Debug.WriteLine("Running with arguments: " + _args.GetString());
                //* Create your Process
                Process process = new Process();
                string runpath = Directory.GetCurrentDirectory();
                process.StartInfo.FileName = runpath + "/ffmpeg.exe";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    process.StartInfo.FileName = runpath + "/ffmpeg";
                }
                if (!Directory.Exists(WorkingDir))
                {
                    Console.WriteLine("Creating directory: " + WorkingDir);
                    Directory.CreateDirectory(WorkingDir);
                }

                process.StartInfo.Arguments = _args.GetString();
                process.StartInfo.WorkingDirectory = WorkingDir;
/*#if DEBUG
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardError = false;

            process.Start();
            process.WaitForExit();
#endif*/
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                //* Set your output and error (asynchronous) handlers
                process.OutputDataReceived += (s, e) => OutputHandler(s, e);
                process.ErrorDataReceived += (s, e) => ErrorOutputHandler(s, e);
                fea._runArgs = _args.GetString();
                fea._workingDirectory = WorkingDir;
                //* Start process and handlers
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }

        public void SetFFlag(FFLags _fflag)
        {
            if (_fflag != null)
            {
                _fflags.Add(" " + _fflag.ToString() + " ");
            }
        }

        public void SetMultimediaFilter(MultimediaFilters _filter)
        {
            if (_filter != null)
            {
                _filters.Add(" " + _filter.ToString() + " ");
            }
        }

        public  void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            
            if (outLine.Data != null)
            {
                FFMpegLogs.InsertLine(outLine.Data);
            }
        }

        public void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            onError(this, fea);
            if (outLine.Data != null)
            {
                FFMpegLogs.InsertLine(outLine.Data);
            }
        }
    }
}
