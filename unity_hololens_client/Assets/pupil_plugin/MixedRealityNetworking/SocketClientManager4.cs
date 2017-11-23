#if NETFX_CORE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MixedRealityNetworking
{
	public class SocketClientManager
	{
		#region Fields

		/// <summary>
		/// The ip address the client connects to
		/// </summary>
		private string host;

		/// <summary>
		/// The port the client connects to
		/// </summary>
		private int port;

		/// <summary>
		/// Contains the callbackmethods for seperate message ID's
		/// </summary>
		private Dictionary<byte, Action<NetworkMessage>> callbackMethods = new Dictionary<byte, Action<NetworkMessage>>();

		/// <summary>
		/// The socket
		/// </summary>
		private DatagramSocket udpSocket;

		/// <summary>
		/// Boolean that indicates if we should print debug information
		/// </summary>
		private bool verboseMode = false;

		#endregion

		#region Properties

		/// <summary>
		/// The host to which the socket needs to connect
		/// </summary>
		public string Host
		{
			set { host = value; }
		}

		/// <summary>
		/// The port to which the socket needs to connect
		/// </summary>
		public int Port
		{
			set { port = value; }
		}

		/// <summary>
		/// Boolean indicating if debug info should be printed
		/// </summary>
		public bool VerboseMode
		{
			get { return verboseMode; }
			set { verboseMode = value; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connects to the socket and starts listening
		/// </summary>
		public void Connect()
		{
			udpSocket = new DatagramSocket();
			udpSocket.MessageReceived += MessageReceived;

			// Calling the binding async
			Bind();
		}

		/// <summary>
		/// Binds to an endpoint
		/// This is not done in the Connect() method, because it's async
		/// and Unity may have problems with it...
		/// </summary>
		private async void Bind()
		{
			await udpSocket.BindServiceNameAsync(port.ToString());
		}

		/// <summary>
		/// Method to subscribe to a message
		/// </summary>
		/// <exception cref="InvalidOperationException">Gets thrown when there is already a subscription for the message ID</exception>
		/// <param name="messageId">The id of the message you want to subscribe to</param>
		/// <param name="callbackMethod">The method that should be called</param>
		public void Subscribe(byte messageId, Action<NetworkMessage> callbackMethod)
		{
			// Check if not already subscribed
			if (callbackMethods.ContainsKey(messageId))
				throw new InvalidOperationException("There is already a subscription to this message ID");

			callbackMethods.Add(messageId, callbackMethod);
		}

		/// <summary>
		/// Sends a message
		/// </summary>
		/// <param name="nm">The <c cref="NetworkMessage">network message</c> that needs to be send</param>
		public void SendMessage(NetworkMessage nm)
		{
			// Write the data into a byte array
			byte[] byteArray = new byte[nm.Content.Length + 1];

			byteArray[0] = nm.MessageId;

			// Write the content into the array
			var i = 1;

			foreach (byte messageData in nm.Content)
			{
				byteArray[i] = messageData;
				++i;
			}

			PrintDebug("Sending message");

			SendMessageToSocket(byteArray);
		}

		/// <summary>
		/// Internal method for sending the message to the socket
		/// Because this method is async, it has to be in a seperated method
		/// </summary>
		/// <param name="byteArray">The network message converted to a byte array</param>
		private async void SendMessageToSocket(byte[] byteArray)
		{
			IOutputStream streamOut = (await udpSocket.GetOutputStreamAsync(new Windows.Networking.HostName(host), port.ToString()));

			// Write the data
			using (DataWriter writer = new DataWriter(streamOut))
			{
				writer.WriteBytes(byteArray);
				await writer.StoreAsync();
			}
		}

		/// <summary>
		/// Event handler that gets called when a message is received on the socket
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="data"></param>
		private void MessageReceived(Windows.Networking.Sockets.DatagramSocket sender, Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
		{
			Stream streamIn = args.GetDataStream().AsStreamForRead();

			byte[] byteArray;

			// Use the memory stream to convert the stream to a byte array
			using (var memoryStream = new MemoryStream())
			{
				streamIn.CopyTo(memoryStream);
				byteArray = memoryStream.ToArray();
			}

			// Data received, create a new NetworkMessage
			byte messageId = byteArray[0];

			// Remove the message ID from the data
			byte[] message = new byte[byteArray.Length - 1];

			for (int i = 1; i < byteArray.Length; ++i)
			{
				message[i - 1] = byteArray[i];
			}

			// Build network message
			NetworkMessage nm = new NetworkMessage(messageId, message);

			// Call the correct callback method
			if (callbackMethods.ContainsKey(messageId))
			{
				// Catch any exceptions and rethrow them,
				// so a user gets a good exception instead of a 
				// object disposed exception
				try
				{
					callbackMethods[messageId](nm);
				}
				catch (Exception e)
				{
					throw e;
				}
			}
			else
			{
				PrintDebug("No known callback for message ID " + messageId.ToString());
			}
		}

		/// <summary>
		/// Stops listening to the socket and closes the thread
		/// </summary>
		public void StopListening()
		{
			// Close the UDP client
			if (udpSocket != null)
				udpSocket.Dispose();
		}

		/// <summary>
		/// Method that prints debug information when verbose mode is enabled
		/// </summary>
		/// <param name="message">The message that needs to be printed</param>
		private void PrintDebug(string message)
		{
			if (verboseMode)
				Debug.WriteLine(message);
		}

		#endregion
	}
}

#endif