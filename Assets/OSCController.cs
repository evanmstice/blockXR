/* Copyright (c) 2020 ExT (V.Sigalkin) */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace extOSC.Examples
{
	public class OSCController : MonoBehaviour
	{
		// public GameObject ProgrammingEnv;
		// public GameObject BEController;
		// public GameObject mainPanel;
		// public Text menuMessage;

		#region Private Vars

		private OSCTransmitter _transmitter;

		private OSCReceiver _receiver;

		private const string _oscAddress = "/*"; 

		private string noBlocksMessage = "No blocks detected!\nPlease make sure the blocks are properly aligned.";

		// states
		private int state;
		private const int RECEIVE = 0;
		private const int STARTUP = 3;
		private const int QUIT = 4;

		// width and height of viewport in unity
		//public const float UNITY_WIDTH = (float)949.0001;
		public float UNITY_WIDTH = Screen.width;
		public float UNITY_HEIGHT = Screen.height;
		//public const float UNITY_HEIGHT = (float)533.6231;

		#endregion

		#region Unity Methods

		protected virtual void Start()
		{
			Debug.Log(Screen.width);
			Debug.Log(Screen.height);
			// Creating a transmitter.
			_transmitter = gameObject.AddComponent<OSCTransmitter>();

			// Set remote host address.
			_transmitter.RemoteHost = "127.0.0.1";

			// Set remote port;
			_transmitter.RemotePort = 31415;

			// transmitter will always be inactive unless there is a message to send
			//_transmitter.enabled = false;

			// Creating a receiver.
			_receiver = gameObject.AddComponent<OSCReceiver>();

			// Set local port.
			_receiver.LocalPort = 7001;

			// Bind "MessageReceived" method to special address.
			_receiver.Bind(_oscAddress, MessageReceived);

			state = STARTUP;
		}

		protected virtual void Update()
		{
			// make state machine to switch between states such as sending, receiving,
			// and performing ui updates
			_transmitter.enabled = false;
			switch(state)
			{
				case STARTUP:
					//Startup();
					break;
				case RECEIVE:
					break;
				case QUIT:
					Debug.Break();
					break;
			}
		}

		#endregion

		#region Protected Methods

		public void UpdateState(string newState)
		{
			switch(newState)
			{
				case "STARTUP":
					state = STARTUP;
					break;
				case "RECEIVE":
					state = RECEIVE;
					break;
				case "QUIT":
					state = QUIT;
					break;
			}
		}

		public void MessageSent(string adr, string[] msgs, int newState)
		{
			if (_transmitter == null) return;
			_transmitter.enabled = true;
		
			// Create message
			var message = new OSCMessage(adr);
			
			foreach (string msg in msgs)
			{
				message.AddValue(OSCValue.String(msg));
			}

			// Send message
			_transmitter.Send(message);

			Debug.Log(message);

			state = newState;
		}

		protected void MessageReceived(OSCMessage message)
		{
			// use address to find out what kind of message it is and
			// call the proper handler method
			if (message.Address == "/program") {
				Debug.Log("PROGRAM RECEIVED");
				if (state == RECEIVE)
					HandleProgram(message);
				else
					Debug.Log("NOT IN RECEIVE STATE");
			}
			else if (message.Address == "/err")
			{
				Debug.Log(message);
			}
			else
			{
				Debug.Log("Address not recognized.");
				Debug.Log(message.Address);
			}
		}

		// protected void Startup()
		// {
		// 	//_transmitter.enabled = true;
		// 	string[] msgs = {"start"};
		// 	MessageSent("/req", msgs, RECEIVE);
		// }

		protected void HandleProgram(OSCMessage message)
		{
			Debug.Log("PROGRAM RECEIVED");
			// SaveLoadCode fileSaver = ProgrammingEnv.GetComponent<SaveLoadCode>();

			List<string> blockList = new List<string>();

			// loop through osc message to create list of block names
			foreach (var value in message.Values) {
				blockList.Add(value.StringValue.Trim());
				Debug.Log("Block detected: " + value.StringValue.Trim());
			}

			if (blockList.Count == 0)
			{
				Debug.Log("No blocks detected!");
				// menuMessage.text = noBlocksMessage;
        		// mainPanel.SetActive(true);
				// BEController.GetComponent<BEController>().loadingAnimation.enabled = false;
			}
			else
			{
				Debug.Log("Received " + blockList.Count + " blocks: ");
				// TODO: Send the blockList to movement script
				
			}

			state = STARTUP;
		}

		#endregion
	}
}