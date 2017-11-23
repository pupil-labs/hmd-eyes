#if !NETFX_CORE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
        /// The thread on which the socket runs
        /// </summary>
        private Thread socketThread;

        /// <summary>
        /// The UDP client
        /// </summary>
        public UdpClient udpClient;

        /// <summary>
        /// Making sure the thread aborts
        /// </summary>
        private bool abortThread = false;

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
        /// Connects to the <c cref="udpClient">socket</c> and starts listening
        /// </summary>
        public void Connect()
        {
            udpClient = new UdpClient(host, port);

            socketThread = new Thread(Listen);
            socketThread.Start();
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
		byte[] byteArray;
		public void SendMessage(NetworkMessage nm)
        {
            // Write the data into a byte array
            byteArray = new byte[nm.Content.Length + 1];

            byteArray[0] = nm.MessageId;

            // Write the content into the array
            var i = 1;

            foreach (byte messageData in nm.Content)
            {
                byteArray[i] = messageData;
                ++i;
            }

            PrintDebug("Sending message");

            // Send it
			udpClient.Send(byteArray, byteArray.Length);
        }

        /// <summary>
        /// Method that listens for incoming messages on the <c cref="udpClient">socket</c>
        /// </summary>
        private void Listen()
        {
            try
            {
				IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

                // Keep looping as long as the thread isn't aborted
                while (!abortThread)
                {
                    byte[] clientData = udpClient.Receive(ref endpoint);

                    // Data received, create a new NetworkMessage
                    byte messageId = clientData[0];

                    // Remove the message ID from the data
                    byte[] message = new byte[clientData.Length - 1];

                    for (int i = 1; i < clientData.Length; ++i)
                    {
                        message[i - 1] = clientData[i];
                    }

                    // Call the correct callback method
                    if (callbackMethods.ContainsKey(messageId))
                    {
                        // Catch any exceptions and rethrow them,
                        // so a user gets a good exception instead of a 
                        // object disposed exception
                        try
                        {
                            callbackMethods[messageId](new NetworkMessage(messageId, message));
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
            }
            catch (ThreadAbortException e)
            {
				PrintDebug (e.Message);
                // Thread aborted, do nothing
            }
            catch (SocketException e)
            {
                // Socket aborted, probably because we want to close it
                if (e.ErrorCode != 10004)
                    throw e;
            }
            catch (Exception e)
            {
                // Rethrow exception
                throw e;

                PrintDebug(e.Message);
            }
            finally
            {
                // Make sure we always close the connection
                udpClient.Close();
            }
        }

        /// <summary>
        /// Stops listening to the <c cref="udpClient">socket</c> and closes the <c cref="socketThread">thread</c>
        /// </summary>
        public void StopListening()
        {
            // Close the UDP client, since aborting the thread
            // doesn't work if it's waiting for packets
            if (udpClient != null)
                udpClient.Close();

            // Check if the thread is initialized
            if (socketThread is Thread && socketThread != null)
            {
                abortThread = true;

                //SocketClientManager.socketThread.Abort();
            }
        }

        /// <summary>
        /// Method that prints debug information when verbose mode is enabled
        /// </summary>
        /// <param name="message">The message that needs to be printed</param>
        private void PrintDebug(string message)
        {
            if (verboseMode)
                Trace.Write(message);
        }

        #endregion
    }
}
#endif
