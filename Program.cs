using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using swed32;
using System.Threading;

namespace template
{
    internal class Program
    {

        [DllImport("user32.dll")]

        static extern short GetAsyncKeyState(Keys vKey);


        #region mem
        //client
        const int localplayer = 0xDB35DC;
        const int entitylist = 0x4DCEEAC;

        //engine

        const int clientstate = 0x058BFC4;
        const int viewangles = 0x4D90;

        //offsets

        const int health = 0x100;
        const int xyz = 0x138;
        const int team = 0xF4;
        const int dormant = 0xED;



        #endregion

        static void Main(string[] args)
        {

            swed swed = new swed();

            swed.GetProcess("csgo");

            // how to get module base address 

            var client = swed.GetModuleBase("client.dll");
            var engine = swed.GetModuleBase("engine.dll");

            entity player = new entity();
            List<entity> entities = new List<entity>();

            while (true)
            {
                if (GetAsyncKeyState(Keys.XButton2) < 0)
                {
                    updatelocal();
                    updateentities();


                    entities = entities.OrderBy(o => o.mag).ToList();
                    if (entities.Count > 0)
                        aim(entities[0]);
                }

                Thread.Sleep(1);

            }

            float calcmag(entity e)
            {
                return (float)Math.Sqrt(Math.Pow(e.x - player.x, 2) + Math.Pow(e.y - player.y, 2) + Math.Pow(e.z - player.z, 2));
            }


            void updatelocal()
            {
                var buffer = swed.ReadPointer(client, localplayer);

                var coords = swed.ReadBytes(buffer, xyz, 12);


                player.x = BitConverter.ToSingle(coords, 0);
                player.y = BitConverter.ToSingle(coords, 0);
                player.z = BitConverter.ToSingle(coords, 0);

                player.team = BitConverter.ToInt32(swed.ReadBytes(buffer, team, 4), 0);
            }

            void updateentities()
            {
                entities.Clear();

                for (int i = 1; i < 32; i++)
                {
                    var buffer = swed.ReadPointer(client, entitylist + i * 0x10);
                    var tm = BitConverter.ToInt32(swed.ReadBytes(buffer, team, 4), 0);

                    var dorm = BitConverter.ToInt32(swed.ReadBytes(buffer, dormant, 4), 0);

                    var hp = BitConverter.ToInt32(swed.ReadBytes(buffer, health, 4), 0);



                    if (hp < 2 || dorm != 0 || tm == player.team)
                        continue;

                    var coords = swed.ReadBytes(buffer, xyz, 12);


                    var ent = new entity();
                    ent.x = BitConverter.ToSingle(coords, 0);
                    ent.y = BitConverter.ToSingle(coords, 0);
                    ent.z = BitConverter.ToSingle(coords, 0);
                    ent.team = tm;
                    ent.health = hp;

                    ent.mag = calcmag(ent);
                    entities.Add(ent);
                }
            }

            void aim(entity ent)
            {
                // x
                float deltaX = ent.x - player.x;
                float deltaY = ent.y - player.y;

                float X = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

                // Y
                float deltaZ = ent.z - player.z;
                double dist = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

                float Y = -(float)(Math.Atan2(deltaZ, dist) * 180 / Math.PI);


                var buffer = swed.ReadPointer(engine, clientstate);
                swed.WriteBytes(buffer, viewangles, BitConverter.GetBytes(Y));
                swed.WriteBytes(buffer, viewangles + 0x4, BitConverter.GetBytes(X));



            }


        }
    }
}
