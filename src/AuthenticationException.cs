
using System;

namespace Dmap {

    public class AuthenticationException : ApplicationException {

        public AuthenticationException (string msg) : base (msg) {
        }
    }
}
