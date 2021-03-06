﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    //Animation stuff
    Animator PlayerAni;
    bool caught;
    bool disableMovement;

    //audio stuff
    AudioSource audioSource;
    public AudioClip numberPickupClip;
    public AudioClip shortDialClip;
    public AudioClip failCall;
    public AudioClip boostClip;
    public AudioSource BGM;
    

    //pickup effect
    public GameObject collectEffect;

    Rigidbody rb;
    public float movementSpeed = 5.0f;

    // PUBLIC
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float jumpForce = 10.0f;
    public static Player CurrentPlayer;
    public Cops copScript;
    public int[] numsCollected;
    public GameObject BoyzPrefab;
    [SerializeField] public string[] numsCollectedLimited;
    public UIScript script_UI;

    public float boostSpeed = 12.0f;
    public float boostJumpForce = 10.0f;
    public float boostDuration = 3.0f;
    private float boostTimer = 0.0f;
    bool boostingIt = false;
    private float baseMovementSpeed;
    private float baseJumpForce;

    private string[] phoneBook;

    // PRIVATE
    // For numbers that have been collected


    // Start is called before the first frame update
    void Start()
    {
        CurrentPlayer = this;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        numsCollected = new int[10];
        PlayerAni = this.GetComponent<Animator>();
        caught = false;
        disableMovement = false;

        // Boost
        baseJumpForce = jumpForce;
        baseMovementSpeed = movementSpeed;

        // Initialise phone book
        phoneBook = new string[3];
        phoneBook[0] = "123";
        phoneBook[1] = "238";
        phoneBook[2] = "555";

        // Initialise phone buffer
        numsCollectedLimited = new string[10];
        numsCollectedLimited[0] = "1";
        numsCollectedLimited[1] = "1";
        numsCollectedLimited[2] = "1";
        numsCollectedLimited[3] = "-";
        numsCollectedLimited[4] = "-";
        numsCollectedLimited[5] = "-";
        numsCollectedLimited[6] = "-";
        numsCollectedLimited[7] = "-";
        numsCollectedLimited[8] = "-";
        numsCollectedLimited[9] = "-";

        Debug.Log(numsCollectedLimited.Length);
        audioSource = GetComponent<AudioSource>();

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey("escape"))
        { 
            Application.Quit();
        }

        if (!caught && !disableMovement)
        {
            // PAGER CONTROLS
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                script_UI.moveSelectLeft();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                script_UI.moveSelectRight();
            }

            if (Input.GetKey(KeyCode.W))
            {
                Run();
                //movement = movement + transform.forward; // Local Coord
                movement = movement + Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                Run();
                //movement = movement - transform.forward; // Local Coord
                movement = movement - Vector3.forward;
            }
            if (Input.GetKey(KeyCode.A))
            {
                Run();
                //movement = movement + transform.TransformDirection(Vector3.left); // Local coord
                movement = movement + Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                Run();
                //movement = movement + transform.TransformDirection(Vector3.right); // Local coord
                movement = movement + Vector3.right;
            }
            movement = Vector3.Normalize(movement);
            movement = movement * movementSpeed;
            rb.MovePosition(transform.position + movement * Time.fixedDeltaTime);

            // Rotate model based on direction of movement
            if (movement != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(movement);
            }

            Debug.DrawRay(new Vector3(transform.position.x + 0.2f, transform.position.y + 1.0f, transform.position.z), Vector3.down * 1.05f, Color.green);
            Debug.DrawRay(new Vector3(transform.position.x - 0.2f, transform.position.y + 1.0f, transform.position.z), Vector3.down * 1.05f, Color.green);
            if (Input.GetKeyDown("space"))
            {
                //bool canJump = Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 1.0f, transform.position.z), Vector3.down, 1.1f);

                bool leftFootHit = Physics.Raycast(new Vector3(transform.position.x - 0.2f, transform.position.y + 1.0f, transform.position.z), Vector3.down, 1.05f);
                bool rightFootHit = Physics.Raycast(new Vector3(transform.position.x + 0.2f, transform.position.y + 1.0f, transform.position.z), Vector3.down, 1.05f);

                if (leftFootHit || rightFootHit)
                {
                    //Debug.Log(jumpForce);
                    //GetComponent<Rigidbody>().AddForce(new Vector3(0.0f, jumpForce, 0.0f));
                    //rb.velocity = (new Vector3(0.0f, jumpForce, 0.0f));
                    Jump();
                    rb.velocity = (new Vector3(rb.velocity.x, jumpForce, rb.velocity.z));
                }
            }

            // BOOSTING
            if (boostingIt)
            {
                boostTimer += Time.deltaTime;
                if (boostTimer >= boostDuration)
                {
                    this.gameObject.layer = 12;
                    boostingIt = false;
                    boostTimer = 0.0f;
                    jumpForce = baseJumpForce;
                    movementSpeed = baseMovementSpeed;
                    audioSource.Stop();
                    BGM.UnPause();
                }
            }


            if (rb.velocity.y < 0)
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            }
            else if (rb.velocity.y > 0 && !Input.GetKeyDown("space"))
            {
                rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
            }


            // CALLING
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.UpArrow)) // to call the bois
            {
                int selected = script_UI.getCurrentSelection();
                Debug.Log("trying to call phonebook " + selected);
                if (CheckNumber(selected)&& selected == 0)
                {
                    Debug.Log("Called selection " + selected);
                    Instantiate(BoyzPrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
                    DialANumber(phoneBook[selected]);
                    audioSource.PlayOneShot(shortDialClip, 0.3f);
                }
                else if(CheckNumber(selected) && selected == 1)
                {
                    Debug.Log("Called selection " + selected);
                    DialANumber(phoneBook[selected]);
                    boostingIt = true;
                    this.gameObject.layer = 13;
                    movementSpeed = boostSpeed;
                    jumpForce = boostJumpForce;
                    audioSource.PlayOneShot(shortDialClip, 0.3f);
                    audioSource.PlayOneShot(boostClip, 0.3f);
                    BGM.Pause();

                }
                else if (CheckNumber(selected) && selected == 2)
                {
                    Debug.Log("Called selection " + selected);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                    copScript.setChasing(false);
                    disableMovement = true;
                    audioSource.PlayOneShot(shortDialClip, 0.3f);

                    DialANumber(phoneBook[selected]);
                }
                else
                {
                    audioSource.PlayOneShot(failCall, 0.3f);

                    Debug.Log("Failed to call");
                }
            }
        }

        //restart scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (transform.position.y < -30)
        {
            Debug.Log("Fell off map");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if ((!(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKeyDown("space")) && !(caught)))
       {
           Idle();
       }
       else if (caught)
       {
            Fall();
       }
    }

    private void FixedUpdate()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Cops") // get caught by cops
        {
            Debug.Log("You got caught");
            script_UI.LoseText.transform.gameObject.SetActive(true);
            caught = true;
            copScript.setChasing(false);
        }

        if (other.tag == "Win")
        {
            Debug.Log("You ESCAPED!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            copScript.setChasing(false);
            disableMovement = true;
        }

        if (other.tag == "Number")
        {
           
            Destroy(other.gameObject);
            audioSource.PlayOneShot(numberPickupClip, 0.3f);
            Instantiate(collectEffect, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
            int numFound = other.gameObject.GetComponent<NumberScript>().thisNumber;
            Debug.Log("Found a " + numFound);
            //numsCollected[numFound]++;
            collectedANumber(numFound);

        }

    }

    private void Run()
    {
        PlayerAni.SetBool("Run", true);
        PlayerAni.SetBool("Idle", false);
        PlayerAni.SetBool("Jump", false);
        PlayerAni.SetBool("Fall", false);
    }

    private void Idle()
    {
        PlayerAni.SetBool("Idle", true);
        PlayerAni.SetBool("Run", false);
        PlayerAni.SetBool("Jump", false);
        PlayerAni.SetBool("Fall", false);
    }

    private void Jump()
    {
        PlayerAni.SetBool("Jump", true);
        PlayerAni.CrossFade("Jump", 0.1f);
        PlayerAni.SetBool("Idle", false);
        PlayerAni.SetBool("Run", false);
        PlayerAni.SetBool("Fall", false);
    }

    private void Fall()
    {
        PlayerAni.SetBool("Fall", true);
        PlayerAni.SetBool("Run", false);
        PlayerAni.SetBool("Idle", false);
        PlayerAni.SetBool("Jump", false);
    }

    bool DialANumber(string s)
    {
        string[] tempString = new string[10];
        numsCollectedLimited.CopyTo(tempString,0);

        foreach (char c in s)
        {
            bool foundTheNumber = false;
            for (int i = 0; i < numsCollectedLimited.Length; i++)
            {
                if (tempString[i] == c.ToString())
                {
                    foundTheNumber = true;
                    //Debug.Log("Got NUMBER!");
                    tempString[i] = "-";
                    break;
                }
            }
            if (!foundTheNumber)
            {
                return false;
            }
            else continue;
        }

        numsCollectedLimited = tempString;
        return true;
    }

    public bool CheckNumber(int phoneBookIndex)
    {
        string[] tempString = new string[10];
        numsCollectedLimited.CopyTo(tempString, 0);

        foreach (char c in phoneBook[phoneBookIndex])
        {
            bool foundTheNumber = false;
            for (int i = 0; i < numsCollectedLimited.Length; i++)
            {
                if (tempString[i] == c.ToString())
                {
                    foundTheNumber = true;
                    //Debug.Log("Got NUMBER!");
                    tempString[i] = "-";
                    break;
                }
            }
            if (!foundTheNumber)
            {
                return false;
            }
            else continue;
        }
        return true;
    }

    void collectedANumber(int num)
    {
        if (num >= 0 && num <=9)
        { 
            // move all the numbers along
            for (int i = numsCollectedLimited.Length - 1; i > 0; i--)
            {
                numsCollectedLimited[i] = numsCollectedLimited[i - 1];
            }

            numsCollectedLimited[0] = num.ToString();
        }
        else
        {
            Debug.Log("Number out of range");
        }
    }
}
