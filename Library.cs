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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GArguS.FFMPEG
{
    public static class ListExtenstion
    {
        public static string GetString(this List<string> arr)
        {
            string _str = String.Empty;
            foreach (var __str in arr)
                _str = _str + __str;
            return _str;
        }
    }

    public class ffmpeglib
    {
        public enum FFLags
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

        public enum MultimediaFilters
        {
            concat,
            segment
        };

        public static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public List<string> _args = new List<string>() { "ffmpeg " };
        public List<string> _filters = new List<string>() { "-fflags" };
        public List<string> _fflags = new List<string>() { "-f" };

        public ffmpeglib()
        {
        }

        public ffmpeglib(params string[] args)
        {
            _args = args.ToList();
        }

        public void CompileArgs()
        {
            _args.AddRange(_filters);
            _args.AddRange(_fflags);
        }

        public void Run()
        {
            Console.WriteLine("Running with arguments: " + _args.GetString());
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
    }
}