namespace GoogleARCore.Examples.CloudAnchors
{
    using System.Collections.Generic;
    using GoogleARCore;
    using GoogleARCore.CrossPlatform;
    using GoogleARCore.Examples.Common;
    using UnityEngine;
    using UnityEngine.UI;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    using Input = InstantPreviewInput;
#endif

    /// <summary>
    /// Controller for our VroomARoom game
    /// </summary>
    public class OurCloudController : MonoBehaviour
    {
        /// <summary>
        /// Manages sharing Anchor Ids across the local network to clients using Unity's NetworkServer.  There
        /// are many ways to share this data and this not part of the ARCore Cloud Anchors API surface.
        /// </summary>
        public RoomSharingServer RoomSharingServer;

        /// <summary>
        /// A controller for managing UI associated with the example.
        /// </summary>
        public OurCloudAnchorUIController UIController;

        /// <summary>
        /// A controller for managing the network aspect of this app
        /// </summary>
        public OurCloudNetworkManager NetworkController;

        [Header("ARCore Device")]

        /// <summary>
        /// The root for ARCore-specific GameObjects in the scene.
        /// </summary>
        public GameObject ARCoreRoot;

        /// <summary>
        /// An Andy Android model to visually represent anchors in the scene; this uses ARCore
        /// lighting estimation shaders.
        /// </summary>
        public GameObject GameARObject;

        [Header("Prefab Objects")]
        /// <summary>
        /// Our game manager.
        /// </summary>
        public GameManager OurGameManager;

        /// <summary>
        /// The ball prefab.
        /// </summary>
        public GameObject ballPrefab;

        public GameObject ballLocate;

        /// <summary>
        /// The instantiated object
        /// </summary>
        private GameObject PlayerPaddle;

        /// <summary>
        /// The loopback ip address.
        /// </summary>
        private const string k_LoopbackIpAddress = "127.0.0.1";

        /// <summary>
        /// The rotation in degrees need to apply to model when the Andy model is placed.
        /// </summary>
        private const float k_ModelRotation = 180.0f;

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        /// <summary>
        /// The last placed anchor.
        /// </summary>
        private Component m_LastPlacedAnchor = null;

        /// <summary>
        /// The last resolved anchor.
        /// </summary>
        private XPAnchor m_LastResolvedAnchor = null;

        /// <summary>
        /// The current cloud anchor mode.
        /// </summary>
        private ApplicationMode m_CurrentMode = ApplicationMode.Ready;

        /// <summary>
        /// Current local room to attach next Anchor to.
        /// </summary>
        private int m_CurrentRoom;

        private bool gameFieldExists = false;
        private bool buttonLeftPressed = false;
        private bool buttonRightPressed = false;
        private bool gamePlayingUnderway = false;
        private string ipAddress = k_LoopbackIpAddress;
        private Transform AnchorLocation;
        private UnityEngine.Networking.NetworkClient NetHost;
        private UnityEngine.Networking.NetworkClient NetClient;

        /// <summary>
        /// Enumerates modes the example application can be in.
        /// </summary>
        public enum ApplicationMode
        {
            Ready,
            Hosting,
            Resolving,
        }




        /// <summary>
        /// The Unity Start() Method
        /// </summary>
        public void Start()
        {
            ARCoreRoot.SetActive(true);

            _ResetStatus();
        }

        /// <summary>
        /// The Unity Update() Method
        /// </summary>
        public void Update()
        {
            _UpdateApplicationLifecycle();

            // If we are not in hosting mode or the resolving mode then the update is complete.
            if (m_CurrentMode == ApplicationMode.Ready)
            {
                return;
            }

            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                if (gamePlayingUnderway && NetClient.isConnected)
                {
                    UIController.ShowPlayingMode();
                    return;
                }

                return;
            }

            // APPLICATION MODE : HOSTING
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                if (gamePlayingUnderway && NetworkController.IsClientConnected())
                {
                    UIController.ShowPlayingMode();
                    return;
                }

                //if (gameFieldExists)
                //{
                //    if (buttonLeftPressed)
                //        MovePlayerLeft();
                //    else if (buttonRightPressed)
                //        MovePlayerRight();
                //    return;
                //}
            }

