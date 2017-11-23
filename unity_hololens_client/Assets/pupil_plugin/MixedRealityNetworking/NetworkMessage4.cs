#if NETFX_CORE

namespace MixedRealityNetworking
{
	public class NetworkMessage
	{
		#region Fields

		/// <summary>
		/// Contains the ID of the message
		/// </summary>
		private byte messageId;

		/// <summary>
		/// The content of the message
		/// </summary>
		private byte[] content;

		/// <summary>
		/// The buffer to write data to
		/// </summary>
		private byte[] buffer = new byte[1000000];

		/// <summary>
		/// The index of the buffer
		/// </summary>
		private int bufferIndex = 0;

		/// <summary>
		/// The index of the reading
		/// </summary>
		private int readIndex = 0;

		#endregion

		#region Properties

		/// <summary>
		/// Contains the ID of the message
		/// </summary>
		public byte MessageId
		{
			get { return this.messageId; }
		}

		/// <summary>
		/// The content of the message
		/// </summary>
		public byte[] Content
		{
			get
			{
				// Check if the content is already filled, if not,
				// read it from the buffer
				if (this.content == null)
				{
					this.content = new byte[this.bufferIndex + 1];

					for (int i = 0; i < this.bufferIndex + 1; ++i)
					{
						this.content[i] = this.buffer[i];
					}
				}

				return this.content;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// The constructor
		/// </summary>
		/// <param name="messageId">The ID of the message</param>
		public NetworkMessage(byte messageId)
		{
			this.messageId = messageId;
		}

		/// <summary>
		/// The constructor
		/// </summary>
		/// <param name="messageId">The ID of the message</param>
		/// <param name="content">The content of the message</param>
		public NetworkMessage(byte messageId, byte[] content)
		{
			this.messageId = messageId;
			this.content = content;
			this.buffer = content;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Writes a byte to the buffer
		/// </summary>
		/// <param name="b">The byte that needs to be writed</param>
		public void Write(byte b)
		{
			this.buffer[this.bufferIndex] = b;
			++this.bufferIndex;
		}

		/// <summary>
		/// Writes a byte array to the buffer
		/// </summary>
		/// <param name="byteArr">The byte array that needs to be writed</param>
		public void Write(byte[] byteArr)
		{
			foreach (byte b in byteArr)
			{
				this.Write(b);
			}
		}

		/// <summary>
		/// Writes a float to the buffer
		/// </summary>
		/// <param name="f">The float that needs to be writed</param>
		public void Write(float f)
		{
			byte[] byteArr = System.BitConverter.GetBytes(f);

			this.Write(byteArr);
		}

		/// <summary>
		/// Writes an integer to the buffer
		/// </summary>
		/// <param name="i">The integer that needs to be writed</param>
		public void Write(int i)
		{
			byte[] byteArr = System.BitConverter.GetBytes(i);

			this.Write(byteArr);
		}

		/// <summary>
		/// Writes a long to the buffer
		/// </summary>
		/// <param name="i">The long that needs to be writed</param>
		public void Write(long l)
		{
			byte[] byteArr = System.BitConverter.GetBytes(l);

			this.Write(byteArr);
		}

		/// <summary>
		/// Reads a byte from the buffer
		/// </summary>
		/// <exception cref="System.IndexOutOfRangeException">Thrown when the index is outside of the Content bounds</exception>
		/// <returns>The byte</returns>
		public byte ReadByte()
		{
			// Check if index is not out of bounds
			if (this.readIndex >= this.Content.Length - 1)
				throw new System.IndexOutOfRangeException("Trying to read outside content bounds");

			byte b = this.Content[this.readIndex];
			++this.readIndex;

			return b;
		}

		/// <summary>
		/// Reads a float from the buffer
		/// </summary>
		/// <exception cref="System.IndexOutOfRangeException">Thrown when the index is outside of the Content bounds</exception>
		/// <returns>The float</returns>
		public float ReadFloat()
		{
			// Check if index is not out of bounds
			if ((this.readIndex + 3) >= this.Content.Length - 1)
				throw new System.IndexOutOfRangeException("Trying to read outside content bounds");

			byte[] floatArr = new byte[4];

			for (int i = 0; i < 4; i++)
			{
				floatArr[i] = this.ReadByte();
			}

			return System.BitConverter.ToSingle(floatArr, 0);
		}

		/// <summary>
		/// Reads an integer from the buffer
		/// </summary>
		/// <exception cref="System.IndexOutOfRangeException">Thrown when the index is outside of the Content bounds</exception>
		/// <returns>The integer</returns>
		public int ReadInt()
		{
			// Check if index is not out of bounds
			if ((this.readIndex + 3) >= this.Content.Length - 1)
				throw new System.IndexOutOfRangeException("Trying to read outside content bounds");

			byte[] floatArr = new byte[4];

			for (int i = 0; i < 4; i++)
			{
				floatArr[i] = this.ReadByte();
			}

			return System.BitConverter.ToInt32(floatArr, 0);
		}

		/// <summary>
		/// Reads a long from the buffer
		/// </summary>
		/// <exception cref="System.IndexOutOfRangeException">Thrown when the index is outside of the Content bounds</exception>
		/// <returns>The long</returns>
		public long ReadLong()
		{
			// Check if index is not out of bounds
			if ((this.readIndex + 7) >= this.Content.Length - 1)
				throw new System.IndexOutOfRangeException("Trying to read outside content bounds");

			byte[] floatArr = new byte[8];

			for (int i = 0; i < 8; i++)
			{
				floatArr[i] = this.ReadByte();
			}

			return System.BitConverter.ToInt64(floatArr, 0);
		}

		/// <summary>
		/// Gets the unread byte count
		/// </summary>
		/// <returns>Integer with the nummer of unread bytes</returns>
		public int UnreadByteCount()
		{
			return (this.Content.Length - this.readIndex - 1);
		}

		#endregion
	}
}

#endif