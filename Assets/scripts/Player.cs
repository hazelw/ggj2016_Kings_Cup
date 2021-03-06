﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;	//Allows us to use UI.

using System.Collections.Generic;

//Player inherits from MovingObject, our base class for objects that can move, Enemy also inherits from this.
public class Player : MovingObject {
	public float restartLevelDelay = 1f;		//Delay time in seconds to restart level.
	public AudioClip[] moveSounds;				//1 of 2 Audio clips to play when player moves.
	public AudioClip[] pourSounds;
	public AudioClip gameOverSound;				//Audio clip to play when player dies.
	
	private Animator animator;					//Used to store a reference to the Player's animator component.
	private int steps;							//Used to store player food points total during level.
	private int stepsInShadow;
	private int maxStepsInShadow = 4;
	private Vector2 touchOrigin = -Vector2.one;	//Used to store location of screen touch origin for mobile controls.
	private GameObject[] torchLights;
	private GameObject[] mainLights;
	private float torchShadowDistance = 2f;
	private float mainShadowDistance = 4f;
	private int wineCount = 1;
	
	//Start overrides the Start function of MovingObject
	protected override void Start () {
		//Get a component reference to the Player's animator component
		animator = GetComponent<Animator>();

		// set starting step count
		steps = 0;
		stepsInShadow = 0;
		
		//Set the text to reflect the current player step total.
		updateStepsText();

		torchLights = GameObject.FindGameObjectsWithTag ("TorchLight");
		mainLights = GameObject.FindGameObjectsWithTag ("AmbientLight");


		
		//Call the Start function of the MovingObject base class.
		base.Start ();

	}

	private void updateStepsText() {
		GameManager.instance.updateStepsText(steps);
	}

	private void updateShadowStepsText() {
		GameManager.instance.updateShadowStepsText(stepsInShadow);
	}
	
	private void Update() {
		//If it's not the player's turn, exit the function.
		if(!GameManager.instance.playersTurn) return;
		
		int horizontal = 0;  	//Used to store the horizontal move direction.
		int vertical = 0;		//Used to store the vertical move direction.
		
		//Check if we are running either in the Unity editor or in a standalone build.
		#if UNITY_STANDALONE || UNITY_WEBPLAYER
		
		//Get input from the input manager, round it to an integer and store in horizontal to set x axis move direction
		horizontal = (int) (Input.GetAxisRaw ("Horizontal"));
		
		//Get input from the input manager, round it to an integer and store in vertical to set y axis move direction
		vertical = (int) (Input.GetAxisRaw ("Vertical"));
		
		//Check if moving horizontally, if so set vertical to zero.
		if(horizontal != 0) {
			vertical = 0;
		}

		// Kick off the walking animation
		if (horizontal > 0){
			animator.SetTrigger("playerWalkRight");
		} else if (horizontal < 0){
			animator.SetTrigger("playerWalkLeft");
		} else if (vertical > 0){
			animator.SetTrigger("playerWalkBack");
		} else if (vertical < 0){
			animator.SetTrigger("playerWalkForward");
		}

		//Check if we are running on iOS, Android, Windows Phone 8 or Unity iPhone
		#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
		
		//Check if Input has registered more than zero touches
		if (Input.touchCount > 0)
		{
			//Store the first touch detected.
			Touch myTouch = Input.touches[0];
			
			//Check if the phase of that touch equals Began
			if (myTouch.phase == TouchPhase.Began)
			{
				//If so, set touchOrigin to the position of that touch
				touchOrigin = myTouch.position;
			}
			
			//If the touch phase is not Began, and instead is equal to Ended and the x of touchOrigin is greater or equal to zero:
			else if (myTouch.phase == TouchPhase.Ended && touchOrigin.x >= 0)
			{
				//Set touchEnd to equal the position of this touch
				Vector2 touchEnd = myTouch.position;
				
				//Calculate the difference between the beginning and end of the touch on the x axis.
				float x = touchEnd.x - touchOrigin.x;
				
				//Calculate the difference between the beginning and end of the touch on the y axis.
				float y = touchEnd.y - touchOrigin.y;
				
				//Set touchOrigin.x to -1 so that our else if statement will evaluate false and not repeat immediately.
				touchOrigin.x = -1;
				
				//Check if the difference along the x axis is greater than the difference along the y axis.
				if (Mathf.Abs(x) > Mathf.Abs(y))
					//If x is greater than zero, set horizontal to 1, otherwise set it to -1
					horizontal = x > 0 ? 1 : -1;
				else
					//If y is greater than zero, set horizontal to 1, otherwise set it to -1
					vertical = y > 0 ? 1 : -1;
			}
		}
		
		#endif //End of mobile platform dependendent compilation section started above with #elif
		//Check if we have a non-zero value for horizontal or vertical
		if(horizontal != 0 || vertical != 0) {
			//Call AttemptMove passing in the generic parameter Wall, since that is what Player may interact with if they encounter one (by attacking it)
			//Pass in horizontal and vertical as parameters to specify the direction to move Player in.
			AttemptMove(horizontal, vertical);
		}
	}
	
