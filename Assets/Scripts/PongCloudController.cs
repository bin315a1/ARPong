//-----------------------------------------------------------------------
// Fall 2018
// CS117 Final Project
// Vroom ARoom
// By Junya Honda, Han Lee, Moo Jin Kim, Jonathan Myong 
//-----------------------------------------------------------------------

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
    /// Controller our AR Pong game.
    /// </summary>
    public class PongCloudController : MonoBehaviour
    {
        /// <summary>
        /// Manages sharing Anchor Ids across the local network to clients using Unity's NetworkServer.  There
        /// are many ways to share this data and this not part of the ARCore Cloud Anchors API surface.
        /// </summary>
        public RoomSharingServer RoomSharingServer;

        /// <summary>
        /// A controller for managing UI.
        /// </summary>
        public PongUIController UIController;

        /// <summary>
        /// A controller for managing network connections between players.
        /// </summary>
        public PongNetworkController NetController;

        /// <summary>
        /// The Game Manager for our AR Pong Game
        /// </summary>
        public GameManager GManager;

        [Header("ARCore")]

        /// <summary>
        /// The root for ARCore-specific GameObjects in the scene.
        /// </summary>
        public GameObject ARCoreRoot;

        /// <summary>
        /// An Andy Android model to visually represent anchors in the scene; this uses ARCore
        /// lighting estimation shaders.
        /// </summary>
        public GameObject ARCoreGameFieldPrefab;

        [Header("ARKit")]

        /// <summary>
        /// The root for ARKit-specific GameObjects in the scene.
        /// </summary>
        public GameObject ARKitRoot;

        /// <summary>
        /// The first-person camera used to render the AR background texture for ARKit.
        /// </summary>
        public Camera ARKitFirstPersonCamera;

        /// <summary>
        /// An Andy Android model to visually represent anchors in the scene; this uses
        /// standard diffuse shaders.
        /// </summary>
        public GameObject ARKitGameFieldPrefab;

        /// <summary>
        /// The loopback ip address.
        /// </summary>
        private const string k_LoopbackIpAddress = "127.0.0.1";

        /// <summary>
        /// The rotation in degrees need to apply to model when GameField object is placed.
        /// </summary>
        private const float k_ModelRotation = 180.0f;

        /// <summary>
        /// A helper object to ARKit functionality.
        /// </summary>
        private ARKitHelper m_ARKit = new ARKitHelper();

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

        /// <summary>
        /// The host IP Address the network manager should connect to.
        /// </summary>
        private string hostIPAddress = "localhost";

        /// <summary>
        /// Checks whether the user has already intended to connect.
        /// </summary>
        private bool NetClicked = false;

        /// <summary>
        /// A check for whether the game is playing or not.
        /// </summary>
        private bool GamePlaying = false;

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
        /// The Unity Start() method.
        /// </summary>
        public void Start()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                ARCoreRoot.SetActive(true);
                ARKitRoot.SetActive(false);
            }
            else
            {
                ARCoreRoot.SetActive(false);
                ARKitRoot.SetActive(true);
            }

            _ResetStatus();
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            _UpdateApplicationLifecycle();

            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                if (NetClicked)
                {
                    if (!NetController.isNetworkActive)
                    {
                        OnNetConnectClick();
                    }
                }
                return;
            }

            // If we are not in hosting mode or the user has already placed an anchor then the update
            // is complete.
            if (m_CurrentMode != ApplicationMode.Hosting || m_LastPlacedAnchor != null)
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
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                TrackableHit hit;
                if (Frame.Raycast(touch.position.x, touch.position.y,
                        TrackableHitFlags.PlaneWithinPolygon, out hit))
                {
                    m_LastPlacedAnchor = hit.Trackable.CreateAnchor(hit.Pose);
                }
            }
            else
            {
                Pose hitPose;
                if (m_ARKit.RaycastPlane(ARKitFirstPersonCamera, touch.position.x, touch.position.y, out hitPose))
                {
                    m_LastPlacedAnchor = m_ARKit.CreateAnchor(hitPose);
                }
            }

            if (m_LastPlacedAnchor != null)
            {
                // Instantiate Andy model at the hit pose.
                var andyObject = Instantiate(_GetGameFieldPrefab(), m_LastPlacedAnchor.transform.position, 
                                             m_LastPlacedAnchor.transform.rotation);

                // Compensate for the hitPose rotation facing away from the raycast (i.e. camera).
                andyObject.transform.Rotate(0, k_ModelRotation, 0, Space.Self);

                // Make GameField object a child of the anchor.
                andyObject.transform.parent = m_LastPlacedAnchor.transform;

                // Save cloud anchor.
                _HostLastPlacedAnchor();
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
            string ipAddress =
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
                    hostIPAddress = ipAddress;
                }
            });
        }

        /// <summary>
        /// Handles the user intent to connect to the network.
        /// </summary>
        public void OnNetConnectClick()
        {
            if (m_CurrentMode == ApplicationMode.Ready)
            {
                return;
            }
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                if (NetClicked)
                {
                    NetController.StopHost();
                    UIController.ShowConnectionEnd(true);
                    NetClicked = false;
                    GamePlaying = false;
                    return;
                }

                NetController.StartHost();
                UIController.ShowConnectionStart(true);
                NetClicked = true;

            }
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                if (NetClicked)
                {
                    NetController.StopClient();
                    UIController.ShowConnectionEnd(false);
                    NetClicked = false;
                    GamePlaying = false;
                    return;
                }

                // When game first starts it always is a server for some reason
                NetController.StopServer();
                NetController.networkAddress = hostIPAddress;

                NetController.StartClient();
                UIController.ShowConnectionStart(false);
                NetClicked = true;
            }
        }

        /// <summary>
        /// Handles the user intent to start the game of AR Pong.
        /// </summary>
        public void OnGameStartClick()
        {
            if (m_CurrentMode != ApplicationMode.Hosting)
                return;

            if (GamePlaying)
            {
                UIController.ShowGameEnd();
                GManager.StopGame();
                GamePlaying = false;
                return;
            }

            UIController.ShowGameStart();
            GManager.StartNewGame();
            GamePlaying = true;
        }

        /// <summary>
        /// Handles the user intent to start moving their PlayerPaddle to the left
        /// </summary>
        public void OnLeftButtonDown()
        {
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject playerObject in playerObjects)
            {
                PlayerPaddleControl playerPaddleObject = playerObject.GetComponent<PlayerPaddleControl>();
                playerPaddleObject.ButtonLeft(true);
            }
        }

        /// <summary>
        /// Handles the user intent to stop moving their PlayerPaddle to the left.
        /// </summary>
        public void OnLeftButtonUp()
        {
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject playerObject in playerObjects)
            {
                PlayerPaddleControl playerPaddleObject = playerObject.GetComponent<PlayerPaddleControl>();
                playerPaddleObject.ButtonLeft(false);
            }
        }

        /// <summary>
        /// Handles the user intent to start moving their PlayerPaddle to the right.
        /// </summary>
        public void OnRightButtonDown()
        {
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject playerObject in playerObjects)
            {
                PlayerPaddleControl playerPaddleObject = playerObject.GetComponent<PlayerPaddleControl>();
                playerPaddleObject.ButtonRight(true);
            }
        }

        /// <summary>
        /// Handles the user intent to stop moving their PlayerPaddle to the right.
        /// </summary>
        public void OnRightButtonUp()
        {
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject playerObject in playerObjects)
            {
                PlayerPaddleControl playerPaddleObject = playerObject.GetComponent<PlayerPaddleControl>();
                playerPaddleObject.ButtonRight(false);
            }
        }

        /// <summary>
        /// Gets if cloud anchor is hosting.
        /// </summary>
        /// <returns><c>true</c>, if cloud anchor host, <c>false</c> otherwise.</returns>
        public bool GetIfCloudAnchorHost()
        {
            return m_CurrentMode == ApplicationMode.Hosting ? true : false;
        }

        /// <summary>
        /// Gets if cloud anchor is resolving.
        /// </summary>
        /// <returns><c>true</c>, if cloud anchor resolving, <c>false</c> otherwise.</returns>
        public bool GetIfCloudAnchorResolving()
        {
            return m_CurrentMode == ApplicationMode.Resolving ? true : false;
        }

        /// <summary>
        /// Outputs the Debug Message Text onto the snackbar.
        /// </summary>
        /// <param name="text">The desired Debug Message Text output.</param>
        public void LogDebug(string text)
        {
            UIController.SetSnackValue(text);
        }



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
        /// Resolves an anchor id and instantiates a GameField prefab on it.
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
                var andyObject = Instantiate(_GetGameFieldPrefab(), result.Anchor.transform);
                andyObject.transform.Rotate(0, k_ModelRotation, 0, Space.Self);
                UIController.ShowResolvingModeSuccess();
            }));
        }

        /// <summary>
        /// Resets the internal status and UI.
        /// </summary>
        private void _ResetStatus()
        {
            if (NetClicked)
                OnNetConnectClick();
            if (GamePlaying)
                OnGameStartClick();

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
        }

        /// <summary>
        /// Gets the platform-specific GameField prefab.
        /// </summary>
        /// <returns>The platform-specific GameField prefab.</returns>
        private GameObject _GetGameFieldPrefab()
        {
            return Application.platform != RuntimePlatform.IPhonePlayer ?
                ARCoreGameFieldPrefab : ARKitGameFieldPrefab;
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

#if !UNITY_IOS
            // Only allow the screen to sleep when not tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                sleepTimeout = lostTrackingSleepTimeout;
            }
#endif

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
