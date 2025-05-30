﻿using Commons;
using System;
using System.IO;
using System.Runtime.InteropServices;

// Initial inspiration go to Jackson Dunstan for his article (http://jacksondunstan.com/articles/3568)

namespace ATL
{
    /// <summary>
    /// Reads data from the given Stream using a forward buffer in order to reduce disk stress and have better control of when data is actually read from the disk.
    /// 
    /// NB1 : Using BufferedBinaryReader instead of the classic BinaryReader creates a ~10% speed gain on the dev environment (MS .NET under Windows)
    /// 
    /// NB2 : The interface of this class is designed to be called exactly like a BinaryReader in order to facilitate swapping in classes that use BinaryReader
    /// However, is does _not_ give access to BaseStream, in order to keep control on buffer and cursor positions.
    /// 
    /// NB3 : This class implements Stream in order to be reusable in methods that take Stream as an input
    /// </summary>
    internal sealed class BufferedBinaryReader : Stream, IDisposable
    {
        private readonly Stream stream;
        private readonly int bufferDefaultSize;
        private readonly long streamSize;

        /*
         *
         * stream ------------[================>=======================]--------------------------------------*EOS
         *                    ^                ^                       ^
         *                    bufferOffset     cursorPosition          streamPosition
         *                    (absolute)       (relative to buffer)    (absolute)
         */
        private byte[] buffer;
        private long bufferOffset;
        private int cursorPosition; // NB : cursorPosition can be > bufferSize in certain cases when BufferedBinaryReader has to read a chunk of data larger than bufferSize
        private long streamPosition;
        private int bufferSize; // NB : bufferSize can be < DEFAULT_BUFFER_SIZE when bufferOffset nears the end of the stream (not enough remaining bytes to fill the whole buffer space)

