using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.IO;

namespace FFmpegUtils
{
	public static class ErrorHandling
	{
		static List<Byte> m_error = new List<byte>();
		public static void OnRead(Byte[] buffer, int count)
		{
			lock (((ICollection)m_error).SyncRoot)
			{
				m_error.AddRange(buffer.Take(count));
			}
		}
		public static Byte[] Dequeue()
		{
			Byte[] tmp;
			lock (((ICollection)m_error).SyncRoot)
			{
				tmp = m_error.ToArray();
				m_error.Clear();
			}
			return tmp;
		}
	}

	public class Disposable : IDisposable
	{
		public bool IsDisposed
		{
			get;
			private set;
		}
		public void Dispose()
		{
			IsDisposed = true;    
		}
	}

	public static class RecursiveReader
	{
		public delegate void OnReadFunc(Byte[] bytes, int count);

		public delegate void OnCompleteFunc();

		public static IDisposable BeginRead(this Stream s, Byte[] buffer, OnReadFunc onRead, OnCompleteFunc onComplete = null)
		{
			var disposable = new Disposable();
			s.BeginRead(buffer, onRead, onComplete, () => disposable.IsDisposed);
			return disposable;
		}

		static void BeginRead(this Stream s, Byte[] buffer, OnReadFunc onRead, OnCompleteFunc onComplete, Func<bool> IsDisposed)
		{
			if (IsDisposed()) return;

			AsyncCallback callback = ar => {
				var ss = ar.AsyncState as Stream;
				var readCount = ss.EndRead(ar);
				if (readCount == 0)
				{
					if (onComplete != null)
					{
						onComplete();
					}
					return;
				}

				onRead(buffer, readCount);

				BeginRead(ss, buffer, onRead, onComplete, IsDisposed);
			};
			s.BeginRead(buffer, 0, buffer.Length, callback, s);
		}
	}

    public static class ArraySegmentExtensions
    {
        public static T Get<T>(this ArraySegment<T> s, int index)
        {
            if (index >= s.Count) throw new IndexOutOfRangeException();
            return s.Array[s.Offset + index];
        }

        public static IEnumerable<T> ToE<T>(this ArraySegment<T> s)
        {
            return s.Array.Skip(s.Offset).Take(s.Count);
        }
    }

    public enum YUVFormat
    {
        YUV420,
        YUV444
    }

    [Serializable]
    public class YUVHeader
    {
        public YUVFormat Format;
        public int Width;
        public int Height;

        public YUVHeader(int w, int h, YUVFormat format)
        {
            Width = w;
            Height = h;
            Format = format;
        }

        public override string ToString()
        {
            return String.Format("[{0}]{1}x{2}", Format, Width, Height);
        }

