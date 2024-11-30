using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Stockfish.NET
{
    public class StockfishProcess
    {
        private ProcessStartInfo _processStartInfo { get; set; }

        private Process _process { get; set; }

        public StockfishProcess(string path)
        {
            
            //TODO: need add method which should be depended on os version
            _processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            _process = new Process {StartInfo = _processStartInfo};
        }

        public StockfishProcess(Process process)
        {
            _process = process;
        }
        
        public void Wait(int millisecond)
        {
            this._process.WaitForExit(millisecond);
        }

        public void WriteLine(string command)
        {
            if (_process.StandardInput == null)
            {
                throw new NullReferenceException();
            }
            _process.StandardInput.WriteLine(command);
            _process.StandardInput.Flush();
        }
        
        public string ReadLine()
        {
            if (_process.StandardOutput == null)
            {
                throw new NullReferenceException();
            }
            return _process.StandardOutput.ReadLine();
        }

        public void Start()
        {
            _process.Start();
        }



        ~StockfishProcess()
        {
            _process.Close();
        }
    }
}