using System;
using System.IO;
using System.Collections;

using Dmap;

namespace Dmap.Tools
{
    public class DcapServer
    {
        public static void Main (string [] args)
        {
            Console.WriteLine ("Starting DCAP server; press ctrl-d to stop");
            var server = new Server ("Test DACP Server");
            server.Start ();

            Console.ReadLine ();

            Console.WriteLine ("Stopping DCAP server");
            server.Stop ();
        }
    }
}