        public int BodyByteLength
        {
            get
            {
                if (Format == YUVFormat.YUV444)
                {
                    return Width * Height * 3;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int YBytesLength
        {
            get
            {
                return Width * Height;
            }
        }

        public int YBytesOffset
        {
            get
            {
                return 0;
            }
        }

        public int UBytesLength
        {
            get
            {
                if (Format == YUVFormat.YUV444)
                {
                    return YBytesLength;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int UBytesOffset
        {
            get
            {
                return YBytesLength;
            }
        }

        public int VBytesLength
        {
            get
            {
                if (Format == YUVFormat.YUV444)
                {
                    return YBytesLength;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int VBytesOffset
        {
            get
            {
                return UBytesOffset + UBytesLength;
            }
        }

        public static YUVHeader Parse(String header)
        {
            int width = 0;
            int height = 0;
            var format = default(YUVFormat);
            foreach (var value in header.Split(' '))
            {
                switch (value.FirstOrDefault())
                {
                    case 'W':
                        width = int.Parse(value.Substring(1));
                        break;

                    case 'H':
                        height = int.Parse(value.Substring(1));
                        break;

                    case 'C':
                        if (value.Equals("C420"))
                        {
                            format = YUVFormat.YUV420;
                        }
                        else if (value.Equals("C444"))
                        {
                            format = YUVFormat.YUV444;
                        }
                        break;
                }
            }
            return new YUVHeader(width, height, format);
        }
    }

    public class YUVFrameReader
    {
        int m_frameNumber = -1;
        public int FrameNumber
        {
            get
            {
                return m_frameNumber;
            }
        }

        List<Byte> m_header = new List<byte>();

        int m_fill;
        Byte[] m_body;
        public Byte[] Body
        {
            get { return m_body; }
        }

        public bool IsFill
        {
            get
            {
                return m_fill >= m_body.Length;
            }
        }

        bool m_isFrameHeader = true;

        public YUVFrameReader(YUVHeader header)
        {
            m_body = new Byte[header.BodyByteLength];
        }

        public void Clear(int number)
        {
            m_isFrameHeader = true;
            m_header.Clear();
            m_fill = 0;
            m_frameNumber = number;
        }

        public int Push(ArraySegment<Byte> bytes, int i)
        {
            if (m_isFrameHeader)
            {
                for (; i < bytes.Count; ++i)
                {
                    if (bytes.Get(i) == 0x0a)
                    {
                        m_isFrameHeader = false;
                        ++i;
                        break;
                    }
                    m_header.Add(bytes.Get(i));
                }
            }

            for (; i < bytes.Count && m_fill < m_body.Length; ++i, ++m_fill)
            {
                m_body[m_fill] = bytes.Get(i);
            }

            return i;
        }
    }

    public class YUVFrame
    {
        public int FrameNumber;
        public Byte[] YBytes;
        public Byte[] UBytes;
        public Byte[] VBytes;

        public YUVFrame()
        {
            FrameNumber = -1;
            YBytes = null;
            UBytes = null;
            VBytes = null;
        }
    }

    [Serializable]
    public class YUVReader
    {
        public YUVHeader Header;

        public List<Byte> m_buffer = new List<byte>();

        Object m_currentLock = new object();
        YUVFrameReader m_current;
        YUVFrameReader m_next;

        YUVFrame m_frame = new YUVFrame();
        public YUVFrame GetFrame()
        {
            if (m_current == null)
            {
                return m_frame;
            }

            lock (m_current)
            {
                if (m_frame.FrameNumber != m_current.FrameNumber)
                {
                    // copy
                    m_frame.FrameNumber = m_current.FrameNumber;
                    if (m_frame.YBytes == null || m_frame.YBytes.Length != Header.YBytesLength)
                    {
                        m_frame.YBytes = new Byte[Header.YBytesLength];
                    }
                    if (m_frame.UBytes == null || m_frame.UBytes.Length != Header.UBytesLength)
                    {
                        m_frame.UBytes = new Byte[Header.UBytesLength];
                        // for testing set all bytes to zero
                        //for (int i = 0; i < m_frame.UBytes.Length; i++)
                        //    m_frame.UBytes[i] = 0;
                    }
                    if (m_frame.VBytes == null || m_frame.VBytes.Length != Header.VBytesLength)
                    {
                        m_frame.VBytes = new Byte[Header.VBytesLength];
                        // for testing set all bytes to zero
                        //for (int i = 0; i < m_frame.UBytes.Length; i++)
                        //    m_frame.UBytes[i] = 0;
                    }

                    Array.Copy(m_current.Body, Header.YBytesOffset, m_frame.YBytes, 0, Header.YBytesLength);
                    Array.Copy(m_current.Body, Header.UBytesOffset, m_frame.UBytes, 0, Header.UBytesLength);
                    Array.Copy(m_current.Body, Header.VBytesOffset, m_frame.VBytes, 0, Header.VBytesLength);
                }
            }
            return m_frame;
        }

        public YUVReader()
        {
        }

        public void Push(ArraySegment<Byte> bytes)
        {
            if (Header == null)
            {
                m_buffer.AddRange(bytes.ToE());
                var index = m_buffer.IndexOf(0x0A);
                if (index == -1)
                {
                    return;
                }
                var tmp = m_buffer.Take(index).ToArray();
                Header = YUVHeader.Parse(Encoding.ASCII.GetString(tmp));
                m_current = new YUVFrameReader(Header);
                m_next = new YUVFrameReader(Header);
                PushBody(new ArraySegment<Byte>(m_buffer.Skip(index + 1).ToArray()));
                m_buffer.Clear();
            }
            else
            {
                PushBody(bytes);
            }
        }

        public int m_frameNumber;

        bool PushBody(ArraySegment<Byte> bytes)
        {
            bool hasNewFrame = false;

            var i = 0;
            while (i < bytes.Count)
            {
                i = m_next.Push(bytes, i);
                if (m_next.IsFill)
                {
                    YUVFrameReader tmp;
                    lock (m_currentLock)
                    {
                        tmp = m_current;
                        m_current = m_next;
                    }
                    m_next = tmp;
                    m_next.Clear(m_frameNumber++);
                    hasNewFrame = true;
                }
            }

            return hasNewFrame;
        }
    }
}
/*
readonly Byte[] frame_header = new[] { (byte)0x46, (byte)0x52, (byte)0x41, (byte)0x4D, (byte)0x45 };


bool IsHead(List<Byte> src)
{
    for (int i = 0; i < frame_header.Length; ++i)
    {
        if (src[i] != frame_header[i]) return false;
    }
    return true;
}
*/
