using System;
using System.IO;
using System.Collections;

namespace Dcap.Tools
{
    public class DcapServer
    {
        public static void Main (string [] args)
        {
            Console.WriteLine ("Starting DCAP server; press any key to stop");
            var server = new Dacp.Server ("Test DACP Server");
            server.AddDatabase (new Daap.Database ("Test DB"));
            server.Commit ();
            server.Start ();

            Console.ReadLine ();

            Console.WriteLine ("Stopping DCAP server");
            server.Stop ();
        }
    }
}

