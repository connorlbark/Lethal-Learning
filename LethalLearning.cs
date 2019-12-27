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
        private static DateTime? firstRequest = null;
        private static Socket listener = null;
        private static Socket client = null;
        private static byte[] buffer = new byte[2];
        private static GameplayEntities.PlayerEntity player = null;
        private static GameplayEntities.PlayerEntity opponent = null;
        private static int lastKills = 0;
        private static int lastDeaths = 0;


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
                    if (firstRequest == null)
                    {
                        firstRequest = DateTime.Now.AddSeconds(6.0);
                    }

                    if (DateTime.Now < firstRequest.Value)
                    {
                        return;
                    }

                    if (World.state == NMNBAFMOBNA.IMPPNEEEKAN)
                    {
                        if (player.playerData.kills > lastKills)
                        {
                            SendKill();
                            lastKills = player.playerData.kills;
                        }
                        else if (player.playerData.deaths > lastDeaths)
                        {
                            SendDeath();
                            lastDeaths = player.playerData.deaths;
                        }
                        else
                        {
                            SendState();
                        }
                    }
                    else if (World.state == NMNBAFMOBNA.NLJIKMKLIMC)
                    {
                        if (hasSentEnd == false)
                        {
                            SendEnd();
                        }
                        else if (hasSentStart == false)
                        {
                            SendStart();
                        }

                    }
                    else
                    {
                        Console.WriteLine("Ready, but nothing to report...");
                    }
                }
                else
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
            }
            catch { }
        }


        public State GetState()
        {
            var ball = world.ballHandler.GetBall();
            var numPlayers = PlayerHandler.AmountPlayersIngame();

            for (int i = 0; i < numPlayers; i++)
            {

                // Player.Get(playerId int)
                var playerObj = ALDOKEMAOMB.BJDPHEHJJJK(i);
                if (playerObj == null)
                {
                    continue;
                }
                // player.get_isAI
                var isAi = playerObj.ALBOPCLADGN;
                // player.playerEntity 
                var entity = playerObj.JCCIAMJEODH;
                if (isAi)
                {
                    opponent = entity;
                }
                else
                {
                    player = entity;
                }
            }
            PlayerState playerState = new PlayerState(player, ball);
            PlayerState enemyState = new PlayerState(opponent, ball, player);
            BallState ballState = new BallState(ball, player);
            WorldState worldState = new WorldState(world);
            return new State(worldState, ballState, playerState, enemyState);
        }

        public void SendState()
        {
            sendStateWithAction("STATE");

        }

        private void sendStateWithAction(string action)
        {
            var data = action + "," + convertStateArrToString(state.ToArray());
            client.Send(Encoding.ASCII.GetBytes(data));
            readyForData = false;
            if (hasSentEnd == true)
            {
                hasSentEnd = false;
                hasSentStart = false;
            }
        }

        private void sendEnd(string result)
        {
            var data = result + "," + convertStateArrToString(State.ZeroArray());
            client.Send(Encoding.ASCII.GetBytes(data));
            readyForData = false;
            hasSentEnd = true;
        }


        public void SendEnd()
        {
            //  NCMFHODLNAJ - Rules
            if (player.playerData.kills > opponent.playerData.kills)
            {
                sendEnd("END");
            }
            else
            {
                sendEnd("LOST");
            }
            hasSentEnd = true;
            firstRequest = null;
        }

        public void SendStart()
        {
            client.Send(Encoding.ASCII.GetBytes("START"));
            hasSentStart = true;
        }

        public void SendDeath()
        {
            sendStateWithAction("DEATH");
        }

        public void SendKill()
        {
            sendStateWithAction("KILL");
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

        public static List<object> ZeroArray(int len)
        {
            List<object> arr = new List<object>();
            object[] r = new object[54];
            Array.Copy(new int[54], 0, r, 0, 54);
            arr.AddRange(r);
            return arr;
        }
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

        public static object[] ZeroArray()
        {
            object[] output = new object[54];
            Array.Copy(new int[54], 0, output, 0, 54);
            return output;
        }

        public PlayerState player;
        public PlayerState enemy;
        public BallState ball;
        public WorldState world;

    }

    public class PlayerState
    {

        public PlayerState(GameplayEntities.PlayerEntity entity, GameplayEntities.BallEntity ball, GameplayEntities.PlayerEntity player = null)
        {
            if (entity == null)
            {
                isNull = true;
                return;
            }

            if (player != null)
            {
                this.position = IBGCBLLKIHA.MDDEMEMPBDP(entity.GetPosition(), player.GetPosition());
            }
            else
            {
                this.position = entity.GetPosition();
            }
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
            if (isNull) {
                return LethalLearning.LethalLearning.ZeroArray(11);
            }

            var list = new List<System.Object> {
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

        private bool isNull = false;
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
        public BallState(GameplayEntities.BallEntity ball, GameplayEntities.PlayerEntity player)
        {
            if (ball == null) {
                isNull = true;
            }
        
            this.velocity = ball.entityData.velocity;
            this.position = IBGCBLLKIHA.MDDEMEMPBDP(ball.GetPosition(), player.GetPosition());
        }

        public override string ToString()
        {
            return " ball vel: " + this.velocity.ToString() + ", ball pos: " + this.position.ToString();
        }

        public List<System.Object> ToList()
        {
            if (isNull)
            {
                return LethalLearning.LethalLearning.ZeroArray(4);
            }
            return new List<System.Object> {
                this.velocity.GCPKPHMKLBN.ToString(),
                this.velocity.CGJJEHPPOAN.ToString(),
                this.position.GCPKPHMKLBN.ToString(),
                this.position.CGJJEHPPOAN.ToString(),
            };
        }

        private bool isNull = false;
        public IBGCBLLKIHA velocity;
        public IBGCBLLKIHA position;
        //        public bool isBeingBunted;
    }

    public class WorldState
    {
        public WorldState(World world) { }

        public override string ToString()
        {
            switch (World.state)
            {
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
