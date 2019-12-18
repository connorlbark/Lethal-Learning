using System;
using UnityEngine;
using LLHandlers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace LethalLearning
{

    public class LethalLearning : MonoBehaviour
    {
        public static LethalLearning instance = null;
        private World world = null;
        private State state = null;
        private static bool hasSentStart = false;
        private static bool hasSentEnd = false;
        private static bool readyForData = false;
        private static Socket listener = null;
        private static Socket client = null;
        private static byte[] buffer = new byte[2];

        public static void Initialize()
        {
            GameObject gameObject = new GameObject("LethalLearning");
            instance = gameObject.AddComponent<LethalLearning>();
            IPAddress ipAddr = GetLocalIPAddress();

            IPEndPoint localEndpoint = new IPEndPoint(ipAddr, 3000);
            listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndpoint);
            listener.Listen(10);
            listener.BeginAccept(Accept, null);
            
            Console.WriteLine("Listening on " + localEndpoint.Address.ToString() + ":" + localEndpoint.Port.ToString());
            DontDestroyOnLoad(gameObject);
            //openPipe();
        }

        private static void Accept(IAsyncResult r)
        {
            client = listener.EndAccept(r);
            client.BeginReceive(buffer, 0, 2, SocketFlags.None, receiveData, null);
        }

        private static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void Update()
        {

            if (world == null)
            {
                world = World.instance;
            }
            else
            {
                state = GetState();
                if (client != null && readyForData)
                {

                    if (World.state == NMNBAFMOBNA.IMPPNEEEKAN)
                    {
                        SendState();

                    } else if (World.state == NMNBAFMOBNA.NLJIKMKLIMC)
                    {
                        if (hasSentEnd == false)
                        {
                            SendEnd();
                        } else if (hasSentStart == false)
                        {
                            SendStart();
                        }

                    } else
                    {
                        Console.WriteLine("Ready, but nothing to report...");
                    }
                } else
                {
                    Console.WriteLine("Not ready yet.");
                }
            }


        }

        private static void receiveData(IAsyncResult r)
        {
            client.EndReceive(r);
            readyForData = true;
            client.BeginReceive(buffer, 0, 2, SocketFlags.None, receiveData, null);
        }

        private void OnGUI()
        {
            GUI.contentColor = Color.white;
            var style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.MiddleLeft;

            if (world != null && state != null) 
            {             
                //ball.ballData.ballScale;
                GUI.Box(new Rect(10, 10, 1000, 25), "world- " + state.world.ToString() + "state: " + World.state, style);
                GUI.Box(new Rect(10, 35, 1000, 25), "ball- " + state.ball.ToString(), style);
                GUI.Box(new Rect(10, 60, 2000, 25), "player- " + state.player.ToString(), style);
                GUI.Box(new Rect(10, 85, 2000, 25), "enemy- " + state.enemy.ToString(), style);
            }
            else
            {
                GUI.Box(new Rect(10, 10, 1000, 25), "world is null; state: " + World.state, style);
            }
        }

        void OnDestroy()
        {
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            } catch { }
        }


        public State GetState() 
        {
            var ball = world.ballHandler.GetBall();
            var numPlayers = PlayerHandler.AmountPlayersIngame();

            PlayerState playerState = null;
            PlayerState enemyState = null;
            for (int i = 0; i < numPlayers; i++)
            {

                // Player.Get(playerId int)
                var playerObj = ALDOKEMAOMB.BJDPHEHJJJK(i);
                // player.get_isAI
                var isAi = playerObj.ALBOPCLADGN;
                // player.playerEntity 
                var entity = playerObj.JCCIAMJEODH;
                if (isAi)
                {
                    enemyState = new PlayerState(entity, ball);
                }
                else
                {
                    playerState = new PlayerState(entity, ball);
                }
            }
            BallState ballState = new BallState(ball);
            WorldState worldState = new WorldState(world);
            return new State(worldState, ballState, playerState, enemyState);
        }

        public void SendState()
        {
            var data = "STATE," + convertStateArrToString(state.ToArray());
            client.Send(Encoding.ASCII.GetBytes(data));
            readyForData = false;
            if (hasSentEnd == true)
            {
                hasSentEnd = false;
            }
        }

        public void SendEnd()
        {
            client.Send(Encoding.ASCII.GetBytes("END"));
            hasSentEnd = true;
        }

        public void SendStart()
        {
            client.Send(Encoding.ASCII.GetBytes("START"));
            hasSentStart = true;
        }

        public string convertStateArrToString(object[] state)
        {
            var output = new StringBuilder();
            for (var i = 0; i < state.Length; i++)
            {
                if (i != 0)
                {
                    output.Append(',');
                }
                output.Append(state[i].ToString());
            }
            return output.ToString();
        }

       
    }

    public class State
    {
        public State(WorldState world, BallState ball, PlayerState player, PlayerState enemy) 
        {
            this.player = player;
            this.ball = ball;
            this.world = world;
            this.enemy = enemy;
        }

        public System.Object[] ToArray()
        {
            var output = new List<System.Object>();
            output.AddRange(this.player.ToList());
            output.AddRange(this.enemy.ToList());
            output.AddRange(this.ball.ToList());
            output.AddRange(this.world.ToList());
            return output.ToArray();

        }

        public PlayerState player;
        public PlayerState enemy;
        public BallState ball;
        public WorldState world;

    }

    public class PlayerState
    {

        public PlayerState(GameplayEntities.PlayerEntity entity, GameplayEntities.BallEntity ball)
        {
            this.position = entity.GetPosition();
            this.health = entity.hitableData.hp;
            this.velocity = entity.entityData.velocity;
            this.hasFullEnergy = entity.HasFullEnergy() ? 1 : 0;
            this.stocks = entity.playerData.stocks;
            this.isInHitpause = entity.IsInHitpause() ? 1 : 0;
            this.isHittingBall = entity.IsHittingBall() ? 1 : 0;
            this.parrySuccess = entity.attackingData.parrySuccess ? 1 : 0;
            this.ownBall = ball.ballData.team == entity.GetTeam() ? 1 : 0;
            this.state = entity.GetCurrentAbilityState();
        }

        public override String ToString()
        {
            return "state: " + this.state.ability + ", own ball: " + this.ownBall + ", health: " + this.health.ToString() + ", hitting: " + this.isHittingBall.ToString() + ", hitpause: " + this.isInHitpause.ToString() + ", parrySuccess: " + this.parrySuccess.ToString() + ", stocks: " + this.stocks.ToString() + ", pos: " + this.position.ToString() + ", vel: " + this.velocity.ToString();
        }

        public List<System.Object> ToList()
        {
            var list =  new List<System.Object> {
                this.velocity.GCPKPHMKLBN.ToString(),
                this.velocity.CGJJEHPPOAN.ToString(),
                this.position.GCPKPHMKLBN.ToString(),
                this.position.CGJJEHPPOAN.ToString(),
                this.health.ToString(),
                this.hasFullEnergy,
                this.stocks,
                this.isHittingBall,
                this.isInHitpause,
                this.parrySuccess,
                this.ownBall,
            };
            list.AddRange(this.AbilityOneHot());
            return list;
        }

        public List<System.Object> AbilityOneHot()
        {
            var numAbilities = abilities.Count;
            var output = new List<System.Object>(new System.Object[numAbilities]);
            for (var i = 0; i < output.Count; i++)
            {
                output[i] = 0;
            }
            output[abilities[this.state.ability]] = 1;
            return output;
        }

        // Vector2d
        public IBGCBLLKIHA position;
        // HitableData.hp
        public HHBCPNCDNDH health;
        //        public IBGCBLLKIHA playerVelocity;
        public IBGCBLLKIHA velocity;
        public int hasFullEnergy;
        public int stocks;
        // Entity.IsHittingBall
        public int isHittingBall;
        // Entity.IsInHitpause
        public int isInHitpause;
        // AttackingData.parrySuccess
        public int parrySuccess;
        public int ownBall;
        public Abilities.AbilityState state;

        private static Dictionary<string, int> abilities = new Dictionary<string, int> {
            { "taunt", 0 },
            { "Expression", 1 },
            { "neutralSwing", 2 },
            { "bunt", 3 },
            { "crouch", 4 },
            { "pitch", 5 },
            { "doubleStrike", 6 },
            { "knockedOut", 7 },
            { "getUpGrab", 8 },
            { "getUp", 9 },
            { "smash", 10 },
            { "downAirSwing", 11 },
            { "grab", 12 },
            { "", 13 }
        };
    }

    public class BallState
    {
        public BallState(GameplayEntities.BallEntity ball)
        {
            this.velocity = ball.entityData.velocity;
            this.position = ball.GetPosition();
        }

        public override string ToString()
        {
            return " ball vel: " + this.velocity.ToString() + ", ball pos: " + this.position.ToString(); 
        }

        public List<System.Object> ToList() 
        {
            return new List<System.Object> {
                this.velocity.GCPKPHMKLBN.ToString(),
                this.velocity.CGJJEHPPOAN.ToString(),
                this.position.GCPKPHMKLBN.ToString(),
                this.position.CGJJEHPPOAN.ToString(),
            };
        }

        public IBGCBLLKIHA velocity;
        public IBGCBLLKIHA position;
//        public bool isBeingBunted;
    }

    public class WorldState
    {    
        public WorldState(World world) { }

        public override string ToString()
        {
            switch (World.state) {
                case NMNBAFMOBNA.IMPPNEEEKAN:
                    return "state: In-game";
                case NMNBAFMOBNA.NLJIKMKLIMC:
                    return "state: Menu";
                case NMNBAFMOBNA.CBPEHBCCEMG:
                    return "state: Paused";

            }
            return "";
        }

        public List<System.Object> ToList()
        {
            return new List<System.Object>();
        }
    }
}


