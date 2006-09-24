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
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DAAP {

    public class Client : IDisposable {
        private const int UpdateSleepInterval = 2 * 60 * 1000; // 2 minutes
        
        private IPAddress address;
        private UInt16 port;
        private ContentCodeBag bag;
        private ServerInfo serverInfo;
        private List<Database> databases = new List<Database> ();
        private ContentFetcher fetcher;
        private int revision;
        private bool updateRunning;

        public event EventHandler Updated;

        internal int Revision {
            get { return revision; }
        }

        public string Name {
            get { return serverInfo.Name; }
        }

        public IPAddress Address {
            get { return address; }
        }

        public ushort Port {
            get { return port; }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return serverInfo.AuthenticationMethod; }
        }

        public IList<Database> Databases {
            get { return new ReadOnlyCollection<Database> (databases); }
        }

        internal ContentCodeBag Bag {
            get { return bag; }
        }

        internal ContentFetcher Fetcher {
            get { return fetcher; }
        }

        public Client (Service service) : this (service.Address, service.Port) {
        }

        public Client (string host, UInt16 port) : this (Dns.Resolve (host).AddressList[0], port) {
        }

        public Client (IPAddress address, UInt16 port) {
            this.address = address;
            this.port = port;
            fetcher = new ContentFetcher (address, port);

            bag = ContentCodeBag.ParseCodes (fetcher.Fetch ("/content-codes"));

            ContentNode node = ContentParser.Parse (ContentCodeBag.Default, fetcher.Fetch ("/server-info"));
            serverInfo = ServerInfo.FromNode (node);
        }

        ~Client () {
            Dispose ();
        }

        public void Dispose () {
            updateRunning = false;
            
            if (fetcher != null) {
                fetcher.Dispose ();
                fetcher = null;
            }
        }

        private void ParseSessionId (ContentNode node) {
            fetcher.SessionId = (int) node.GetChild ("dmap.sessionid").Value;
        }

        public void Login () {
            Login (null, null);
        }

        public void Login (string password) {
            Login (null, password);
        }

        public void Login (string username, string password) {
            fetcher.Username = username;
            fetcher.Password = password;

            try {
                ContentNode node = ContentParser.Parse (bag, fetcher.Fetch ("/login"));
                ParseSessionId (node);

                FetchDatabases ();
                Refresh ();
                
                if (serverInfo.SupportsUpdate) {
                    updateRunning = true;
                    Thread thread = new Thread (UpdateLoop);
                    thread.IsBackground = true;
                    thread.Start ();
                }
            } catch (WebException e) {
                if (e.Response != null && (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                    throw new AuthenticationException ("Username or password incorrect");
                else
                    throw new LoginException ("Failed to login", e);
            } catch (Exception e) {
                throw new LoginException ("Failed to login", e);
            }
        }

        public void Logout () {
            try {
                updateRunning = false;
                fetcher.KillAll ();
                fetcher.Fetch ("/logout");
            } catch (WebException e) {
                // some servers don't implement this, etc.
            }
            
            fetcher.SessionId = 0;
        }

        private void FetchDatabases () {
            ContentNode dbnode = ContentParser.Parse (bag, fetcher.Fetch ("/databases"));

            foreach (ContentNode child in (ContentNode[]) dbnode.Value) {
                if (child.Name != "dmap.listing")
                    continue;

                foreach (ContentNode item in (ContentNode[]) child.Value) {
                    Database db = new Database (this, item);
                    databases.Add (db);
                }
            }
        }

        private int GetCurrentRevision () {
            ContentNode revNode = ContentParser.Parse (bag, fetcher.Fetch ("/update"), "dmap.serverrevision");
            return (int) revNode.Value;
        }

        private int WaitForRevision (int currentRevision) {
            ContentNode revNode = ContentParser.Parse (bag, fetcher.Fetch ("/update",
                                                                           "revision-number=" + currentRevision));

            return (int) revNode.GetChild ("dmap.serverrevision").Value;
        }

        private void Refresh () {
            int newrev = revision;

            if (serverInfo.SupportsUpdate) {
                if (revision == 0)
                    newrev = GetCurrentRevision ();
                else
                    newrev = WaitForRevision (revision);
            
                if (newrev == revision)
                    return;
            }
                
            // Console.WriteLine ("Got new revision: " + newrev);
            foreach (Database db in databases) {
                db.Refresh (newrev);
            }
            
            revision = newrev;
            if (Updated != null)
                Updated (this, new EventArgs ());
        }

        private void UpdateLoop () {
            while (true) {
                try {
                    if (!updateRunning)
                        break;
                    
                    Refresh ();
                } catch (WebException e) {
                    if (!updateRunning)
                        break;
                    
                    // chill out for a while, maybe the server went down
                    // temporarily or something.
                    Thread.Sleep (UpdateSleepInterval);
                } catch (Exception e) {
                    if (!updateRunning)
                        break;
                    
                    Console.Error.WriteLine ("Exception in update loop: " + e);
                    Thread.Sleep (UpdateSleepInterval);
                }
            }
        }
    }
}
