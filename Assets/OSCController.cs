/* Copyright (c) 2020 ExT (V.Sigalkin) */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace extOSC.Examples
{
	public class OSCController : MonoBehaviour
	{
		public PlayerMovement playerMovement;
	

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

		// boolean tracks whether program is running, ensures that only one program runs when the user presses run
		private bool isRunning = false;

		
		public float UNITY_WIDTH = Screen.width;
		public float UNITY_HEIGHT = Screen.height;
		

		#endregion

		#region Unity Methods

		//TESTING CODE
		[ContextMenu("Test: Move Forward")]
		public void TestMoveForward()
		{
			List<string> testBlocks = new List<string>(){

				"Forward",
				"Forward",

			};
			StartCoroutine(ExecuteBlocks(testBlocks));
		}

	[ContextMenu("Test: Incorrect Path")]
		public void TestIncorrectPath()
		{
			List<string> testBlocks = new List<string>(){

				"Right",
				"Forward",
				"Forward"

			};
			StartCoroutine(ExecuteBlocks(testBlocks));
		}
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
			}
			else
			{
				Debug.Log("Received " + blockList.Count + " blocks: ");
				
				// the use of coroutine allows there to be a delay between each block
				StartCoroutine(ExecuteBlocks(blockList));
				
			}

			state = STARTUP;
		}

		// coroutine to execute the list of blocks with delays between each block
		private IEnumerator ExecuteBlocks(List<string> blockList)
		{
			// stops overlapping coroutines
			if(isRunning == true){
				yield break;
			}
			// creates a lock so that only one coroutine can run at a time
			isRunning = true;
			// reset when there is new blocks
			playerMovement.offPath = false;

			
			foreach (string block in blockList)
			{
				// if (block == "When clicked"){
				// 	run = true;
				// 	continue;
				// }
				if (block == "Forward"){

					// waits for this movement to finish before moving on to next block
					yield return StartCoroutine(playerMovement.MoveForward());
					yield return new WaitForSeconds(0.2f);
				}
				if (block == "Right")
				{
					playerMovement.TurnRight();
					yield return new WaitForSeconds(0.2f);
				}
				if (block == "Left")
				{
					playerMovement.TurnLeft();
					yield return new WaitForSeconds(0.2f);
				}
			}
			
			// Did the player reach the goal
			GameManager.Instance.Result();
			isRunning = false;
		}
		

		#endregion
	}
}