	//AttemptMove overrides the AttemptMove function in the base class MovingObject
	//AttemptMove takes a generic parameter T which for Player will be of the type Wall, it also takes integers for x and y direction to move in.
	protected void AttemptMove(int xDir, int yDir) {
		//If Move returns true, meaning Player was able to move into an empty space.
		if (Move (xDir, yDir)) {
			steps++;
			updateStepsText();
			if (checkInLight ()) {
				stepsInShadow = 0;
			} else {
				stepsInShadow++;
			}
			updateShadowStepsText ();
			//Call RandomizeSfx of SoundManager to play the move sound, passing in two audio clips to choose from.
			SoundManager.instance.RandomizeSfx (moveSounds);
		}
		
		//Since the player has moved and lost food points, check if the game has ended.
		GameOver ();
		
		//Set the playersTurn boolean of GameManager to false now that players turn is over.
		GameManager.instance.setPlayersTurn(false);
	}

	private bool checkInLight() {
		if (objectWithinDistance (mainShadowDistance, mainLights)) {
			return true;
		} else {
			return objectWithinDistance (torchShadowDistance, torchLights);
		}
	}

	private bool objectWithinDistance(float targetDistance, GameObject[] objects) {
		float closest = -1f;
		foreach (GameObject obj in objects) {
			float distance = Vector3.Distance (transform.position, obj.transform.position);
			if (closest < 0f || distance < closest) {
				closest = distance;
			}
		}
		return closest >= 0 && closest < targetDistance;
	}
		
	
	//OnTriggerEnter2D is sent when another object enters a trigger collider attached to this object (2D physics only).
	private void OnTriggerEnter2D (Collider2D other) {
		Debug.Log ("OnTriggerEnter2D");
		//Check if the tag of the trigger collided with is Exit.
		if (other.tag == "Exit") {
			SoundManager.instance.RandomizeSfx (pourSounds);
			//Invoke the Restart function to start the next level with a delay of restartLevelDelay (default 1 second).
			Invoke ("Completed", restartLevelDelay);
			Debug.Log ("Completed!");
		} else if (other.tag == "Hostile") {
			this.collisionWithNPC ();
		} else if (other.tag == "King") {
			this.AttemptRegiside ();
		}
	}

	private void collisionWithNPC(){
		wineCount--;
		this.GameOver ();
		Debug.Log ("WINE TIME");
	}
	
	//Restart reloads the scene when called.
	private void Completed () {
		GameManager.instance.ShowNextLevelMessage ();
	}

	private void AttemptRegiside(){
		GameOver (GameManager.GameOverReason.ATTEMPTED_REGICIDE);
	}

	private void GameOver() {
		if (stepsInShadow > maxStepsInShadow) {
			GameOver (GameManager.GameOverReason.SHADOWS);
		} else if (steps > GameManager.instance.MaxTurns) {
			GameOver (GameManager.GameOverReason.TIME);
		} else if (wineCount == 0) {
			GameOver (GameManager.GameOverReason.WINE);
		}
	}
	
	//CheckIfGameOver checks if the player is out of food points and if so, ends the game.
	private void GameOver (GameManager.GameOverReason reason) {
		GameManager.instance.GameOver (reason);
	}
}