        /// Mandatory override to Stream.Position
        public override long Position
        {
            get { return bufferOffset + cursorPosition; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        /// Mandatory override to Stream.Length
        public override long Length
        {
            get { return streamSize; }
        }

        /// Mandatory override to Stream.CanRead
        public override bool CanRead
        {
            get { return true; }
        }

        /// Mandatory override to Stream.CanSeek
        public override bool CanSeek
        {
            get { return true; }
        }

        /// Mandatory override to Stream.CanWrite
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Construct a new instance of BufferedBinaryReader using the given Stream
        /// </summary>
        /// <param name="stream">Stream to read</param>
        public BufferedBinaryReader(Stream stream)
        {
            this.stream = stream;
            bufferDefaultSize = Settings.FileBufferSize;
            buffer = new byte[bufferDefaultSize];
            streamSize = stream.Length;
            streamPosition = stream.Position;
            bufferOffset = streamPosition;
        }

        /// <summary>
        /// Construct a new instance of BufferedBinaryReader using the given Stream and buffer size
        /// </summary>
        /// <param name="stream">Stream to read</param>
        /// <param name="bufferSize">Buffer size to use</param>
        public BufferedBinaryReader(Stream stream, int bufferSize)
        {
            this.stream = stream;
            bufferDefaultSize = bufferSize;
            buffer = new byte[bufferSize];
            streamSize = stream.Length;
            streamPosition = stream.Position;
            bufferOffset = streamPosition;
        }

        // NB : cannot handle when previousBytesToKeep > bufferSize
        private bool fillBuffer(int previousBytesToKeep = 0)
        {
            if (previousBytesToKeep > 0) Array.Copy(buffer, cursorPosition, buffer, 0, previousBytesToKeep);
            int bytesToRead = (int)Math.Max(0, Math.Min(bufferDefaultSize - previousBytesToKeep, streamSize - streamPosition - previousBytesToKeep));

            bufferOffset = streamPosition - previousBytesToKeep;
            cursorPosition = 0;

            if (bytesToRead > 0)
            {
                stream.Read(buffer, previousBytesToKeep, bytesToRead);
                streamPosition += bytesToRead;
                bufferSize = bytesToRead + previousBytesToKeep;
                return true;
            }

            return false;
        }

        private bool prepareBuffer(int bytesToRead)
        {
            if (bufferSize - cursorPosition < bytesToRead)
            {
                return fillBuffer(Math.Max(0, bufferSize - cursorPosition));
            }
            return false;
        }

        /// Mandatory override to Stream.Seek
        public override long Seek(long offset, SeekOrigin origin)
        {
            long delta = 0; // Distance between absolute cursor position and specified position in bytes

            if (origin.Equals(SeekOrigin.Current)) delta = offset;
            else if (origin.Equals(SeekOrigin.Begin))
            {
                delta = offset - Position;
            }
            else if (origin.Equals(SeekOrigin.End))
            {
                delta = streamSize + offset - Position;
            }

            if (0 == delta) return Position;
            else if (delta < 0)
            {
                // If cursor is still within buffer, jump within buffer
                if ((cursorPosition + delta < bufferSize) && (cursorPosition + delta >= 0))
                {
                    cursorPosition += (int)delta;
                }
                else // Jump outside buffer : move the whole buffer at the beginning of the zone to read
                {
                    streamPosition = bufferOffset + cursorPosition + delta;
                    stream.Position = streamPosition;
                    fillBuffer();
                }
            }
            else if (cursorPosition + delta < bufferSize) // Jump within buffer
            {
                cursorPosition += (int)delta;
            }
            else // Jump outside buffer: move the whole buffer at the beginning of the zone to read
            {
                streamPosition = bufferOffset + cursorPosition + delta;
                stream.Position = streamPosition;
                fillBuffer();
            }
            return Position;
        }

        /// Mandatory override to Stream.Read
        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            // Bytes to read are all already buffered
            if (count <= bufferSize - cursorPosition)
            {
                prepareBuffer(count);
                Array.Copy(this.buffer, cursorPosition, buffer, offset, count);
                cursorPosition += count;
                return count;
            }
            else
            {
                // First retrieve buffered data if possible
                int availableBytes = bufferSize - cursorPosition;
                if (availableBytes > 0)
                {
                    Array.Copy(this.buffer, cursorPosition, buffer, offset, availableBytes);
                }
                else
                {
                    availableBytes = 0;
                }

                // Then retrieve the rest by reading the stream
                int readBytes = stream.Read(buffer, offset + availableBytes, count - availableBytes);

                streamPosition += readBytes;
                stream.Position = streamPosition;

                cursorPosition += availableBytes + readBytes; // Virtual position outside buffer zone

                return (availableBytes + readBytes);
            }
        }

        public bool PeekChar()
        {
            return prepareBuffer(1);
        }

        /// <summary>
        /// Read an array of bytes of the given length from the current position
        /// </summary>
        /// <param name="nbBytes">Number of bytes to read</param>
        /// <returns>Array of bytes of the given length read from the current position</returns>
        public byte[] ReadBytes(int nbBytes)
        {
            byte[] buf = new byte[nbBytes];

            Read(buf, 0, nbBytes);

            return buf;
        }

        /// <summary>
        /// Read an array of Latin-1 encoded chars of the given length from the current position
        /// </summary>
        /// <param name="nbBytes">Number of bytes to read</param>
        /// <returns>Array of Latin-1 encoded chars of the given length read from the current position</returns>
        public char[] ReadChars(int nbBytes)
        {
            return Utils.Latin1Encoding.GetChars(ReadBytes(nbBytes));
        }

        /// <summary>
        /// Read a single byte from the current position
        /// </summary>
        /// <returns>Byte read from the current position</returns>
        public new byte ReadByte()
        {
            prepareBuffer(1);
            byte val = buffer[cursorPosition];
            cursorPosition++;
            return val;
        }

        /// <summary>
        /// Read a single signed byte from the current position
        /// </summary>
        /// <returns>Signed byte read from the current position</returns>
        public sbyte ReadSByte()
        {
            prepareBuffer(1);
            sbyte val = (sbyte)buffer[cursorPosition];
            cursorPosition++;
            return val;
        }

        /// <summary>
        /// Read a single unsigned Int16 from the current position
        /// </summary>
        /// <returns>Unsigned Int16 read from the current position</returns>
        public ushort ReadUInt16()
        {
            prepareBuffer(2);
            ushort val = (ushort)(buffer[cursorPosition] | buffer[cursorPosition + 1] << 8);
            cursorPosition += 2;
            return val;
        }

        /// <summary>
        /// Read a single signed Int16 from the current position
        /// </summary>
        /// <returns>Signed Int16 read from the current position</returns>
        public short ReadInt16()
        {
            prepareBuffer(2);
            short val = (short)(buffer[cursorPosition] | buffer[cursorPosition + 1] << 8);
            cursorPosition += 2;
            return val;
        }

        /// <summary>
        /// Read a single unsigned Int32 from the current position
        /// </summary>
        /// <returns>Unsigned Int32 read from the current position</returns>
        public uint ReadUInt32()
        {
            prepareBuffer(4);
            uint val = (uint)(buffer[cursorPosition] | buffer[cursorPosition + 1] << 8 | buffer[cursorPosition + 2] << 16 | buffer[cursorPosition + 3] << 24);
            cursorPosition += 4;
            return val;
        }

        /// <summary>
        /// Read a single signed Int32 from the current position
        /// </summary>
        /// <returns>Signed Int32 read from the current position</returns>
        public int ReadInt32()
        {
            prepareBuffer(4);
            int val = (buffer[cursorPosition] | buffer[cursorPosition + 1] << 8 | buffer[cursorPosition + 2] << 16 | buffer[cursorPosition + 3] << 24);
            cursorPosition += 4;
            return val;
        }

        /// <summary>
        /// Read a single unsigned Int64 from the current position
        /// </summary>
        /// <returns>Unsigned Int64 read from the current position</returns>
        public ulong ReadUInt64()
        {
            prepareBuffer(8);
            ulong val = buffer[cursorPosition] | (ulong)buffer[cursorPosition + 1] << 8 | (ulong)buffer[cursorPosition + 2] << 16 | (ulong)buffer[cursorPosition + 3] << 24 | (ulong)buffer[cursorPosition + 4] << 32 | (ulong)buffer[cursorPosition + 5] << 40 | (ulong)buffer[cursorPosition + 6] << 48 | (ulong)buffer[cursorPosition + 7] << 56;
            cursorPosition += 8;
            return val;
        }

        /// <summary>
        /// Read a single signed Int64 from the current position
        /// </summary>
        /// <returns>Signed Int64 read from the current position</returns>
        public long ReadInt64()
        {
            prepareBuffer(8);
            long val = buffer[cursorPosition] | (long)buffer[cursorPosition + 1] << 8 | (long)buffer[cursorPosition + 2] << 16 | (long)buffer[cursorPosition + 3] << 24 | (long)buffer[cursorPosition + 4] << 32 | (long)buffer[cursorPosition + 5] << 40 | (long)buffer[cursorPosition + 6] << 48 | (long)buffer[cursorPosition + 7] << 56;
            cursorPosition += 8;
            return val;
        }

        /// Override to IDisposable.Dispose
        public new void Dispose()
        {
            Flush();
            stream.Close();
        }

        /// Mandatory override to Stream.Flush
        public override void Flush()
        {
            buffer = null;
        }

        /// Mandatory override to Stream.SetLength
        public override void SetLength(long value)
        {
            throw new NotImplementedException(); // This class is a _reader_ helper only
        }

        /// Mandatory override to Stream.Write
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException(); // This class is a _reader_ helper only
        }
    }
}
