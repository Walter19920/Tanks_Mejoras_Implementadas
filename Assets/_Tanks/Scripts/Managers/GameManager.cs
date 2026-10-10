using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Tanks.Complete
{
    public class GameManager : MonoBehaviour
    {
        // Variable estática para conservar la selección de tanques entre niveles
        private static PlayerData[] s_SavedPlayerData;

        // Variable estática para conservar el conteo continuo de rondas entre biomas/escenas
        private static int s_GlobalRoundNumber = 0;

        // Which state the game is currently in
        public enum GameState
        {
            MainMenu,
            Game
        }

        // Data about the selected tanks passed from the menu to the GameManager
        public class PlayerData
        {
            public bool IsComputer;
            public Color TankColor;
            public GameObject UsedPrefab;
            public int ControlIndex;
        }

        public int m_NumRoundsToWin = 5;             // The number of rounds a single player has to win to win the game.
        public float m_StartDelay = 3f;              // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;                // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.

        [Header("Tanks Prefabs")]
        public GameObject m_Tank1Prefab;            // The Prefab used by the tank in Slot 1 of the Menu
        public GameObject m_Tank2Prefab;            // The Prefab used by the tank in Slot 2 of the Menu
        public GameObject m_Tank3Prefab;            // The Prefab used by the tank in Slot 3 of the Menu
        public GameObject m_Tank4Prefab;            // The Prefab used by the tank in Slot 4 of the Menu

        [FormerlySerializedAs("m_Tanks")]
        public TankManager[] m_SpawnPoints;         // A collection of managers for enabling and disabling different aspects of the tanks.

        private GameState m_CurrentState;

        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankManager m_RoundWinner;          // Reference to the winner of the current round. Used to make an announcement of who won.
        private TankManager m_GameWinner;           // Reference to the winner of the game. Used to make an announcement of who won.

        private PlayerData[] m_TankData;            // Data passed from the menu about each selected tank (at least 2, max 4)
        private int m_PlayerCount = 0;              // The number of players (2 to 4), decided from the number of PlayerData passed by the menu
        private TextMeshProUGUI m_TitleText;        // The text used to display game message. Automatically found as part of the Menu prefab

        private void Start()
        {
            m_CurrentState = GameState.MainMenu;

            // Find the text used to display game info.
            var textRef = FindAnyObjectByType<MessageTextReference>(FindObjectsInactive.Include);

            if (textRef == null)
            {
                Debug.LogError("You need to add the Menus prefab in the scene to use the GameManager!");
                return;
            }

            m_TitleText = textRef.Text;
            m_TitleText.text = "";

            if (m_Tank1Prefab == null || m_Tank2Prefab == null || m_Tank3Prefab == null || m_Tank4Prefab == null)
            {
                Debug.LogError("You need to assign 4 tank prefab in the GameManager!");
            }

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Si iniciamos una partida nueva desde el Nivel 1 (sin datos guardados de un nivel previo), reiniciamos el contador
            if (currentSceneIndex == 0 && s_SavedPlayerData == null)
            {
                s_GlobalRoundNumber = 0;
            }

            // AUTO-INICIO EN NIVEL 2: Si venimos del Nivel 1 con tanques guardados
            if (currentSceneIndex > 0 && s_SavedPlayerData != null)
            {
                OcultarPantallasDeMenu(textRef);
                StartGame(s_SavedPlayerData);
            }
        }

        private void OcultarPantallasDeMenu(MessageTextReference textRef)
        {
            if (textRef == null) return;

            // 1. Activar el texto de mensajes y todos sus contenedores padres
            textRef.gameObject.SetActive(true);
            Transform parent = textRef.transform.parent;
            while (parent != null)
            {
                parent.gameObject.SetActive(true);
                parent = parent.parent;
            }

            // 2. Ocultar el TitleCanvas si existe y no contiene el texto de anuncio
            GameObject titleCanvas = GameObject.Find("TitleCanvas");
            if (titleCanvas != null && !textRef.transform.IsChildOf(titleCanvas.transform))
            {
                titleCanvas.SetActive(false);
            }

            // 3. Ocultar los paneles visuales de selección de menú
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if (!textRef.transform.IsChildOf(canvas.transform) &&
                    canvas.gameObject != textRef.gameObject &&
                    !canvas.name.Contains("MobileControl"))
                {
                    canvas.gameObject.SetActive(false);
                }
                else
                {
                    // Si es el Canvas que contiene el texto de anuncio, ocultamos solo los elementos visuales de menú
                    foreach (Transform child in canvas.transform)
                    {
                        if (!textRef.transform.IsChildOf(child) && child.gameObject != textRef.gameObject)
                        {
                            string childName = child.name.ToLower();
                            if (childName.Contains("menu") ||
                                childName.Contains("selection") ||
                                childName.Contains("panel") ||
                                childName.Contains("title"))
                            {
                                child.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        void GameStart()
        {
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            SpawnAllTanks();
            SetCameraTargets();

            StartCoroutine(GameLoop());
        }

        void ChangeGameState(GameState newState)
        {
            m_CurrentState = newState;

            switch (m_CurrentState)
            {
                case GameState.Game:
                    GameStart();
                    break;
            }
        }

        public void StartGame(PlayerData[] playerData)
        {
            s_SavedPlayerData = playerData; // Guardar tanques globalmente
            m_TankData = playerData;
            m_PlayerCount = m_TankData.Length;
            ChangeGameState(GameState.Game);
        }

        private void SpawnAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                var playerData = m_TankData[i];

                m_SpawnPoints[i].m_Instance =
                    Instantiate(playerData.UsedPrefab, m_SpawnPoints[i].m_SpawnPoint.position, m_SpawnPoints[i].m_SpawnPoint.rotation) as GameObject;

                var mov = m_SpawnPoints[i].m_Instance.GetComponent<TankMovement>();
                if (mov != null)
                {
                    // Asignación correcta del control de la IA del tanque enemigo/bot
                    mov.m_IsComputerControlled = playerData.IsComputer;
                }

                m_SpawnPoints[i].m_PlayerNumber = i + 1;
                m_SpawnPoints[i].ControlIndex = playerData.ControlIndex;
                m_SpawnPoints[i].m_PlayerColor = playerData.TankColor;
                m_SpawnPoints[i].m_ComputerControlled = playerData.IsComputer;
            }

            foreach (var tank in m_SpawnPoints)
            {
                if (tank.m_Instance == null)
                    continue;

                tank.Setup(this);
            }
        }

        private void SetCameraTargets()
        {
            Transform[] targets = new Transform[m_PlayerCount];

            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = m_SpawnPoints[i].m_Instance.transform;
            }

            m_CameraControl.m_Targets = targets;
        }

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            if (m_GameWinner != null)
            {
                int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

                if (currentSceneIndex == 0)
                {
                    // Si gana en el Nivel 1 (Main), pasa al Nivel 2 (Luna)
                    SceneManager.LoadScene(1);
                }
                else
                {
                    // Si gana en el Nivel 2 (Luna), limpia los datos, reinicia las rondas y regresa al Nivel 1
                    s_SavedPlayerData = null;
                    s_GlobalRoundNumber = 0;
                    SceneManager.LoadScene(0);
                }
            }
            else
            {
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator RoundStarting()
        {
            ResetAllTanks();
            DisableTankControl();

            m_CameraControl.SetStartPositionAndSize();

            // Incrementamos la ronda global para que no se reinicie al cambiar de bioma
            s_GlobalRoundNumber++;
            m_RoundNumber = s_GlobalRoundNumber;
            m_TitleText.text = "ROUND " + m_RoundNumber;

            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            EnableTankControl();
            m_TitleText.text = string.Empty;

            while (!OneTankLeft())
            {
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            DisableTankControl();

            m_RoundWinner = null;
            m_RoundWinner = GetRoundWinner();

            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            m_GameWinner = GetGameWinner();

            string message = EndMessage();
            m_TitleText.text = message;

            yield return m_EndWait;
        }

        private bool OneTankLeft()
        {
            int numTanksLeft = 0;

            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            return numTanksLeft <= 1;
        }

        private TankManager GetRoundWinner()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    return m_SpawnPoints[i];
            }

            return null;
        }

        private TankManager GetGameWinner()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Wins == m_NumRoundsToWin)
                    return m_SpawnPoints[i];
            }

            return null;
        }

        private string EndMessage()
        {
            string message = "DRAW!";

            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            message += "\n\n\n\n";

            for (int i = 0; i < m_PlayerCount; i++)
            {
                message += m_SpawnPoints[i].m_ColoredPlayerText + ": " + m_SpawnPoints[i].m_Wins + " WINS\n";
            }

            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }

        private void ResetAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].Reset();
            }
        }

        private void EnableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].EnableControl();
            }
        }

        private void DisableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                m_SpawnPoints[i].DisableControl();
            }
        }
    }
}