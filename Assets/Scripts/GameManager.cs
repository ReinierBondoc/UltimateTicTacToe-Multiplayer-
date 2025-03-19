using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : NetworkBehaviour{


    public static GameManager Instance { get; private set;}

    
    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs: EventArgs {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs{
        public Line line;
    }
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;

    public enum PlayerType{
        None,
        Cross,
        Circle,
    }

    public enum Orientation{
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }
    public struct Line {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPostion;
        public Orientation orientation;
    }

    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;
    


    private void Awake() {
        if (Instance != null){
            Debug.LogError("More than one GameManager instance!");
        }
        Instance = this;
        playerTypeArray = new PlayerType[5, 5];
        lineList = new List<Line>{
            //Horizontal wins
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0), },
                centerGridPostion = new Vector2Int(2, 0),
                orientation = Orientation.Horizontal,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1), new Vector2Int(4,1), },
                centerGridPostion = new Vector2Int(2, 1),
                orientation = Orientation.Horizontal,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2), new Vector2Int(4,2), },
                centerGridPostion = new Vector2Int(2, 2),
                orientation = Orientation.Horizontal,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,3), new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(3,3), new Vector2Int(4,3), },
                centerGridPostion = new Vector2Int(2, 3),
                orientation = Orientation.Horizontal,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,4), new Vector2Int(1,4), new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4), },
                centerGridPostion = new Vector2Int(2, 4),
                orientation = Orientation.Horizontal,
            },

            //Vertical wins
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4), },
                centerGridPostion = new Vector2Int(0, 2),
                orientation = Orientation.Vertical,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(1,3), new Vector2Int(1,4), },
                centerGridPostion = new Vector2Int(1, 2),
                orientation = Orientation.Vertical,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), new Vector2Int(2,3), new Vector2Int(2,4), },
                centerGridPostion = new Vector2Int(2, 2),
                orientation = Orientation.Vertical,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(3,0), new Vector2Int(3,1), new Vector2Int(3,2), new Vector2Int(3,3), new Vector2Int(3,4), },
                centerGridPostion = new Vector2Int(3, 2),
                orientation = Orientation.Vertical,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(4,0), new Vector2Int(4,1), new Vector2Int(4,2), new Vector2Int(4,3), new Vector2Int(4,4), },
                centerGridPostion = new Vector2Int(4, 2),
                orientation = Orientation.Vertical,
            },

            //Diagonal wins
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(1,1), new Vector2Int(2,2), new Vector2Int(3,3), new Vector2Int(4,4), },
                centerGridPostion = new Vector2Int(2, 2),
                orientation = Orientation.DiagonalA,
            },
            new Line{
                gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,4), new Vector2Int(1,3), new Vector2Int(2,2), new Vector2Int(3,1), new Vector2Int(4,0), },
                centerGridPostion = new Vector2Int(2, 2),
                orientation = Orientation.DiagonalB,
            },
        };
    }

    public override void OnNetworkSpawn(){
        Debug.Log("OnNetworkSpawn: " + NetworkManager.Singleton.LocalClientId);
        if(NetworkManager.Singleton.LocalClientId == 0){
            localPlayerType = PlayerType.Cross;
        }
        else{
            localPlayerType = PlayerType.Circle;
        }

        if(IsServer){
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) => {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty); 

        };

    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj){
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2){
            //Beginning the game
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc(){
        OnGameStarted?.Invoke(this, EventArgs.Empty);

    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType){

        Debug.Log("ClickedOnGridPosition " + x + ", " + y);
        if (playerType != currentPlayablePlayerType.Value){
            return;
        }

        if(playerTypeArray[x,y] != PlayerType.None){
            //Player took that spot already
            return;
        }

        playerTypeArray[x, y] = playerType;    



        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs{
            x = x,
            y = y,
            playerType = playerType,
        });

        switch (currentPlayablePlayerType.Value) {
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;
        }

        TestWinner();


    }

    private bool TestWinnerLine(Line line){
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y], 
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y], 
            playerTypeArray[line.gridVector2IntList[3].x, line.gridVector2IntList[3].y], 
            playerTypeArray[line.gridVector2IntList[4].x, line.gridVector2IntList[4].y] 
        );
    }
    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType, PlayerType dPlayerType, PlayerType ePlayerType){
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType &&
            cPlayerType == dPlayerType &&
            dPlayerType == ePlayerType;
    }
    private void TestWinner() {
        foreach(Line line in lineList) {
            if (TestWinnerLine(line)) {
                //Winner!!!
                Debug.Log("Winner!");
                currentPlayablePlayerType.Value = PlayerType.None;
                OnGameWin?.Invoke(this, new OnGameWinEventArgs {
                    line = line
                    });
                    break;
            }
        }
    }
    

    public PlayerType GetLocalPlayerType(){
        return localPlayerType;
    }


    public PlayerType GetCurrentPlayablePlayerType(){
        return currentPlayablePlayerType.Value;
    }

}
