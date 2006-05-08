/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections;

namespace DAAP.Tools {

    public class Mirror {

        private static Server server;
        private static ArrayList clients = new ArrayList ();
        
        public static void Main (string[] args) {
            string name = "Foobar Mirror";
            ushort port = 3690;
            
            if (args.Length > 0)
                name = args[0];
            if (args.Length > 1)
                port = UInt16.Parse (args[1]);
            
            server = new Server (name);
            server.Port = port;
            server.Start ();

            ServiceLocator locator = new ServiceLocator ();
            locator.Found += OnServiceFound;
            locator.Removed += OnServiceRemoved;
            locator.Start ();

            Console.WriteLine ("Press enter to quit");
            Console.ReadLine ();

            foreach (Client client in clients) {
                client.Logout ();
            }

            locator.Stop ();
            server.Stop ();
        }

        private static void OnServiceFound (object o, ServiceArgs args) {
            if (args.Service.Name == server.Name)
                return;
            
            Console.WriteLine ("Found: " + args.Service.Name);
            if (args.Service.IsProtected) {
                Console.WriteLine ("Password is required, skipping");
                return;
            }

            Client client = new Client (args.Service);
            client.Login ();
            client.Updated += OnClientUpdated;
            
            foreach (Database db in client.Databases) {
                server.AddDatabase (db);
                Console.WriteLine ("Added database: " + db.Name);
            }

            server.Commit ();
            clients.Add (client);
        }

        private static void OnServiceRemoved (object o, ServiceArgs args) {
            Console.WriteLine ("Removed: " + args.Service.Name);

            foreach (Client client in clients) {
                if (client.Name == args.Service.Name) {
                    foreach (Database db in client.Databases) {
                        server.RemoveDatabase (db);
                    }
                    
                    clients.Remove (client);
                    break;
                }
            }
        }

        private static void OnClientUpdated (object o, EventArgs args) {
            server.Commit ();
        }
    }
}
