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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

using Mono.Zeroconf;

namespace Dmap {

    public delegate void ServiceHandler (object o, ServiceArgs args);

    public class ServiceArgs : EventArgs {

        private Service service;

        public Service Service {
            get { return service; }
        }

        public ServiceArgs (Service service) {
            this.service = service;
        }
    }

    public class Service {
        private IPAddress address;
        private ushort port;
        private string name;
        private bool isprotected;

        public IPAddress Address {
            get { return address; }
        }

        public ushort Port {
            get { return port; }
        }

        public string Name {
            get { return name; }
        }

        public bool IsProtected {
            get { return isprotected; }
        }

        public Service (IPAddress address, ushort port, string name, bool isprotected) {
            this.address = address;
            this.port = port;
            this.name = name;
            this.isprotected = isprotected;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1} ({2})", Address, Port, Name);
        }
    }

    public class ServiceLocator {

        private ServiceBrowser browser;
        private Dictionary <string, Service> services = new Dictionary <string, Service> ();
        private bool showLocals = false;

        public event ServiceHandler Found;
        public event ServiceHandler Removed;

        public bool ShowLocalServices {
            get { return showLocals; }
            set { showLocals = value; }
        }

        public Service [] Services {
            get {
                Service [] ret = new Service [services.Count];
                services.Values.CopyTo (ret, 0);
                return ret;
            }
        }

        public ServiceLocator ()
        {
        }

        public void Start () {
            if (browser != null) {
                Stop ();
            }

            browser = new ServiceBrowser ();
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
            browser.Browse ("_daap._tcp", null);
        }

        public void Stop () {
            if (browser != null) {
                browser.Dispose ();
                browser = null;
            }
            services.Clear ();
        }

        private static void Log (string str, params object [] args)
        {
            if (args == null || args.Length == 0)
                Console.WriteLine (str);
            else
                Console.WriteLine (str, args);
        }

        private void OnServiceAdded (object o, ServiceBrowseEventArgs args) {
            args.Service.Resolved += OnServiceResolved;
            Log ("Found DAAP share {0}, trying to resolve...", args.Service.Name);
            args.Service.Resolve ();
        }

        private void OnServiceResolved (object o, ServiceResolvedEventArgs args) {
            string name = args.Service.Name;

            Log ("Managed to resolve DAAP share {0}.", args.Service.Name);

            bool password_required = false;

            // iTunes tacks this on to indicate a passsword protected share.  Ugh.
            if (name.EndsWith ("_PW")) {
                name = name.Substring (0, name.Length - 3);
                password_required = true;
            }

            IResolvableService service = (IResolvableService) args.Service;

            foreach(TxtRecordItem item in service.TxtRecord) {
                if(item.Key.ToLower () == "password") {
                    password_required = item.ValueString.ToLower () == "true";
                } else if (item.Key.ToLower () == "machine name") {
                    name = item.ValueString;
                }
            }

            IPAddress address = args.Service.HostEntry.AddressList[0];

            Log ("OnServiceResolved provided {0}", address);

            // XXX: Workaround a Mono bug where we can't resolve IPv6 addresses properly
            if (services.ContainsKey (name) && address.AddressFamily == AddressFamily.InterNetworkV6) {
                // Only skip this service if it resolves to a IPv6 address, and we already have info
                // for this service already.
                Log ("Skipping service: already have IPv4 address.");
                return;
            } else if (!services.ContainsKey (name) && address.AddressFamily == AddressFamily.InterNetworkV6) {
                // This is the first address we've resolved, however, it's an IPv6 address.
                // Try and resolve the hostname in hope that it'll end up as an IPv4 address - it doesn't
                // really matter if it still ends up with an IPv6 address, we're not risking anything.

                foreach (IPAddress addr in Dns.GetHostEntry (args.Service.HostEntry.HostName).AddressList) {
                    if (addr.AddressFamily == AddressFamily.InterNetwork) {
                        address = addr;
                    }
                }
            }

            Log ("Using address {0}", address);

            Dmap.Service svc = new Dmap.Service (address, (ushort)service.Port,
                name, password_required);

            if (services.ContainsKey (name)) {
                services[name] = svc;
            } else {
                services.Add (name, svc);
            }

            if (Found != null)
                Found (this, new ServiceArgs (svc));
        }

        private void OnServiceRemoved (object o, ServiceBrowseEventArgs args) {
            if (services.ContainsKey (args.Service.Name)) {
                Service svc = (Service) services[args.Service.Name];
                if (svc != null) {
                    services.Remove (svc.Name);

                    if (Removed != null)
                        Removed (this, new ServiceArgs (svc));
                }
            }
        }
    }
}