            // If the user has already placed an achor then the update is complete
            if (m_LastPlacedAnchor != null)
            {
                return;
            }

            // If the player has not touched the screen then the update is complete.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            if (Frame.Raycast(touch.position.x, touch.position.y, TrackableHitFlags.PlaneWithinPolygon, out hit))
            {
                m_LastPlacedAnchor = hit.Trackable.CreateAnchor(hit.Pose);
            }

            if (m_LastPlacedAnchor != null)
            {
                // Instantiate Andy model at the hit pose.
                var andyObject = Instantiate(GameARObject, m_LastPlacedAnchor.transform.position,
                    m_LastPlacedAnchor.transform.rotation);

                // Compensate for the hitPose rotation facing away from the raycast (i.e. camera).
                //andyObject.transform.Rotate(0, k_ModelRotation, 0, Space.Self);
                andyObject.transform.Rotate(0, 0, 0, Space.Self);

                // Make Andy model a child of the anchor.
                andyObject.transform.parent = m_LastPlacedAnchor.transform;

                // Save cloud anchor.
                _HostLastPlacedAnchor();

                PlayerPaddle = andyObject;
                //NetworkController = andyObject.GetComponentInChildren(typeof(OurCloudNetworkManager)) as OurCloudNetworkManager;

                gameFieldExists = true;
            }
        }

        /// <summary>
        /// Handles user intent to enter a mode where they can place an anchor to host or to exit this mode if
        /// already in it.
        /// </summary>
        public void OnEnterHostingModeClick()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                return;
            }

            m_CurrentMode = ApplicationMode.Hosting;
            m_CurrentRoom = Random.Range(1, 9999);
            UIController.SetRoomTextValue(m_CurrentRoom);
            UIController.ShowHostingModeBegin();
        }

        /// <summary>
        /// Handles a user intent to enter a mode where they can input an anchor to be resolved or exit this mode if
        /// already in it.
        /// </summary>
        public void OnEnterResolvingModeClick()
        {
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                return;
            }

            m_CurrentMode = ApplicationMode.Resolving;
            UIController.ShowResolvingModeBegin();
        }

        /// <summary>
        /// Handles the user intent to resolve the cloud anchor associated with a room they have typed into the UI.
        /// </summary>
        public void OnResolveRoomClick()
        {
            var roomToResolve = UIController.GetRoomInputValue();
            if (roomToResolve == 0)
            {
                UIController.ShowResolvingModeBegin("Anchor resolve failed due to invalid room code.");
                return;
            }

            UIController.SetRoomTextValue(roomToResolve);
            ipAddress =
                UIController.GetResolveOnDeviceValue() ? k_LoopbackIpAddress : UIController.GetIpAddressInputValue();

            UIController.ShowResolvingModeAttemptingResolve();
            RoomSharingClient roomSharingClient = new RoomSharingClient();
            roomSharingClient.GetAnchorIdFromRoom(roomToResolve, ipAddress, (bool found, string cloudAnchorId) =>
            {
                if (!found)
                {
                    UIController.ShowResolvingModeBegin("Anchor resolve failed due to invalid room code, " +
                                                        "ip address or network error.");
                }
                else
                {
                    _ResolveAnchorFromId(cloudAnchorId);
                }
            });
        }

        /// <summary>
        /// Handles the user intent to start the pong game
        /// </summary>
        public void OnStartGameClick()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                //bool noConnection = (NetworkController.client == null || NetworkController.client.connection == null ||
                                 //NetworkController.client.connection.connectionId == -1);

                if (gamePlayingUnderway)
                {
                    UIController.ShowPlayingMode("Connect");
                    gamePlayingUnderway = false;
                    NetworkController.StopHost();
                    return;
                }

                UIController.ShowPlayingMode("Disconnect");
                gamePlayingUnderway = true;
                NetHost = NetworkController.StartHost();

            }
            else if (m_CurrentMode == ApplicationMode.Resolving)
            {
                if (gamePlayingUnderway)
                {
                    UIController.ShowPlayingMode("Connect");
                    gamePlayingUnderway = false;
                    NetworkController.StopClient();
                    return;
                }

                UIController.ShowPlayingMode("Disconnect");
                gamePlayingUnderway = true;
                NetworkController.networkAddress = ipAddress;
                NetClient = NetworkController.StartClient();
            }
        }

        public ApplicationMode GetCurrentMode()
        {
            return m_CurrentMode;
        }

        public Transform GetBallLocation()
        {
            return AnchorLocation != null ? AnchorLocation.transform : null;
        }

        //////////////////////////
        ///// PADDLE EDITS HERE 
        //////////////////////////

        //public void ButtonLeftAction() { buttonLeftPressed = true; }
        //public void ButtonLeftStop() { buttonLeftPressed = false; }

        //private void MovePlayerLeft() {
        //    var x = Time.deltaTime * 0.7f;
        //    PlayerPaddle.transform.Translate(x, 0, 0);
        //    Debug.Log("MOVE L");
        //}

        //public void ButtonRightAction() { buttonRightPressed = true; }
        //public void ButtonRightStop() { buttonRightPressed = false; }

        //private void MovePlayerRight()
        //{
        //    var x = Time.deltaTime * 0.7f;
        //    PlayerPaddle.transform.Translate(-x, 0, 0);
        //    Debug.Log("MOVE R");
        //}

        //////////////////////////
        ///// STOP
        //////////////////////////



        /// <summary>
        /// Hosts the user placed cloud anchor and associates the resulting Id with the current room.
        /// </summary>
        private void _HostLastPlacedAnchor()
        {
#if !UNITY_IOS || ARCORE_IOS_SUPPORT

#if !UNITY_IOS
            var anchor = (Anchor)m_LastPlacedAnchor;
#else
            var anchor = (UnityEngine.XR.iOS.UnityARUserAnchorComponent)m_LastPlacedAnchor;
#endif
            UIController.ShowHostingModeAttemptingHost();
            XPSession.CreateCloudAnchor(anchor).ThenAction(result =>
            {
                if (result.Response != CloudServiceResponse.Success)
                {
                    UIController.ShowHostingModeBegin(
                        string.Format("Failed to host cloud anchor: {0}", result.Response));
                    return;
                }

                RoomSharingServer.SaveCloudAnchorToRoom(m_CurrentRoom, result.Anchor);
                UIController.ShowHostingModeBegin("Cloud anchor was created and saved.");
            });
#endif
        }

        /// <summary>
        /// Resolves an anchor id and instantiates a field prefab on it.
        /// </summary>
        /// <param name="cloudAnchorId">Cloud anchor id to be resolved.</param>
        private void _ResolveAnchorFromId(string cloudAnchorId)
        {
            XPSession.ResolveCloudAnchor(cloudAnchorId).ThenAction((System.Action<CloudAnchorResult>)(result =>
            {
                if (result.Response != CloudServiceResponse.Success)
                {
                    UIController.ShowResolvingModeBegin(string.Format("Resolving Error: {0}.", result.Response));
                    return;
                }

                m_LastResolvedAnchor = result.Anchor;
                Instantiate(GameARObject, result.Anchor.transform);
                UIController.ShowResolvingModeSuccess();
            }));
        }

        /// <summary>
        /// Resets the internal status and UI.
        /// </summary>
        private void _ResetStatus()
        {
            // Reset internal status.
            m_CurrentMode = ApplicationMode.Ready;
            if (m_LastPlacedAnchor != null)
            {
                Destroy(m_LastPlacedAnchor.gameObject);
            }

            m_LastPlacedAnchor = null;
            if (m_LastResolvedAnchor != null)
            {
                Destroy(m_LastResolvedAnchor.gameObject);
            }

            m_LastResolvedAnchor = null;
            UIController.ShowReadyMode();

            NetworkController.StopHost();
            NetworkController.StopClient();
            gameFieldExists = false;
            buttonLeftPressed = false;
            buttonRightPressed = false;
            gamePlayingUnderway = false;
            ipAddress = k_LoopbackIpAddress;
        }

        /// <summary>
        /// Check and update the application lifecycle.
        /// </summary>
        private void _UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            var sleepTimeout = SleepTimeout.NeverSleep;

            // Only allow the screen to sleep when not tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                sleepTimeout = lostTrackingSleepTimeout;
            }
            Screen.sleepTimeout = sleepTimeout;

            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}
