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
using System.Text;
using System.Collections;
using Avahi;

namespace DAAP {

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

        private Avahi.Client client;
        private ServiceBrowser browser;
        private Hashtable services = new Hashtable ();
        private ArrayList resolvers = new ArrayList ();
        private bool showLocals = false;

        public event ServiceHandler Found;
        public event ServiceHandler Removed;

        public bool ShowLocalServices {
            get { return showLocals; }
            set { showLocals = value; }
        }
        
        public Service[] Services {
            get {
                ArrayList list = new ArrayList ();

                foreach (Service service in services.Values) {
                    list.Add (service);
                }

                return (Service[]) list.ToArray (typeof (Service));
            }
        }
        
        public ServiceLocator () {
            client = new Avahi.Client ();
        }

        public void Start () {
            browser = new ServiceBrowser (client, "_daap._tcp");
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
        }

        public void Stop () {
            services.Clear ();
            browser.Dispose ();
        }

        private void OnServiceAdded (object o, ServiceInfoArgs args) {
            if ((args.Service.Flags & LookupResultFlags.Local) > 0 && !showLocals)
                return;
            
            ServiceResolver resolver = new ServiceResolver (client, args.Service);
            resolvers.Add (resolver);
            resolver.Found += OnServiceResolved;
            resolver.Timeout += OnServiceTimeout;
        }

        private void OnServiceResolved (object o, ServiceInfoArgs args) {

            resolvers.Remove (o);
            (o as ServiceResolver).Dispose ();

            string name = args.Service.Name;

            if (services[args.Service.Name] != null) {
                return; // we already have it somehow
            }
            
            bool pwRequired = false;

            // iTunes tacks this on to indicate a passsword protected share.  Ugh.
            if (name.EndsWith ("_PW")) {
                name = name.Substring (0, name.Length - 3);
                pwRequired = true;
            }
            
            foreach (byte[] txt in args.Service.Text) {
                string txtstr = Encoding.UTF8.GetString (txt);

                string[] splitstr = txtstr.Split('=');

                if (splitstr.Length < 2)
                    continue;

                if (splitstr[0].ToLower () == "password")
                    pwRequired = splitstr[1].ToLower () == "true";
                else if (splitstr[0].ToLower () == "machine name")
                    name = splitstr[1];
            }

            Service svc = new Service (args.Service.Address, args.Service.Port,
                                       name, pwRequired);

            services[svc.Name] = svc;

            if (Found != null)
                Found (this, new ServiceArgs (svc));
        }

        private void OnServiceTimeout (object o, EventArgs args) {
            Console.Error.WriteLine ("Failed to resolve");
        }

        private void OnServiceRemoved (object o, ServiceInfoArgs args) {
            Service svc = (Service) services[args.Service.Name];
            if (svc != null) {
                services.Remove (svc);

                if (Removed != null)
                    Removed (this, new ServiceArgs (svc));
            }
        }
    }
}
