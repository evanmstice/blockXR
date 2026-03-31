/* Copyright (c) 2020 ExT (V.Sigalkin) */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace extOSC.Examples
{
	public class OSCController : MonoBehaviour
	{
		public PlayerMovement playerMovement;

		private OSCTransmitter _transmitter;
		private OSCReceiver _receiver;
		private const string _oscAddress = "/program"; 

		// states
		private int state;
		private const int RECEIVE = 0;
		private const int STARTUP = 3;
		private const int QUIT = 4;

		// tracks whether program is running, ensures that only one program runs when the user presses run
		private bool isRunning = false;

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

	[ContextMenu("Test: Level 2")]
		public void TestLevel2()
		{
			List<string> testBlocks = new List<string>(){

				"Left",
				"Forward",
				"Right",
				"Forward",
				"Forward"

			};
			StartCoroutine(ExecuteBlocks(testBlocks));
		}	

		[ContextMenu("Test: Level 3")]
		public void TestLevel3()
		{
			List<string> testBlocks = new List<string>(){

				"Repeat until",
				"Forward",
				"End loop"

			};
			StartCoroutine(ExecuteBlocks(testBlocks));
		}	
		protected virtual void Awake()
		{
			DontDestroyOnLoad(gameObject);
			// Creating a transmitter.
			_transmitter = gameObject.AddComponent<OSCTransmitter>();
			_transmitter.RemoteHost = "127.0.0.1";
			_transmitter.RemotePort = 31415;

			// Setup Receiver (Python to Unity)
			_receiver = gameObject.AddComponent<OSCReceiver>();
			_receiver.LocalPort = 7001;
			_receiver.Bind(_oscAddress, MessageReceived);

			// Initial State
			state = RECEIVE;

			// Trigger computer vision capture
			SendCaptureRequest();
		}

		protected virtual void Update() {
			if (_transmitter != null)
        		_transmitter.enabled = false;
		}

		public void SendCaptureRequest() {
			Debug.Log("Unity: Requesting Block Capture from Python...");
			string[] msgs = { "capture" };
			// Send message to Python port 31415
			MessageSent("/req", msgs, RECEIVE);
		}

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

		protected void MessageReceived(OSCMessage message) {
			// use address to find out what kind of message it is and
			// call the proper handler method
			if (message.Address == "/program") {
				Debug.Log("PROGRAM RECEIVED");
				if (state == RECEIVE)
					HandleProgram(message);
				else
					Debug.Log("NOT IN RECEIVE STATE");
			}
			else if (message.Address == "/err") {
				Debug.Log(message);
			}
			else {
				Debug.Log("Address not recognized.");
				Debug.Log(message.Address);
			}
		}

		protected void Startup()
		{
			string[] msgs = {"start"};
			MessageSent("/req", msgs, RECEIVE);
		}

		protected void HandleProgram(OSCMessage message)
		{
			List<string> blockList = new List<string>();

			// loop through osc message to create list of block names
			foreach (var value in message.Values) {
				blockList.Add(value.StringValue.Trim());
				Debug.Log("Block detected: " + value.StringValue.Trim());
			}

			if (blockList.Count == 0) {
				Debug.Log("No blocks detected!");
			} else {
				StartCoroutine(ExecuteBlocks(blockList));	
			}
			state = STARTUP;
		}

		// coroutine to execute the list of blocks with delays between each block
		private IEnumerator ExecuteBlocks(List<string> blockList, bool loop = false)
		{
			// stop overlapping coroutines, and allows for loops to run 
			if(loop == false){
				if(isRunning == true){
					yield break;
				}

				// creates a lock so that only one coroutine can run at a time
				isRunning = true;
				if (playerMovement == null)
            {
                Debug.LogWarning("OSCController: playerMovement is not assigned yet");
                isRunning = false;
                yield break;
            }
			// reset when there is new blocks
				playerMovement.offPath = false;

				bool whileLoop = blockList.Contains("Repeat until");
				bool endLoop = blockList.Contains("End loop");

				if (whileLoop && !endLoop){
					Debug.Log("While loop does not have an end loop attached");
					GameManager.Instance.ShowTryAgainPanel("OOPS!\nYOU ARE MISSING\nAN END LOOP BLOCK!");
					isRunning = false;
					yield break;
				}
			
			}

			int i = 0;
			while (i < blockList.Count)
			{
				string block = blockList[i];

				if (block == "Forward"){

					// waits for this movement to finish before moving on to next block
					yield return StartCoroutine(playerMovement.MoveForward());
					if (playerMovement.offPath) {
						GameManager.Instance.Result();
						isRunning = false;
						yield break;
					}
					yield return new WaitForSeconds(0.5f);
					i++;
				}
				else if (block == "Right")
				{
					playerMovement.TurnRight();
					yield return new WaitForSeconds(0.2f);
					i++;
				}
				else if (block == "Left")
				{
					playerMovement.TurnLeft();
					yield return new WaitForSeconds(0.2f);
					i++;
				} else if(block == "Repeat until"){
					// find the end loop
					int endLoopIndex = blockList.IndexOf("End loop", i);

					// blocks inside the loop
					List<string> loopBlockList = blockList.GetRange(i + 1, endLoopIndex - i - 1);

					// get loop count for the current level, default is one for levels that do not require a loop

					LevelData levelData = FindAnyObjectByType<LevelData>();
		
					int loopCount;
					if(levelData != null){
						loopCount = levelData.loopCount;
					} else {
						loopCount = 1;
					}

					for(int j = 0; j< loopCount; j++){
						yield return StartCoroutine(ExecuteBlocks(loopBlockList, true));
					}

					// skip over the end loop
					i = endLoopIndex + 1;
				} else{
					i++;
				}
			}
			
			// Did the player reach the goal
			if(loop == false){
				GameManager.Instance.Result();
				isRunning = false;
			}
			
		}
		
	}
}