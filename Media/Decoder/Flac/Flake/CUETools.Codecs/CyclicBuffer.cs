﻿using System;
using System.IO;
using System.Threading;

namespace CUETools.Codecs
{
    public class CyclicBuffer : IDisposable
    {
        private readonly byte[] _buffer;
        private readonly int _size;
        private int _start = 0; // moved only by Write
        private int _end = 0; // moved only by Read
        private bool _eof = false;
        private Thread _readThread = null, _writeThread = null;
        private Exception _ex = null;

        private int DataAvailable
        {
            get
            {
                return _end - _start;
            }
        }

        private int FreeSpace
        {
            get
            {
                return _size - DataAvailable;
            }
        }

        public CyclicBuffer(int len)
        {
            _size = len;
            _buffer = new byte[len];
        }

        public CyclicBuffer(int len, Stream input, Stream output)
        {
            _size = len;
            _buffer = new byte[len];
            ReadFrom(input);
            WriteTo(output);
        }

        public void ReadFrom(Stream input)
        {
            _readThread = new Thread(PumpRead)
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true
            };
            _readThread.Start(input);
        }

        public void WriteTo(Stream output)
        {
            WriteTo(FlushOutputToStream, CloseOutputToStream, ThreadPriority.Highest, output);
        }

        public void WriteTo(FlushOutput flushOutputDelegate, CloseOutput closeOutputDelegate, ThreadPriority priority, object to)
        {
            if (flushOutputDelegate != null)
                FlushOutputEvent += flushOutputDelegate;
            if (closeOutputDelegate != null)
                CloseOutputEvent += closeOutputDelegate;
            _writeThread = new Thread(FlushThread)
            {
                Priority = priority,
                IsBackground = true
            };
            _writeThread.Start(to);
        }

        private void CloseOutputToStream(object to)
        {
            ((Stream)to).Close();
        }

        private void FlushOutputToStream(byte[] buffer, int pos, int chunk, object to)
        {
            ((Stream)to).Write(buffer, pos, chunk);
        }

        private void PumpRead(object o)
        {
            while (Read((Stream)o))
                ;
            SetEOF();
        }

        public void Close()
        {
            if (_readThread != null)
            {
                _readThread.Join();
                _readThread = null;
            }
            SetEOF();
            if (_writeThread != null)
            {
                _writeThread.Join();
                _writeThread = null;
            }
        }

        public void SetEOF()
        {
            lock (this)
            {
                _eof = true;
                Monitor.Pulse(this);
            }
        }

        public bool Read(Stream input)
        {
            int pos, chunk;
            lock (this)
            {
                while (FreeSpace == 0 && _ex is null)
                    Monitor.Wait(this);
                if (_ex != null)
                    throw _ex;
                pos = _end % _size;
                chunk = Math.Min(FreeSpace, _size - pos);
            }
            chunk = input.Read(_buffer, pos, chunk);
            if (chunk == 0)
                return false;
            lock (this)
            {
                _end += chunk;
                Monitor.Pulse(this);
            }
            return true;
        }

        public void Read(byte[] array, int offset, int count)
        {
            int pos, chunk;
            while (count > 0)
            {
                lock (this)
                {
                    while (FreeSpace == 0 && _ex is null)
                        Monitor.Wait(this);
                    if (_ex != null)
                        throw _ex;
                    pos = _end % _size;
                    chunk = Math.Min(FreeSpace, _size - pos);
                    chunk = Math.Min(chunk, count);
                }
                Array.Copy(array, offset, _buffer, pos, chunk);
                lock (this)
                {
                    _end += chunk;
                    Monitor.Pulse(this);
                }
                count -= chunk;
                offset += chunk;
            }
        }

        public void Write(byte[] buff, int offs, int count)
        {
            while (count > 0)
            {
                int pos, chunk;
                lock (this)
                {
                    while (DataAvailable == 0 && !_eof)
                        Monitor.Wait(this);
                    if (DataAvailable == 0)
                        break;
                    pos = _start % _size;
                    chunk = Math.Min(DataAvailable, _size - pos);
                }
                if (FlushOutputEvent != null)
                    Array.Copy(_buffer, pos, buff, offs, chunk);
                offs += chunk;
                lock (this)
                {
                    _start += chunk;
                    Monitor.Pulse(this);
                }
            }
        }

        private void FlushThread(object to)
        {
            while (true)
            {
                int pos, chunk;
                lock (this)
                {
                    while (DataAvailable == 0 && !_eof)
                        Monitor.Wait(this);
                    if (DataAvailable == 0)
                        break;
                    pos = _start % _size;
                    chunk = Math.Min(DataAvailable, _size - pos);
                }
                if (FlushOutputEvent != null)
                    try
                    {
                        FlushOutputEvent(_buffer, pos, chunk, to);
                    }
                    catch (Exception ex)
                    {
                        lock (this)
                        {
                            _ex = ex;
                            Monitor.Pulse(this);
                            return;
                        }
                    }
                lock (this)
                {
                    _start += chunk;
                    Monitor.Pulse(this);
                }
            }
            CloseOutputEvent?.Invoke(to);
        }

        public void Dispose()
        {
            Close();
        }

        public delegate void FlushOutput(byte[] buffer, int pos, int chunk, object to);
        public delegate void CloseOutput(object to);

        public event FlushOutput FlushOutputEvent;
        public event CloseOutput CloseOutputEvent;
    }
}
