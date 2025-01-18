using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioSystem : MonoBehaviour
{
    public StudioEventEmitter TavernMusic;
    public StudioEventEmitter TavernFireplace;

    EventInstance DoorsSound;
    public EventReference doorsEvent;
    EventInstance FootstepsSound;
    public EventReference footstepsEvent;
    EventInstance JumpSound;
    public EventReference jumpEvent;
    EventInstance LandSound;
    public EventReference landEvent;
    public EventInstance SpellSound;
    public EventReference spellEvent;

    EventInstance InsideRoom;
    public EventReference insideRoomSnap;
    public EventInstance Outside;
    public EventReference outsideSnapshot;
    EventInstance HealthSnapshot;
    public EventReference healthSnapshot;

    public VCA GlobalVCA;
    public VCA MusicVCA;
    public VCA TavernVCA;
    public VCA OutsideVCA;


    private string footsteps_surface;
    public string doorsName;
    private string open;
    private string close;
    private string door_1;
    private string door_2;
    private string door_3;

    // FLAGS // 
    private bool doorsOpened_1;
    private bool doorsOpened_2;
    private bool doorsOpened_3;
    public bool isGrounded = true;
    private bool isJumping = false;
    private bool outsideSnapActivated = false;
    public bool roomsAmbientActivated;
    public bool isMusicPlaying = true;
    public bool muteActive;
    public bool musicMuteActive;
    public bool tavernMuteActive;
    public bool outsideMuteActive;
    private bool healthSnapActive;
    private PLAYBACK_STATE spell_pb;

    // DISTANCE TO GROUND //
    public float distToGround;

    // Start is called before the first frame update
    void Start()
    {
        // VCA SETUP //
        GlobalVCA = RuntimeManager.GetVCA("vca:/Mute"); // podanie klasie VCA ścieżki do wybranego eventu / snapshotu
        MusicVCA = RuntimeManager.GetVCA("vca:/Music");
        TavernVCA = RuntimeManager.GetVCA("vca:/Tavern_amb");
        OutsideVCA = RuntimeManager.GetVCA("vca:/Outside_amb");
        GlobalVCA.setVolume(DecibelToLinear(-100));

        // START SETUP //
        doorsOpened_1 = true;
        doorsOpened_2 = true;
        doorsOpened_3 = true;
        muteActive = true;
        isGrounded = true;
        isJumping = false;
        outsideSnapActivated = false;
        healthSnapActive = false;
        footsteps_surface = "Footsteps_surface";
        open = "Open";
        close = "Close";
        door_1 = "Tavern_door_room";
        door_2 = "Tavern_door_room (1)";
        door_3 = "Tavern_door_room (2)";

    // CALC DISTANCE TO GROUND // 
    distToGround = GetComponent<Collider>().bounds.extents.y;

        if (TavernFireplace == null)
            Debug.LogError("NULL");
    }

    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! FUNCTIONS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//

    // GROUNDING CHECKER //
    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.5f);
    }

    // DECIBEL TO LINEAR //  
    public float DecibelToLinear(float dB)
    {
        float linear = Mathf.Pow(10.0f, dB / 20f);
        return linear;
    }

    // ROOMS AMBIENT STATUS // 
    public void RoomsAmbientON()
    {
        roomsAmbientActivated = true;
    }
    public void RoomsAmbientOFF()
    {
        roomsAmbientActivated = false;
    }

    // FIREPLACE SOUND // 
    public void FireplaceOFF()
    {
        TavernFireplace.SetParameter("Fire", 0);
    }

    public void FireplaceON()
    {
        TavernFireplace.SetParameter("Fire", 1);
    }

    // DOORS SOUNDS //
    void DoorsManager(ref EventInstance doorSoundInstance, int doorsNumber, string doorState)
    {
        doorSoundInstance = RuntimeManager.CreateInstance(doorsEvent);
        doorSoundInstance.setParameterByNameWithLabel("Doors", doorState);
        doorSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject.transform));
        doorSoundInstance.start();
        doorSoundInstance.release();

        if (doorsNumber == 1)
            doorsOpened_1 = !doorsOpened_1;
        else if (doorsNumber == 2)
            doorsOpened_2 = !doorsOpened_2;
        else if (doorsNumber == 3)
            doorsOpened_3 = !doorsOpened_3;
    }
    public void PlayDoorSound()
    {
        if (doorsName == door_1)
        {
            if(doorsOpened_1)
                DoorsManager(ref DoorsSound, 1, close);
            else
                DoorsManager(ref DoorsSound, 1, open);
        }
        else if (doorsName == door_2)
        {
            if (doorsOpened_2)
                DoorsManager(ref DoorsSound, 2, close);
            else
                DoorsManager(ref DoorsSound, 2, open);
        }
        else if (doorsName == door_3)
        {
            if (doorsOpened_3)
                DoorsManager(ref DoorsSound, 3, close);
            else
                DoorsManager(ref DoorsSound, 3, open);
        }
    }    

    // FOOTSTEPS SOUNDS // 
    public void PlayFootsteps()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out var hit, distToGround + 0.5f)) return;
        var surfaceType = hit.collider.tag switch
        {
            "Wood" => "Wood",
            "Stone" or "Outside" or "Inside_stone" => "Stone",
            _ => "Stone"
        };

        FootstepsSound = RuntimeManager.CreateInstance(footstepsEvent);
        FootstepsSound.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject.transform));
        FootstepsSound.setParameterByNameWithLabel(footsteps_surface, surfaceType);
        FootstepsSound.start();
        FootstepsSound.release();
    }

    // JUMP SOUNDS //
    public void PlayJump()
    {
        if (!IsGrounded()) return;
        JumpSound = RuntimeManager.CreateInstance(jumpEvent); // "event:/Footsteps"

        if (IsGrounded())
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
            {
                string surface = hit.collider.tag switch
                {
                    "Stone" => "Stone",
                    "Wood" => "Wood",
                    "Inside_stone" => "Stone",
                    "Bed" => "Bed",
                    _ => "Stone"
                };

                JumpSound.setParameterByNameWithLabel(footsteps_surface, surface);
                JumpSound.start();
            }
        }

        JumpSound.release();
        isGrounded = false;
        isJumping = true;
    }

    // LAND SOUNDS //
    public void PlayLanding()
    {
        if (IsGrounded() && !isGrounded)
        {
            if (isJumping)
            {
                LandSound = RuntimeManager.CreateInstance(landEvent);
                RaycastHit hit;

                if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
                {
                    string surface = hit.collider.tag switch
                    {
                        "Stone" => "Stone",
                        "Wood" => "Wood",
                        "Inside_stone" => "Stone",
                        "Bed" => "Bed",
                        _ => "Stone"
                    };

                    LandSound.setParameterByNameWithLabel(footsteps_surface, surface);
                    LandSound.start();
                }

                LandSound.release();
                isGrounded = true;
                isJumping = false;
            }
        }
    }

    // SPELL CAST //
    public void SpellCast()
    {
        SpellSound = RuntimeManager.CreateInstance(spellEvent);
        SpellSound.setParameterByNameWithLabel("throwSpell", "false");
        SpellSound.start();
        SpellSound.release();
    }

    public void SpellRelease()
    {
        SpellSound.setParameterByNameWithLabel("throwSpell", "true");
        SpellSound.release();
    }

    // SNAPSHOTS //     
    public void OutsideSnap()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            if (hit.collider.CompareTag("Outside") && outsideSnapActivated == false)
            {
                // mechanika snapshotu
                Outside = RuntimeManager.CreateInstance(outsideSnapshot);
                Outside.start();
                outsideSnapActivated = !outsideSnapActivated;
                Debug.Log(outsideSnapActivated);
            }
            else if (hit.collider.CompareTag("Inside_stone") && outsideSnapActivated == true)
            {
                // mechanika snapshotu
                Outside.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                Outside.release();
                outsideSnapActivated = !outsideSnapActivated;
                Debug.Log(outsideSnapActivated);
            }
        }
    }

    private void RoomsSnapInstanceStart()
    {
        Debug.Log("doors closed");
        InsideRoom.start();
        InsideRoom.release();
    }

    private void RoomsSnapInstanceStop()
    {
        Debug.Log("doors opened");
        InsideRoom.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        InsideRoom.release();
    }

    public void RoomsSnap()
    {
        Debug.LogError(InsideRoom.isValid());

        if (!InsideRoom.isValid())
        {
            InsideRoom = RuntimeManager.CreateInstance(insideRoomSnap);
        }
        
        if (roomsAmbientActivated == true && doorsName == door_1 && doorsOpened_1 == false)
        {
            RoomsSnapInstanceStart();
        }
        else if (roomsAmbientActivated == true && doorsName == door_2 && doorsOpened_2 == false)
        {
            RoomsSnapInstanceStart();
        }
        else if (roomsAmbientActivated == true && doorsName == door_3 && doorsOpened_3 == false)
        {
            RoomsSnapInstanceStart();
        }
        else
        {
            RoomsSnapInstanceStop();
        }
    }

    public void HealthSnap()
    {
        if (!healthSnapActive)
        {
            HealthSnapshot = RuntimeManager.CreateInstance(healthSnapshot);
            HealthSnapshot.start();
            healthSnapActive = !healthSnapActive;
        }
        else if (healthSnapActive)
        {
            HealthSnapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            HealthSnapshot.release();
            healthSnapActive = !healthSnapActive;
        }
    }

    // VCA // 
    public void ToggleMute(KeyCode key, ref bool muteActive, VCA vca)
    {
        if (Input.GetKeyDown(key))
        {
            float volume = muteActive ? 0 : -100;
            vca.setVolume(DecibelToLinear(volume));
            muteActive = !muteActive;
        }
    }
}
